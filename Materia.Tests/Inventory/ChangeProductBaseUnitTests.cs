using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;

namespace Materia.Tests.Inventory;

/// <summary>
/// Domain-layer tests for Product.ChangeBaseUnit, Stock.CorrectUnit,
/// and the ChangeProductBaseUnitCommandValidator.
/// </summary>
public class ChangeProductBaseUnitTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Product PristineProduct(string unit = "sak")
    {
        var p = Product.Create("Semen", null, new UnitName(unit), "user-1");
        p.ClearDomainEvents();
        return p;
    }

    private static Stock PristineStock(Product product)
    {
        var s = Stock.Initialize(product.Id, product.BaseUnit.Value, "user-1");
        s.ClearDomainEvents();
        return s;
    }

    // ── Product.ChangeBaseUnit — happy path ───────────────────────────────────

    [Fact]
    public void ChangeBaseUnit_Pristine_RaisesProductBaseUnitChanged()
    {
        var product = PristineProduct("sak");

        product.ChangeBaseUnit(new UnitName("kg"), "admin");

        var evt = product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductBaseUnitChanged>().Subject;
        evt.ProductId.Should().Be(product.Id);
        evt.BaseUnit.Should().Be("kg");
        evt.ChangedBy.Should().Be("admin");
    }

    [Fact]
    public void ChangeBaseUnit_Pristine_UpdatesBaseUnitOnAggregate()
    {
        var product = PristineProduct("sak");

        product.ChangeBaseUnit(new UnitName("kg"), "admin");

        product.BaseUnit.Value.Should().Be("kg");
    }

    // ── Product.ChangeBaseUnit — guard rejections ─────────────────────────────

    [Fact]
    public void ChangeBaseUnit_WhenInactive_ThrowsDomainException()
    {
        var product = PristineProduct();
        product.Deactivate("user-1");
        product.ClearDomainEvents();

        Action act = () => product.ChangeBaseUnit(new UnitName("kg"), "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*inactive*");
    }

    [Fact]
    public void ChangeBaseUnit_WithUnitConversions_ThrowsDomainException()
    {
        var product = PristineProduct("sak");
        product.AddUnitConversion(
            new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 5), "user-1");
        product.ClearDomainEvents();

        Action act = () => product.ChangeBaseUnit(new UnitName("kg"), "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*unit conversions*");
    }

    [Fact]
    public void ChangeBaseUnit_WithColorVariants_ThrowsDomainException()
    {
        var product = PristineProduct("kaleng");
        product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        product.ClearDomainEvents();

        Action act = () => product.ChangeBaseUnit(new UnitName("pcs"), "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*color variants*");
    }

    [Fact]
    public void ChangeBaseUnit_SameUnit_ThrowsDomainException()
    {
        var product = PristineProduct("sak");

        Action act = () => product.ChangeBaseUnit(new UnitName("sak"), "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*differ*");
    }

    // ── Product.Reconstitute reflects new unit ────────────────────────────────

    [Fact]
    public void Reconstitute_AfterChangeBaseUnit_RestoresNewUnit()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.ChangeBaseUnit(new UnitName("kg"), "admin");

        var restored = Product.Reconstitute(product.DomainEvents.ToList());

        restored.BaseUnit.Value.Should().Be("kg");
        restored.DomainEvents.Should().BeEmpty();
    }

    // ── Stock.CorrectUnit — happy path ────────────────────────────────────────

    [Fact]
    public void CorrectUnit_NoMovements_RaisesStockUnitCorrected()
    {
        var product = PristineProduct("sak");
        var stock = PristineStock(product);

        stock.CorrectUnit("kg", "admin");

        var evt = stock.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<StockUnitCorrected>().Subject;
        evt.Unit.Should().Be("kg");
        evt.CorrectedBy.Should().Be("admin");
        evt.StockId.Should().Be(stock.Id);
        evt.ProductId.Should().Be(product.Id);
    }

    [Fact]
    public void CorrectUnit_NoMovements_UpdatesUnitOnAggregate()
    {
        var product = PristineProduct("sak");
        var stock = PristineStock(product);

        stock.CorrectUnit("kg", "admin");

        stock.Unit.Should().Be("kg");
    }

    // ── Stock.CorrectUnit — guard rejections ──────────────────────────────────

    [Fact]
    public void CorrectUnit_AfterAdjust_ThrowsDomainException()
    {
        var product = PristineProduct();
        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        stock.Adjust(10m, "stok awal", "user-1");
        stock.ClearDomainEvents();

        Action act = () => stock.CorrectUnit("kg", "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*movements*");
    }

    [Fact]
    public void CorrectUnit_AfterReconcileFromPurchase_ThrowsDomainException()
    {
        var product = PristineProduct();
        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        var poId = Domain.Purchasing.PurchaseOrderId.New();
        stock.ReconcileFromPurchase(5m, poId, 10_000m, "sak", "user-1");
        stock.ClearDomainEvents();

        Action act = () => stock.CorrectUnit("kg", "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*movements*");
    }

    [Fact]
    public void CorrectUnit_AfterReduceFromPurchaseReturn_ThrowsDomainException()
    {
        var product = PristineProduct();
        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        stock.Adjust(10m, null, "user-1"); // give some qty first
        stock.ClearDomainEvents();

        // We must bypass the _hasMovements that was already set; reconstruct a fresh
        // stock from only the initialization event to then apply a PurchaseReturn.
        var freshStock = Stock.Initialize(product.Id, "sak", "user-1");
        var poId = Domain.Purchasing.PurchaseOrderId.New();
        freshStock.ReconcileFromPurchase(10m, poId, 5_000m, "sak", "user-1");
        freshStock.ReduceFromPurchaseReturn(2m, poId, 5_000m, "sak", "user-1");
        freshStock.ClearDomainEvents();

        Action act = () => freshStock.CorrectUnit("kg", "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*movements*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CorrectUnit_BlankUnit_ThrowsDomainException(string unit)
    {
        var product = PristineProduct();
        var stock = PristineStock(product);

        Action act = () => stock.CorrectUnit(unit, "admin");

        act.Should().Throw<DomainException>();
    }

    // ── Stock.Reconstitute reflects corrected unit ────────────────────────────

    [Fact]
    public void Reconstitute_AfterCorrectUnit_RestoresNewUnit()
    {
        var product = PristineProduct("sak");
        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        stock.CorrectUnit("kg", "admin");

        var restored = Stock.Reconstitute(stock.DomainEvents.ToList());

        restored.Unit.Should().Be("kg");
        restored.DomainEvents.Should().BeEmpty();
    }

    // ── After CorrectUnit, further CorrectUnit is blocked if movements exist ──

    [Fact]
    public void CorrectUnit_ThenAdjust_ThenCorrectUnit_ThrowsDomainException()
    {
        var product = PristineProduct("sak");
        var stock = Stock.Initialize(product.Id, "sak", "user-1");
        stock.CorrectUnit("kg", "admin");
        stock.Adjust(10m, null, "user-1");
        stock.ClearDomainEvents();

        Action act = () => stock.CorrectUnit("pcs", "admin");

        act.Should().Throw<DomainException>()
            .WithMessage("*movements*");
    }
}
