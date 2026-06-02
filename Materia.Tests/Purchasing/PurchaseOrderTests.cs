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
        Action act = () => PurchaseOrder.Create(AnySupplier, [], "user1");
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
