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
}
