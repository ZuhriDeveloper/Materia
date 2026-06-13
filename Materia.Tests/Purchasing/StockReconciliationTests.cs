using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;
using Materia.Domain.Purchasing;

namespace Materia.Tests.Purchasing;

public class StockReconciliationTests
{
    [Fact]
    public void ReconcileFromPurchase_WhenStockNegative_RestoresBalance()
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");
        stock.Adjust(-5m, "sale before stock arrives", "cashier1");
        stock.ClearDomainEvents();

        stock.ReconcileFromPurchase(10m, PurchaseOrderId.New(), 50_000m, "pcs", "warehouse");

        stock.Quantity.Should().Be(5m);
        var evt = stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReconciledFromPurchase>().Subject;
        evt.ReceivedQty.Should().Be(10m);
        evt.NewQuantity.Should().Be(5m);
        evt.UnitCost.Should().Be(50_000m);
    }

    [Fact]
    public void ReconcileFromPurchase_WhenStockIsZero_IncreasesCorrectly()
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");
        stock.ClearDomainEvents();

        stock.ReconcileFromPurchase(20m, PurchaseOrderId.New(), 45_000m, "pcs", "warehouse");

        stock.Quantity.Should().Be(20m);
    }

    [Fact]
    public void ReconcileFromPurchase_WithZeroQty_ThrowsDomainException()
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");

        Action act = () => stock.ReconcileFromPurchase(0m, PurchaseOrderId.New(), 50_000m, "pcs", "warehouse");
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void ReconcileFromPurchase_WithNegativeQty_ThrowsDomainException()
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");

        Action act = () => stock.ReconcileFromPurchase(-5m, PurchaseOrderId.New(), 50_000m, "pcs", "warehouse");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ReduceFromPurchaseReturn_LowersQuantity()
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");
        stock.ReconcileFromPurchase(10m, PurchaseOrderId.New(), 50_000m, "pcs", "warehouse");
        stock.ClearDomainEvents();

        var poId = PurchaseOrderId.New();
        stock.ReduceFromPurchaseReturn(3m, poId, 50_000m, "pcs", "warehouse");

        stock.Quantity.Should().Be(7m);
        var evt = stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockReducedFromPurchaseReturn>().Subject;
        evt.ReturnedQty.Should().Be(3m);
        evt.NewQuantity.Should().Be(7m);
        evt.PurchaseOrderId.Should().Be(poId);
    }

    [Fact]
    public void ReduceFromPurchaseReturn_MayDriveStockNegative()
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");
        stock.ReconcileFromPurchase(5m, PurchaseOrderId.New(), 50_000m, "pcs", "warehouse");
        stock.Adjust(-5m, "sold before return processed", "cashier1");
        stock.ClearDomainEvents();

        stock.ReduceFromPurchaseReturn(2m, PurchaseOrderId.New(), 50_000m, "pcs", "warehouse");

        stock.Quantity.Should().Be(-2m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-4)]
    public void ReduceFromPurchaseReturn_NonPositiveQty_ThrowsDomainException(decimal qty)
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");

        Action act = () => stock.ReduceFromPurchaseReturn(qty, PurchaseOrderId.New(), 50_000m, "pcs", "warehouse");
        act.Should().Throw<DomainException>().WithMessage("*positive*");
    }

    [Fact]
    public void Adjust_NegativeDelta_AllowsStockBelowZero()
    {
        var stock = Stock.Initialize(ProductId.New(), "pcs", "system");
        stock.Adjust(5m, null, "user1");
        stock.ClearDomainEvents();

        stock.Adjust(-10m, "urgent sale", "cashier1");

        stock.Quantity.Should().Be(-5m);
        stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockAdjusted>();
    }
}
