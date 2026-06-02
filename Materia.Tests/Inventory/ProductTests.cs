using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;

namespace Materia.Tests.Inventory;

public class ProductTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_RaisesProductCreated()
    {
        var product = Product.Create("Semen Padang", "Semen 50kg", new UnitName("sak"), "user-1");

        var evt = product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreated>().Subject;

        evt.Name.Should().Be("Semen Padang");
        evt.Description.Should().Be("Semen 50kg");
        evt.BaseUnit.Should().Be("sak");
        evt.CreatedBy.Should().Be("user-1");
    }

    [Fact]
    public void Create_WithValidData_ProductIsActiveWithCorrectState()
    {
        var product = Product.Create("Besi Beton", null, new UnitName("batang"), "user-1");

        product.IsActive.Should().BeTrue();
        product.Name.Should().Be("Besi Beton");
        product.BaseUnit.Value.Should().Be("batang");
        product.CategoryIds.Should().BeEmpty();
        product.UnitConversions.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        Action act = () => Product.Create(name, null, new UnitName("pcs"), "user-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*name*");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithNewName_RaisesProductNameUpdated()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.ClearDomainEvents();

        product.Update("Semen Padang 50kg", "Desc baru", "user-2");

        product.DomainEvents.Should().Contain(e => e is ProductNameUpdated);
        product.Name.Should().Be("Semen Padang 50kg");
    }

    [Fact]
    public void Update_WhenInactive_ThrowsDomainException()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.Deactivate("user-1");
        product.ClearDomainEvents();

        Action act = () => product.Update("Baru", null, "user-2");

        act.Should().Throw<DomainException>();
    }

    // ── Deactivate / Activate ─────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_RaisesProductDeactivatedAndSetsInactive()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.ClearDomainEvents();

        product.Deactivate("user-1");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductDeactivated>();
        product.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.Deactivate("user-1");

        Action act = () => product.Deactivate("user-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*already inactive*");
    }

    [Fact]
    public void Activate_WhenInactive_RaisesProductActivatedAndSetsActive()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.Deactivate("user-1");
        product.ClearDomainEvents();

        product.Activate("user-2");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductActivated>();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsDomainException()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");

        Action act = () => product.Activate("user-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*already active*");
    }

    // ── Unit Conversions ──────────────────────────────────────────────────────

    [Fact]
    public void AddUnitConversion_Valid_RaisesEventAndUpdatesState()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.ClearDomainEvents();

        // 1 sak = 5 bungkus (10kg each)
        product.AddUnitConversion(new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 5), "user-1");

        var evt = product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUnitConversionAdded>().Subject;

        evt.ToUnit.Should().Be("bungkus");
        evt.Factor.Should().Be(5);

        product.UnitConversions.Should().ContainSingle()
            .Which.Factor.Should().Be(5);
    }

    [Fact]
    public void AddUnitConversion_DuplicateToUnit_ThrowsDomainException()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.AddUnitConversion(new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 5), "user-1");

        Action act = () =>
            product.AddUnitConversion(new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 10), "user-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*bungkus*already exists*");
    }

    [Fact]
    public void AddUnitConversion_WithNonPositiveFactor_ThrowsDomainException()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");

        Action act = () =>
            product.AddUnitConversion(new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 0), "user-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*greater than zero*");
    }

    [Fact]
    public void RemoveUnitConversion_Existing_RaisesEventAndClearsConversion()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        product.AddUnitConversion(new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 5), "user-1");
        product.ClearDomainEvents();

        product.RemoveUnitConversion(new UnitName("bungkus"), "user-1");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductUnitConversionRemoved>();
        product.UnitConversions.Should().BeEmpty();
    }

    [Fact]
    public void UnitConversion_Convert_ReturnsCorrectQuantity()
    {
        var conversion = new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 5);

        // 3 sak × 5 = 15 bungkus
        conversion.Convert(3).Should().Be(15);
    }

    // ── Categories ────────────────────────────────────────────────────────────

    [Fact]
    public void AssignCategory_NewCategory_RaisesEventAndAddsToList()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        var categoryId = CategoryId.New();
        product.ClearDomainEvents();

        product.AssignCategory(categoryId, "user-1");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCategoryAssigned>();
        product.CategoryIds.Should().Contain(categoryId);
    }

    [Fact]
    public void AssignCategory_AlreadyAssigned_IsIdempotentAndRaisesNoEvent()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        var categoryId = CategoryId.New();
        product.AssignCategory(categoryId, "user-1");
        product.ClearDomainEvents();

        product.AssignCategory(categoryId, "user-1");

        product.DomainEvents.Should().BeEmpty();
        product.CategoryIds.Should().ContainSingle();
    }

    [Fact]
    public void RemoveCategory_Assigned_RaisesEventAndRemovesFromList()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1");
        var categoryId = CategoryId.New();
        product.AssignCategory(categoryId, "user-1");
        product.ClearDomainEvents();

        product.RemoveCategory(categoryId, "user-1");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCategoryRemoved>();
        product.CategoryIds.Should().BeEmpty();
    }

    // ── Event Sourcing Reconstruction ─────────────────────────────────────────

    [Fact]
    public void Reconstitute_FromEvents_RestoresFullStateWithNoUncommittedEvents()
    {
        var original = Product.Create("Semen", "50kg", new UnitName("sak"), "user-1");
        var categoryId = CategoryId.New();
        original.AssignCategory(categoryId, "user-1");
        original.AddUnitConversion(new UnitConversion(new UnitName("sak"), new UnitName("bungkus"), 5), "user-1");
        original.Update("Semen Padang", "Updated desc", "user-2");
        original.Deactivate("user-1");

        var restored = Product.Reconstitute(original.DomainEvents.ToList());

        restored.Id.Should().Be(original.Id);
        restored.Name.Should().Be("Semen Padang");
        restored.IsActive.Should().BeFalse();
        restored.CategoryIds.Should().Contain(categoryId);
        restored.UnitConversions.Should().ContainSingle();
        restored.DomainEvents.Should().BeEmpty();
    }

    // ── Sale price & barcode ──────────────────────────────────────────────────

    [Fact]
    public void Create_WithSalePriceAndBarcode_CapturesThemOnEvent()
    {
        var product = Product.Create(
            "Semen Padang", null, new UnitName("sak"), "user-1",
            salePrice: 65_000m, barcode: " 8991234567890 ");

        var evt = product.DomainEvents.OfType<ProductCreated>().Single();
        evt.SalePrice.Should().Be(65_000m);
        evt.Barcode.Should().Be("8991234567890");   // trimmed
        product.SalePrice.Should().Be(65_000m);
        product.Barcode.Should().Be("8991234567890");
    }

    [Fact]
    public void Create_WithNegativeSalePrice_ThrowsDomainException()
    {
        Action act = () => Product.Create("Semen", null, new UnitName("sak"), "user-1", salePrice: -1m);
        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void Create_WithBlankBarcode_NormalizesToNull()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1", barcode: "   ");
        product.Barcode.Should().BeNull();
    }

    [Fact]
    public void Update_ChangingSalePriceAndBarcode_RaisesChangeEvents()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1", salePrice: 60_000m);
        product.ClearDomainEvents();

        product.Update("Semen", null, "user-2", salePrice: 65_000m, barcode: "8991234567890");

        product.DomainEvents.OfType<ProductSalePriceChanged>().Should().ContainSingle()
            .Which.SalePrice.Should().Be(65_000m);
        product.DomainEvents.OfType<ProductBarcodeChanged>().Should().ContainSingle()
            .Which.Barcode.Should().Be("8991234567890");
        product.SalePrice.Should().Be(65_000m);
        product.Barcode.Should().Be("8991234567890");
    }

    [Fact]
    public void Update_WithUnchangedSalePriceAndBarcode_RaisesNoPriceOrBarcodeEvents()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1", salePrice: 60_000m, barcode: "111");
        product.ClearDomainEvents();

        product.Update("Semen Baru", null, "user-2", salePrice: 60_000m, barcode: "111");

        product.DomainEvents.OfType<ProductSalePriceChanged>().Should().BeEmpty();
        product.DomainEvents.OfType<ProductBarcodeChanged>().Should().BeEmpty();
        product.DomainEvents.OfType<ProductNameUpdated>().Should().ContainSingle();
    }

    [Fact]
    public void Reconstitute_RestoresSalePriceAndBarcode()
    {
        var original = Product.Create("Semen", null, new UnitName("sak"), "user-1", salePrice: 60_000m);
        original.Update("Semen", null, "user-2", salePrice: 72_500m, barcode: "8991234567890");

        var restored = Product.Reconstitute(original.DomainEvents.ToList());

        restored.SalePrice.Should().Be(72_500m);
        restored.Barcode.Should().Be("8991234567890");
    }

    [Fact]
    public void AddUnitConversion_WithSalePrice_CapturesPerUnitPrice()
    {
        var product = Product.Create("Semen", null, new UnitName("sak"), "user-1", salePrice: 65_000m);
        product.ClearDomainEvents();

        product.AddUnitConversion(
            new UnitConversion(new UnitName("sak"), new UnitName("dus"), 4, salePrice: 250_000m), "user-1");

        var evt = product.DomainEvents.OfType<ProductUnitConversionAdded>().Single();
        evt.SalePrice.Should().Be(250_000m);
        product.UnitConversions.Single(c => c.ToUnit.Value == "dus").SalePrice.Should().Be(250_000m);
    }

    [Fact]
    public void Reconstitute_RestoresPerUnitConversionPrice()
    {
        var original = Product.Create("Semen", null, new UnitName("sak"), "user-1", salePrice: 65_000m);
        original.AddUnitConversion(
            new UnitConversion(new UnitName("sak"), new UnitName("dus"), 4, salePrice: 250_000m), "user-1");

        var restored = Product.Reconstitute(original.DomainEvents.ToList());

        restored.UnitConversions.Single(c => c.ToUnit.Value == "dus").SalePrice.Should().Be(250_000m);
    }
}
