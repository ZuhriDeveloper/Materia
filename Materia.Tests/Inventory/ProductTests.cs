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
}
