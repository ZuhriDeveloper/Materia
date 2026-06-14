using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;
using Materia.Domain.Purchasing.Events;

namespace Materia.Tests.Purchasing;

public class PurchaseOrderTests
{
    private static SupplierId AnySupplier => SupplierId.New();
    private static ProductId AnyProduct => ProductId.New();

    [Fact]
    public void Create_WithValidLines_RaisesPurchaseOrderCreated()
    {
        var productId = AnyProduct;
        var po = PurchaseOrder.Create(AnySupplier, [(productId, 10m, 50_000m, "pcs")], "user1");

        var evt = po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderCreated>().Subject;

        po.Status.Should().Be(PurchaseOrderStatus.Draft);
        evt.Lines.Should().HaveCount(1);
        evt.Lines[0].OrderedQty.Should().Be(10m);
        evt.Lines[0].UnitCost.Should().Be(50_000m);
    }

    [Fact]
    public void Create_WithNoLines_ThrowsDomainException()
    {
        var noLines = new List<(ProductId ProductId, decimal Qty, decimal UnitCost, string Unit)>();
        Action act = () => PurchaseOrder.Create(AnySupplier, noLines, "user1");
        act.Should().Throw<DomainException>().WithMessage("*at least one line*");
    }

    [Fact]
    public void Create_WithZeroQty_ThrowsDomainException()
    {
        Action act = () => PurchaseOrder.Create(AnySupplier, [(AnyProduct, 0m, 50_000m, "pcs")], "user1");
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void Create_WithZeroCost_ThrowsDomainException()
    {
        Action act = () => PurchaseOrder.Create(AnySupplier, [(AnyProduct, 5m, 0m, "pcs")], "user1");
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void Confirm_FromDraft_ChangesStatusToConfirmed()
    {
        var po = BuildConfirmedPo();

        po.Status.Should().Be(PurchaseOrderStatus.Confirmed);
        po.DomainEvents.Should().Contain(e => e is PurchaseOrderConfirmed);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ThrowsDomainException()
    {
        var po = BuildConfirmedPo();
        po.ClearDomainEvents();

        Action act = () => po.Confirm("user1");
        act.Should().Throw<DomainException>().WithMessage("*Draft*");
    }

    [Fact]
    public void Receive_FullQty_TransitionsToReceived()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.ClearDomainEvents();

        po.Receive([(productId, 10m)], "warehouse");

        po.Status.Should().Be(PurchaseOrderStatus.Received);
        po.ReceivedAt.Should().NotBeNull();
        po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderReceived>();
    }

    [Fact]
    public void Receive_PartialQty_TransitionsToPartiallyReceived()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.ClearDomainEvents();

        po.Receive([(productId, 5m)], "warehouse");

        po.Status.Should().Be(PurchaseOrderStatus.PartiallyReceived);
        po.ReceivedAt.Should().BeNull();
    }

    [Fact]
    public void Receive_AccumulatesReceivedQty_AcrossMultipleCalls()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId, orderedQty: 10m);
        po.Receive([(productId, 4m)], "warehouse");
        po.ClearDomainEvents();

        po.Receive([(productId, 6m)], "warehouse");

        po.Status.Should().Be(PurchaseOrderStatus.Received);
        po.Lines[0].ReceivedQty.Should().Be(10m);
    }

    [Fact]
    public void Receive_ProductNotInPo_ThrowsDomainException()
    {
        var po = BuildConfirmedPo();
        po.ClearDomainEvents();

        Action act = () => po.Receive([(ProductId.New(), 5m)], "warehouse");
        act.Should().Throw<DomainException>().WithMessage("*not found in this PO*");
    }

    [Fact]
    public void Receive_WhenAlreadyReceived_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");

        Action act = () => po.Receive([(productId, 1m)], "warehouse");
        act.Should().Throw<DomainException>();
    }

    // ── Per-line chained discount ─────────────────────────────────────────────

    [Fact]
    public void Create_WithLineDiscounts_StoresNetUnitCostAndChain()
    {
        var productId = AnyProduct;
        var po = PurchaseOrder.Create(
            AnySupplier,
            [(productId, 10m, 100_000m, (IReadOnlyList<decimal>)[12.5m, 7m, 5m], "pcs")],
            "user1");

        var line = po.Lines.Should().ContainSingle().Subject;
        line.ListUnitCost.Should().Be(100_000m);
        line.UnitCost.Should().Be(77_306.25m);   // net after 12.5+7+5
        line.Discounts.Should().Equal(12.5m, 7m, 5m);

        var evt = po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderCreated>().Subject;
        evt.Lines[0].UnitCost.Should().Be(77_306.25m);
        evt.Lines[0].ListUnitCost.Should().Be(100_000m);
        evt.Lines[0].Discounts.Should().Equal(12.5m, 7m, 5m);
    }

    [Fact]
    public void Create_FlatOverload_LeavesListEqualToNetAndNoDiscounts()
    {
        var po = PurchaseOrder.Create(AnySupplier, [(AnyProduct, 10m, 50_000m, "pcs")], "user1");

        var line = po.Lines[0];
        line.UnitCost.Should().Be(50_000m);
        line.ListUnitCost.Should().Be(50_000m);
        line.Discounts.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithDiscounts_NetFlowsIntoReconstitutedLine()
    {
        var productId = AnyProduct;
        var original = PurchaseOrder.Create(
            AnySupplier,
            [(productId, 4m, 100_000m, (IReadOnlyList<decimal>)[12.5m, 7m, 5m], "pcs")],
            "user1");

        var reconstituted = PurchaseOrder.Reconstitute(original.DomainEvents);

        reconstituted.Lines[0].UnitCost.Should().Be(77_306.25m);
        reconstituted.Lines[0].Discounts.Should().Equal(12.5m, 7m, 5m);
    }

    [Fact]
    public void Create_WithEditedListUnitCost_UsesItForNet()
    {
        var productId = AnyProduct;
        var po = PurchaseOrder.Create(
            AnySupplier,
            [(productId, 5m, 110_000m, (IReadOnlyList<decimal>)[], "pcs")],
            "user1");

        po.Lines[0].ListUnitCost.Should().Be(110_000m);
        po.Lines[0].UnitCost.Should().Be(110_000m);   // no discount → net == list
    }

    // ── Update-catalog-on-receipt flag ────────────────────────────────────────

    [Fact]
    public void Create_WithUpdateCatalogOnReceipt_StoresFlagAndRaisesItOnEvent()
    {
        var po = PurchaseOrder.Create(
            AnySupplier, [(AnyProduct, 10m, 50_000m, "pcs")], "user1",
            paymentTerm: null, updateCatalogOnReceipt: true);

        po.UpdateCatalogOnReceipt.Should().BeTrue();
        po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderCreated>()
            .Which.UpdateCatalogOnReceipt.Should().BeTrue();
    }

    [Fact]
    public void Create_DefaultsUpdateCatalogOnReceiptToFalse()
    {
        var po = PurchaseOrder.Create(AnySupplier, [(AnyProduct, 10m, 50_000m, "pcs")], "user1");
        po.UpdateCatalogOnReceipt.Should().BeFalse();
    }

    [Fact]
    public void Create_WithDiscountOutOfRange_ThrowsDomainException()
    {
        Action act = () => PurchaseOrder.Create(
            AnySupplier,
            [(AnyProduct, 1m, 100_000m, (IReadOnlyList<decimal>)[120m], "pcs")],
            "user1");
        act.Should().Throw<DomainException>().WithMessage("*between 0 and 100*");
    }

    // ── Payment tenor ─────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithPaymentTerm_StoresTermAndRaisesItOnEvent()
    {
        var po = PurchaseOrder.Create(
            AnySupplier, [(AnyProduct, 10m, 50_000m, "pcs")], "user1",
            new PaymentTerm(2, PaymentTermUnit.Months));

        po.PaymentTerm.Should().Be(new PaymentTerm(2, PaymentTermUnit.Months));

        var evt = po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderCreated>().Subject;
        evt.PaymentTermValue.Should().Be(2);
        evt.PaymentTermUnit.Should().Be("Months");
    }

    [Fact]
    public void Create_WithoutPaymentTerm_IsCash()
    {
        var po = PurchaseOrder.Create(AnySupplier, [(AnyProduct, 10m, 50_000m, "pcs")], "user1");
        po.PaymentTerm.Should().BeNull();
    }

    // ── Returns / void ────────────────────────────────────────────────────────

    [Fact]
    public void RecordReturn_ReducesNetReceivedQty()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");
        po.ClearDomainEvents();

        po.RecordReturn([(productId, 3m)], "broken on arrival", "warehouse");

        po.Lines[0].ReturnedQty.Should().Be(3m);
        po.Lines[0].NetReceivedQty.Should().Be(7m);
        po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderReturned>();
    }

    [Fact]
    public void RecordReturn_FullQty_NetsToZero()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");

        po.RecordReturn([(productId, 10m)], "all broken", "warehouse");

        po.Lines[0].NetReceivedQty.Should().Be(0m);
    }

    [Fact]
    public void RecordReturn_MoreThanNetReceived_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");

        Action act = () => po.RecordReturn([(productId, 11m)], "too many", "warehouse");
        act.Should().Throw<DomainException>().WithMessage("*only 10*");
    }

    [Fact]
    public void RecordReturn_AccumulatesAndRejectsOverNetAcrossCalls()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");
        po.RecordReturn([(productId, 6m)], "first batch", "warehouse");

        Action act = () => po.RecordReturn([(productId, 5m)], "second batch", "warehouse");
        act.Should().Throw<DomainException>();
        po.Lines[0].NetReceivedQty.Should().Be(4m);
    }

    [Fact]
    public void RecordReturn_BeforeAnyReceipt_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);

        Action act = () => po.RecordReturn([(productId, 1m)], "nothing received yet", "warehouse");
        act.Should().Throw<DomainException>().WithMessage("*received goods*");
    }

    [Fact]
    public void RecordReturn_WithoutReason_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");

        Action act = () => po.RecordReturn([(productId, 2m)], "  ", "warehouse");
        act.Should().Throw<DomainException>().WithMessage("*reason*");
    }

    [Fact]
    public void RecordReturn_ProductNotInPo_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");

        Action act = () => po.RecordReturn([(ProductId.New(), 1m)], "wrong product", "warehouse");
        act.Should().Throw<DomainException>().WithMessage("*not found in this PO*");
    }

    [Fact]
    public void Reconstitute_WithReturnsAndTerm_RestoresState()
    {
        var productId = AnyProduct;
        var original = PurchaseOrder.Create(
            AnySupplier, [(productId, 10m, 50_000m, "pcs")], "user1",
            new PaymentTerm(1, PaymentTermUnit.Weeks));
        original.Confirm("user1");
        original.Receive([(productId, 10m)], "warehouse");
        original.RecordReturn([(productId, 4m)], "broken", "warehouse");

        var reconstituted = PurchaseOrder.Reconstitute(original.DomainEvents);

        reconstituted.PaymentTerm.Should().Be(new PaymentTerm(1, PaymentTermUnit.Weeks));
        reconstituted.Lines[0].NetReceivedQty.Should().Be(6m);
        reconstituted.DomainEvents.Should().BeEmpty();
    }

    // ── Short-close ("Selesaikan PO") ─────────────────────────────────────────

    [Fact]
    public void Close_FromPartiallyReceived_TransitionsToClosedAndSetsReceivedAt()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId, orderedQty: 10m);
        po.Receive([(productId, 4m)], "warehouse");   // partial
        po.ClearDomainEvents();

        po.Close("supplier out of stock", "user1");

        po.Status.Should().Be(PurchaseOrderStatus.Closed);
        po.ReceivedAt.Should().NotBeNull();
        po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderClosed>();
    }

    [Fact]
    public void Close_WithoutReason_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId, orderedQty: 10m);
        po.Receive([(productId, 4m)], "warehouse");

        Action act = () => po.Close("  ", "user1");
        act.Should().Throw<DomainException>().WithMessage("*reason*");
    }

    [Fact]
    public void Close_WhenNothingReceived_ThrowsDomainException()
    {
        var po = BuildConfirmedPo();   // Confirmed, no receipts

        Action act = () => po.Close("supplier out of stock", "user1");
        act.Should().Throw<DomainException>().WithMessage("*partially-received*");
    }

    [Fact]
    public void Close_WhenFullyReceived_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId, orderedQty: 10m);
        po.Receive([(productId, 10m)], "warehouse");   // full → Received

        Action act = () => po.Close("too late", "user1");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RecordReturn_OnClosedPo_Succeeds()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId, orderedQty: 10m);
        po.Receive([(productId, 6m)], "warehouse");
        po.Close("supplier out of stock", "user1");
        po.ClearDomainEvents();

        po.RecordReturn([(productId, 2m)], "broken", "warehouse");

        po.Lines[0].NetReceivedQty.Should().Be(4m);
        po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderReturned>();
    }

    [Fact]
    public void Receive_WhenClosed_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId, orderedQty: 10m);
        po.Receive([(productId, 4m)], "warehouse");
        po.Close("supplier out of stock", "user1");

        Action act = () => po.Receive([(productId, 1m)], "warehouse");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reconstitute_AfterClose_RestoresClosedState()
    {
        var productId = AnyProduct;
        var original = BuildConfirmedPo(productId, orderedQty: 10m);
        original.Receive([(productId, 4m)], "warehouse");
        original.Close("supplier out of stock", "user1");

        var reconstituted = PurchaseOrder.Reconstitute(original.DomainEvents);

        reconstituted.Status.Should().Be(PurchaseOrderStatus.Closed);
        reconstituted.ReceivedAt.Should().NotBeNull();
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_WhenPartiallyReceived_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId, orderedQty: 10m);
        po.Receive([(productId, 4m)], "warehouse");   // partial → has receipts

        Action act = () => po.Cancel("changed mind", "user1");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_FromDraft_TransitionsToCancelled()
    {
        var po = PurchaseOrder.Create(AnySupplier, [(AnyProduct, 10m, 50_000m, "pcs")], "user1");
        po.ClearDomainEvents();

        po.Cancel("supplier unavailable", "user1");

        po.Status.Should().Be(PurchaseOrderStatus.Cancelled);
        po.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<PurchaseOrderCancelled>();
    }

    [Fact]
    public void Cancel_WhenReceived_ThrowsDomainException()
    {
        var productId = AnyProduct;
        var po = BuildConfirmedPo(productId);
        po.Receive([(productId, 10m)], "warehouse");

        Action act = () => po.Cancel("too late", "user1");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Reconstitute_FromEvents_RestoresState()
    {
        var productId = AnyProduct;
        var original = BuildConfirmedPo(productId);
        original.Receive([(productId, 10m)], "warehouse");

        var reconstituted = PurchaseOrder.Reconstitute(original.DomainEvents);

        reconstituted.Status.Should().Be(PurchaseOrderStatus.Received);
        reconstituted.Lines[0].ReceivedQty.Should().Be(10m);
        reconstituted.DomainEvents.Should().BeEmpty();
    }

    private static PurchaseOrder BuildConfirmedPo(ProductId? productId = null, decimal orderedQty = 10m)
    {
        var pid = productId ?? AnyProduct;
        var po = PurchaseOrder.Create(AnySupplier, [(pid, orderedQty, 50_000m, "pcs")], "user1");
        po.Confirm("user1");
        return po;
    }
}
