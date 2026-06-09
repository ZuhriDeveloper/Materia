using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Sales;
using Materia.Domain.Sales.Events;

namespace Materia.Tests.Sales;

public class SaleAggregateTests
{
    private const string Staff = "kasir-01";

    private static Sale SaleWithOneItem()
    {
        var sale = Sale.Create("INV-0001", Staff);
        sale.AddItem(
            productId: Guid.NewGuid(),
            productName: "Semen 50kg",
            unitName: "sak",
            quantity: 3m,
            quantityInBaseUnit: 3m,
            unitPrice: 65_000m,
            updatedBy: Staff);
        return sale;
    }

    [Fact]
    public void AddItem_WithColorVariant_CapturesVariantOnEventAndItem()
    {
        var sale = Sale.Create("INV-0100", Staff);
        var variantId = Guid.NewGuid();

        var itemId = sale.AddItem(
            productId: Guid.NewGuid(),
            productName: "Cat Tembok",
            unitName: "kaleng",
            quantity: 2m,
            quantityInBaseUnit: 2m,
            unitPrice: 55_000m,
            updatedBy: Staff,
            variantId: variantId,
            colorName: "  Merah  ");

        var evt = sale.DomainEvents.OfType<SaleItemAdded>().Single();
        evt.VariantId.Should().Be(variantId);
        evt.ColorName.Should().Be("Merah");   // trimmed

        var item = sale.Items.Single(i => i.Id == itemId);
        item.VariantId.Should().Be(variantId);
        item.ColorName.Should().Be("Merah");
    }

    [Fact]
    public void AddItem_WithoutVariant_LeavesVariantNull()
    {
        var sale = SaleWithOneItem();

        var item = sale.Items.Single();
        item.VariantId.Should().BeNull();
        item.ColorName.Should().BeNull();
    }

    [Fact]
    public void Reconstitute_RestoresItemVariantAndColor()
    {
        var sale = Sale.Create("INV-0101", Staff);
        var variantId = Guid.NewGuid();
        sale.AddItem(Guid.NewGuid(), "Cat", "kaleng", 1m, 1m, 50_000m, Staff, variantId, "Biru");

        var restored = Sale.Reconstitute(sale.DomainEvents.ToList());

        var item = restored.Items.Single();
        item.VariantId.Should().Be(variantId);
        item.ColorName.Should().Be("Biru");
    }

    [Fact]
    public void Finalize_WithItems_RaisesSaleFinalizedWithStaffAndTotal()
    {
        var sale = SaleWithOneItem();
        sale.ClearDomainEvents();

        sale.Finalize(Staff);

        var evt = sale.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SaleFinalized>().Subject;
        evt.ServedBy.Should().Be(Staff);
        evt.GrandTotal.Should().Be(195_000m);          // 3 × 65,000
        evt.IsDeliveryRequired.Should().BeFalse();
    }

    [Fact]
    public void Finalize_SetsServedByAndConfirmedStatus()
    {
        var sale = SaleWithOneItem();

        sale.Finalize(Staff);

        sale.ServedBy.Should().Be(Staff);
        sale.Status.Should().Be(SaleStatus.Confirmed);
    }

    [Fact]
    public void Finalize_WithoutItems_ThrowsDomainException()
    {
        var sale = Sale.Create("INV-0002", Staff);

        Action act = () => sale.Finalize(Staff);

        act.Should().Throw<DomainException>().WithMessage("*tanpa item*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Finalize_WithBlankServedBy_ThrowsDomainException(string servedBy)
    {
        var sale = SaleWithOneItem();

        Action act = () => sale.Finalize(servedBy);

        act.Should().Throw<DomainException>().WithMessage("*ServedBy*");
    }

    [Fact]
    public void Finalize_WhenAlreadyFinalized_ThrowsDomainException()
    {
        var sale = SaleWithOneItem();
        sale.Finalize(Staff);

        Action act = () => sale.Finalize(Staff);

        act.Should().Throw<DomainException>();   // no longer Draft
    }

    [Fact]
    public void RequestDelivery_SetsFlagAndRaisesEvent()
    {
        var sale = SaleWithOneItem();
        sale.ClearDomainEvents();

        sale.RequestDelivery(Staff);

        sale.IsDeliveryRequired.Should().BeTrue();
        sale.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<DeliveryRequested>();
    }

    [Fact]
    public void RequestDelivery_ThenFinalize_PropagatesFlagToEvent()
    {
        var sale = SaleWithOneItem();
        sale.RequestDelivery(Staff);
        sale.ClearDomainEvents();

        sale.Finalize(Staff);

        var evt = sale.DomainEvents.OfType<SaleFinalized>().Single();
        evt.IsDeliveryRequired.Should().BeTrue();
    }

    [Fact]
    public void RequestDelivery_IsIdempotent()
    {
        var sale = SaleWithOneItem();
        sale.RequestDelivery(Staff);
        sale.ClearDomainEvents();

        sale.RequestDelivery(Staff);

        sale.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Finalize_DoesNotDependOnStock_AllowingNegativeInventoryDownstream()
    {
        // The aggregate intentionally has no stock coupling — inventory is decremented
        // by the application handler, which is allowed to drive the balance negative.
        var sale = SaleWithOneItem();

        Action act = () => sale.Finalize(Staff);

        act.Should().NotThrow();
    }

    [Fact]
    public void Reconstitute_ReplaysServedByAndDeliveryFlag()
    {
        var original = SaleWithOneItem();
        original.RequestDelivery(Staff);
        original.Finalize(Staff);

        var replayed = Sale.Reconstitute(original.DomainEvents);

        replayed.ServedBy.Should().Be(Staff);
        replayed.IsDeliveryRequired.Should().BeTrue();
        replayed.Status.Should().Be(SaleStatus.Confirmed);
        replayed.GrandTotal.Amount.Should().Be(195_000m);
        replayed.DomainEvents.Should().BeEmpty();
    }

    // ── Discount & tax on finalize ─────────────────────────────────────────────

    [Fact]
    public void Finalize_WithDiscount_ReducesGrandTotal()
    {
        var sale = SaleWithOneItem();   // subtotal 195,000

        sale.Finalize(Staff, discount: 15_000m);

        sale.Discount.Amount.Should().Be(15_000m);
        sale.GrandTotal.Amount.Should().Be(180_000m);
    }

    [Fact]
    public void Finalize_WithTaxEnabled_AddsElevenPercentPpn()
    {
        var sale = SaleWithOneItem();   // subtotal 195,000

        sale.Finalize(Staff, taxEnabled: true);

        sale.Tax.Amount.Should().Be(21_450m);            // 195,000 × 11%
        sale.GrandTotal.Amount.Should().Be(216_450m);
    }

    [Fact]
    public void Finalize_AppliesPpnOnTheDiscountedAmount()
    {
        var sale = SaleWithOneItem();   // subtotal 195,000

        sale.Finalize(Staff, discount: 95_000m, taxEnabled: true);

        sale.Tax.Amount.Should().Be(11_000m);            // (195,000 − 95,000) × 11%
        sale.GrandTotal.Amount.Should().Be(111_000m);    // 100,000 + 11,000
    }

    [Fact]
    public void Finalize_DiscountExceedingSubtotal_IsClampedToSubtotal()
    {
        var sale = SaleWithOneItem();

        sale.Finalize(Staff, discount: 500_000m);

        sale.Discount.Amount.Should().Be(195_000m);
        sale.GrandTotal.Amount.Should().Be(0m);
    }

    // ── Settlement (full payment / down payment / credit) ──────────────────────

    private static Sale FinalizedSaleForCustomer(Guid customerId)
    {
        var sale = Sale.Create("INV-0500", Staff);
        sale.SetCustomer(customerId, "Budi", Staff);
        sale.AddItem(Guid.NewGuid(), "Semen 50kg", "sak", 3m, 3m, 65_000m, Staff);
        sale.Finalize(Staff);     // grand total 195,000
        return sale;
    }

    [Fact]
    public void RecordSettlement_FullPayment_MarksPaidWithNoOutstanding()
    {
        var sale = FinalizedSaleForCustomer(Guid.NewGuid());

        sale.RecordSettlement(195_000m, PaymentMethod.Cash, Staff);

        sale.Status.Should().Be(SaleStatus.Paid);
        sale.AmountPaid.Amount.Should().Be(195_000m);
        sale.OutstandingAmount.Amount.Should().Be(0m);
        sale.Payment!.Change.Amount.Should().Be(0m);
    }

    [Fact]
    public void RecordSettlement_Overpayment_YieldsChangeAndIsPaid()
    {
        var sale = FinalizedSaleForCustomer(Guid.NewGuid());

        sale.RecordSettlement(200_000m, PaymentMethod.Cash, Staff);

        sale.Status.Should().Be(SaleStatus.Paid);
        sale.OutstandingAmount.Amount.Should().Be(0m);
        sale.Payment!.Change.Amount.Should().Be(5_000m);
    }

    [Fact]
    public void RecordSettlement_PartialPayment_ForRegisteredCustomer_LeavesOutstandingDebt()
    {
        var sale = FinalizedSaleForCustomer(Guid.NewGuid());

        sale.RecordSettlement(50_000m, PaymentMethod.Cash, Staff);

        sale.Status.Should().Be(SaleStatus.PartiallyPaid);
        sale.AmountPaid.Amount.Should().Be(50_000m);
        sale.OutstandingAmount.Amount.Should().Be(145_000m);

        var evt = sale.DomainEvents.OfType<SaleSettled>().Single();
        evt.IsCredit.Should().BeTrue();
        evt.OutstandingAmount.Should().Be(145_000m);
    }

    [Fact]
    public void RecordSettlement_ZeroDownPayment_ForRegisteredCustomer_IsFullCredit()
    {
        var sale = FinalizedSaleForCustomer(Guid.NewGuid());

        sale.RecordSettlement(0m, PaymentMethod.Cash, Staff);

        sale.Status.Should().Be(SaleStatus.PartiallyPaid);
        sale.OutstandingAmount.Amount.Should().Be(195_000m);
    }

    [Fact]
    public void RecordSettlement_PartialPayment_ForWalkIn_ThrowsDomainException()
    {
        var sale = Sale.Create("INV-0501", Staff);
        sale.SetCustomer(null, "Umum", Staff);   // anonymous walk-in
        sale.AddItem(Guid.NewGuid(), "Semen 50kg", "sak", 3m, 3m, 65_000m, Staff);
        sale.Finalize(Staff);

        Action act = () => sale.RecordSettlement(50_000m, PaymentMethod.Cash, Staff);

        act.Should().Throw<DomainException>().WithMessage("*pelanggan terdaftar*");
    }

    [Fact]
    public void RecordSettlement_BeforeFinalize_ThrowsDomainException()
    {
        var sale = Sale.Create("INV-0502", Staff);
        sale.SetCustomer(Guid.NewGuid(), "Budi", Staff);
        sale.AddItem(Guid.NewGuid(), "Semen", "sak", 1m, 1m, 65_000m, Staff);

        Action act = () => sale.RecordSettlement(10_000m, PaymentMethod.Cash, Staff);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordSettlement_IsIdempotentOnceSettled()
    {
        var sale = FinalizedSaleForCustomer(Guid.NewGuid());
        sale.RecordSettlement(195_000m, PaymentMethod.Cash, Staff);
        sale.ClearDomainEvents();

        sale.RecordSettlement(50_000m, PaymentMethod.Cash, Staff);   // ignored

        sale.DomainEvents.Should().BeEmpty();
        sale.Status.Should().Be(SaleStatus.Paid);
    }

    [Fact]
    public void Reconstitute_ReplaysCreditSettlement()
    {
        var original = FinalizedSaleForCustomer(Guid.NewGuid());
        original.RecordSettlement(75_000m, PaymentMethod.BankTransfer, Staff);

        var replayed = Sale.Reconstitute(original.DomainEvents);

        replayed.Status.Should().Be(SaleStatus.PartiallyPaid);
        replayed.AmountPaid.Amount.Should().Be(75_000m);
        replayed.OutstandingAmount.Amount.Should().Be(120_000m);
        replayed.Payment!.Method.Should().Be(PaymentMethod.BankTransfer);
        replayed.DomainEvents.Should().BeEmpty();
    }
}
