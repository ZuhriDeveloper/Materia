using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;

namespace Materia.Tests.Inventory;

public class StockTests
{
    private static ProductId AnyProductId() => ProductId.New();

    [Fact]
    public void Initialize_WithValidData_RaisesStockInitialized()
    {
        var productId = AnyProductId();
        var stock = Stock.Initialize(productId, "sak", "user-1");

        var evt = stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockInitialized>().Subject;

        evt.ProductId.Should().Be(productId);
        evt.Quantity.Should().Be(0m);
        evt.Unit.Should().Be("sak");
        evt.CreatedBy.Should().Be("user-1");
    }

    [Fact]
    public void Initialize_StartsAtZero()
    {
        var stock = Stock.Initialize(AnyProductId(), "kg", "user-1");

        stock.Quantity.Should().Be(0m);
        stock.Unit.Should().Be("kg");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Initialize_WithBlankUnit_ThrowsDomainException(string unit)
    {
        Action act = () => Stock.Initialize(AnyProductId(), unit, "user-1");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Adjust_PositiveDelta_IncreasesQuantity()
    {
        var stock = Stock.Initialize(AnyProductId(), "sak", "user-1");
        stock.ClearDomainEvents();

        stock.Adjust(50m, "Stok awal", "user-1");

        stock.Quantity.Should().Be(50m);
        var evt = stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockAdjusted>().Subject;
        evt.Delta.Should().Be(50m);
        evt.NewQuantity.Should().Be(50m);
    }

    [Fact]
    public void Adjust_NegativeDelta_DecreasesQuantity()
    {
        var stock = Stock.Initialize(AnyProductId(), "sak", "user-1");
        stock.Adjust(100m, null, "user-1");
        stock.ClearDomainEvents();

        stock.Adjust(-30m, "Penjualan", "user-2");

        stock.Quantity.Should().Be(70m);
    }

    [Fact]
    public void Adjust_BelowZero_AllowsNegativeStock()
    {
        // Negative stock is intentionally allowed so sales can proceed before
        // a delayed stocktake or purchase order arrives.
        var stock = Stock.Initialize(AnyProductId(), "sak", "user-1");
        stock.Adjust(10m, null, "user-1");
        stock.ClearDomainEvents();

        stock.Adjust(-20m, null, "user-1");

        stock.Quantity.Should().Be(-10m);
        stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockAdjusted>();
    }

    [Fact]
    public void Adjust_DecimalQuantity_WorksCorrectly()
    {
        var stock = Stock.Initialize(AnyProductId(), "kg", "user-1");
        stock.Adjust(1.5m, null, "user-1");
        stock.Adjust(2.25m, null, "user-1");

        stock.Quantity.Should().Be(3.75m);
    }

    [Fact]
    public void Reconstitute_FromEvents_RestoresState()
    {
        var productId = AnyProductId();
        var original = Stock.Initialize(productId, "sak", "user-1");
        original.Adjust(100m, "Stok awal", "user-1");
        original.Adjust(-5m, "Penjualan", "user-2");

        var reconstituted = Stock.Reconstitute(original.DomainEvents);

        reconstituted.ProductId.Should().Be(productId);
        reconstituted.Quantity.Should().Be(95m);
        reconstituted.Unit.Should().Be("sak");
        reconstituted.DomainEvents.Should().BeEmpty();
    }
}
