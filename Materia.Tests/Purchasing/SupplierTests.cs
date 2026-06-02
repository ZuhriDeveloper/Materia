using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Purchasing;
using Materia.Domain.Purchasing.Events;

namespace Materia.Tests.Purchasing;

public class SupplierTests
{
    [Fact]
    public void Register_WithValidData_RaisesSupplierRegistered()
    {
        var supplier = Supplier.Register("PT Jaya Bahan", "0812xxx", "admin");

        var evt = supplier.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SupplierRegistered>().Subject;

        evt.Name.Should().Be("PT Jaya Bahan");
        evt.ContactPhone.Should().Be("0812xxx");
        evt.CreatedBy.Should().Be("admin");
        supplier.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_WithBlankName_ThrowsDomainException(string name)
    {
        Action act = () => Supplier.Register(name, null, "admin");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetPurchasePrice_AddsToProductCatalog()
    {
        var supplier = Supplier.Register("PT Jaya Bahan", null, "admin");
        var productId = ProductId.New();
        var price = new PurchasePrice(45_000m, "IDR", "pcs", DateTime.UtcNow);

        supplier.SetPurchasePrice(productId, price, "admin");

        supplier.Catalog.Should().ContainKey(productId.Value);
        supplier.Catalog[productId.Value].LatestPrice!.Amount.Should().Be(45_000m);
    }

    [Fact]
    public void SetPurchasePrice_AddsMultiplePrices_LatestPriceIsNewest()
    {
        var supplier = Supplier.Register("PT Jaya Bahan", null, "admin");
        var productId = ProductId.New();

        supplier.SetPurchasePrice(productId,
            new PurchasePrice(40_000m, "IDR", "pcs", DateTime.UtcNow.AddDays(-30)), "admin");
        supplier.SetPurchasePrice(productId,
            new PurchasePrice(45_000m, "IDR", "pcs", DateTime.UtcNow), "admin");

        supplier.Catalog[productId.Value].LatestPrice!.Amount.Should().Be(45_000m);
    }

    [Fact]
    public void SetPurchasePrice_OnInactiveSupplier_ThrowsDomainException()
    {
        var supplier = Supplier.Register("PT Jaya Bahan", null, "admin");
        supplier.Deactivate("admin");

        Action act = () => supplier.SetPurchasePrice(
            ProductId.New(), new PurchasePrice(45_000m, "IDR", "pcs", DateTime.UtcNow), "admin");

        act.Should().Throw<DomainException>().WithMessage("*inactive*");
    }

    [Fact]
    public void Deactivate_WhenActive_RaisesSupplierDeactivated()
    {
        var supplier = Supplier.Register("PT Jaya Bahan", null, "admin");
        supplier.ClearDomainEvents();

        supplier.Deactivate("admin");

        supplier.IsActive.Should().BeFalse();
        supplier.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SupplierDeactivated>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var supplier = Supplier.Register("PT Jaya Bahan", null, "admin");
        supplier.Deactivate("admin");

        Action act = () => supplier.Deactivate("admin");
        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }

    [Fact]
    public void Activate_WhenInactive_RestoresActiveState()
    {
        var supplier = Supplier.Register("PT Jaya Bahan", null, "admin");
        supplier.Deactivate("admin");
        supplier.ClearDomainEvents();

        supplier.Activate("admin");

        supplier.IsActive.Should().BeTrue();
        supplier.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<SupplierActivated>();
    }

    [Fact]
    public void Reconstitute_FromEvents_RestoresState()
    {
        var productId = ProductId.New();
        var original = Supplier.Register("PT Jaya Bahan", "0812xxx", "admin");
        original.SetPurchasePrice(productId,
            new PurchasePrice(45_000m, "IDR", "pcs", DateTime.UtcNow), "admin");

        var reconstituted = Supplier.Reconstitute(original.DomainEvents);

        reconstituted.Name.Should().Be("PT Jaya Bahan");
        reconstituted.IsActive.Should().BeTrue();
        reconstituted.Catalog.Should().ContainKey(productId.Value);
        reconstituted.Catalog[productId.Value].LatestPrice!.Amount.Should().Be(45_000m);
        reconstituted.DomainEvents.Should().BeEmpty();
    }
}
