using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;

namespace Materia.Tests.Inventory;

public class ProductColorVariantTests
{
    private static Product NewProduct(decimal salePrice = 50_000m)
    {
        var product = Product.Create("Cat Tembok", null, new UnitName("kaleng"), "user-1", salePrice: salePrice);
        product.ClearDomainEvents();
        return product;
    }

    // ── Color value object ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Color_WithBlankName_ThrowsDomainException(string name)
    {
        Action act = () => _ = new Color(name);

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    [Fact]
    public void Color_NormalizesNameAndBlankCodeToNull()
    {
        var color = new Color("  Merah  ", "   ");

        color.Name.Should().Be("Merah");
        color.Code.Should().BeNull();
    }

    [Fact]
    public void Color_WithCode_RendersBothInToString()
    {
        new Color("Biru", "RAL 5010").ToString().Should().Be("Biru (RAL 5010)");
        new Color("Biru").ToString().Should().Be("Biru");
    }

    // ── Add ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void AddColorVariant_Valid_RaisesEventAndUpdatesState()
    {
        var product = NewProduct();

        var variantId = product.AddColorVariant(
            new Color("Merah", "RAL 3020"), " 8990001112223 ", priceOverride: 55_000m, "user-1");

        var evt = product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductColorVariantAdded>().Subject;
        evt.VariantId.Should().Be(variantId);
        evt.ColorName.Should().Be("Merah");
        evt.ColorCode.Should().Be("RAL 3020");
        evt.Barcode.Should().Be("8990001112223");      // trimmed
        evt.PriceOverride.Should().Be(55_000m);

        var variant = product.ColorVariants.Should().ContainSingle().Subject;
        variant.Id.Should().Be(variantId);
        variant.Color.Name.Should().Be("Merah");
        variant.Barcode.Should().Be("8990001112223");
        variant.PriceOverride.Should().Be(55_000m);
        variant.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AddColorVariant_WithoutOverride_LeavesPriceNull()
    {
        var product = NewProduct();

        product.AddColorVariant(new Color("Putih"), barcode: null, priceOverride: null, "user-1");

        product.ColorVariants.Single().PriceOverride.Should().BeNull();
    }

    [Fact]
    public void AddColorVariant_DuplicateColorName_IsCaseInsensitiveAndThrows()
    {
        var product = NewProduct();
        product.AddColorVariant(new Color("Merah"), null, null, "user-1");

        Action act = () => product.AddColorVariant(new Color("MERAH"), null, null, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*MERAH*already exists*");
    }

    [Fact]
    public void AddColorVariant_DuplicateBarcode_ThrowsDomainException()
    {
        var product = NewProduct();
        product.AddColorVariant(new Color("Merah"), "111", null, "user-1");

        Action act = () => product.AddColorVariant(new Color("Biru"), "111", null, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*111*already used*");
    }

    [Fact]
    public void AddColorVariant_NegativePriceOverride_ThrowsDomainException()
    {
        var product = NewProduct();

        Action act = () => product.AddColorVariant(new Color("Merah"), null, priceOverride: -1m, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*negative*");
    }

    [Fact]
    public void AddColorVariant_TwoDistinctColors_BothTracked()
    {
        var product = NewProduct();

        product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        product.AddColorVariant(new Color("Biru"), null, null, "user-1");

        product.ColorVariants.Select(v => v.Color.Name).Should().BeEquivalentTo("Merah", "Biru");
    }

    // ── Update ──────────────────────────────────────────────────────────────────

    [Fact]
    public void UpdateColorVariant_Existing_RaisesEventAndMutatesState()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), "111", 55_000m, "user-1");
        product.ClearDomainEvents();

        product.UpdateColorVariant(variantId, new Color("Merah Tua", "RAL 3004"), "222", 60_000m, "user-2");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductColorVariantUpdated>();
        var variant = product.ColorVariants.Single();
        variant.Color.Name.Should().Be("Merah Tua");
        variant.Color.Code.Should().Be("RAL 3004");
        variant.Barcode.Should().Be("222");
        variant.PriceOverride.Should().Be(60_000m);
    }

    [Fact]
    public void UpdateColorVariant_KeepingOwnColorName_DoesNotFalselyFlagDuplicate()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), null, null, "user-1");

        Action act = () => product.UpdateColorVariant(variantId, new Color("Merah"), "999", null, "user-1");

        act.Should().NotThrow();
    }

    [Fact]
    public void UpdateColorVariant_ToAnotherVariantsColorName_ThrowsDomainException()
    {
        var product = NewProduct();
        product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        var blueId = product.AddColorVariant(new Color("Biru"), null, null, "user-1");

        Action act = () => product.UpdateColorVariant(blueId, new Color("Merah"), null, null, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*already exists*");
    }

    [Fact]
    public void UpdateColorVariant_Unknown_ThrowsDomainException()
    {
        var product = NewProduct();

        Action act = () => product.UpdateColorVariant(VariantId.New(), new Color("Merah"), null, null, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*not found*");
    }

    // ── Remove ──────────────────────────────────────────────────────────────────

    [Fact]
    public void RemoveColorVariant_Existing_RaisesEventAndRemoves()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        product.ClearDomainEvents();

        product.RemoveColorVariant(variantId, "user-1");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductColorVariantRemoved>();
        product.ColorVariants.Should().BeEmpty();
    }

    [Fact]
    public void RemoveColorVariant_Unknown_IsIdempotentAndRaisesNoEvent()
    {
        var product = NewProduct();

        product.RemoveColorVariant(VariantId.New(), "user-1");

        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void RemoveColorVariant_ThenReAddSameColor_IsAllowed()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        product.RemoveColorVariant(variantId, "user-1");

        Action act = () => product.AddColorVariant(new Color("Merah"), null, null, "user-1");

        act.Should().NotThrow();
        product.ColorVariants.Should().ContainSingle().Which.Color.Name.Should().Be("Merah");
    }

    // ── Activate / Deactivate ───────────────────────────────────────────────────

    [Fact]
    public void DeactivateColorVariant_WhenActive_RaisesEventAndSetsInactive()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        product.ClearDomainEvents();

        product.DeactivateColorVariant(variantId, "user-1");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductColorVariantDeactivated>();
        product.ColorVariants.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public void DeactivateColorVariant_AlreadyInactive_ThrowsDomainException()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        product.DeactivateColorVariant(variantId, "user-1");

        Action act = () => product.DeactivateColorVariant(variantId, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }

    [Fact]
    public void ActivateColorVariant_WhenInactive_RaisesEventAndSetsActive()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), null, null, "user-1");
        product.DeactivateColorVariant(variantId, "user-1");
        product.ClearDomainEvents();

        product.ActivateColorVariant(variantId, "user-1");

        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductColorVariantActivated>();
        product.ColorVariants.Single().IsActive.Should().BeTrue();
    }

    [Fact]
    public void ActivateColorVariant_AlreadyActive_ThrowsDomainException()
    {
        var product = NewProduct();
        var variantId = product.AddColorVariant(new Color("Merah"), null, null, "user-1");

        Action act = () => product.ActivateColorVariant(variantId, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*already active*");
    }

    // ── Event Sourcing Reconstruction ───────────────────────────────────────────

    [Fact]
    public void Reconstitute_RestoresColorVariantsWithFullStateAndLifecycle()
    {
        var original = Product.Create("Cat Tembok", null, new UnitName("kaleng"), "user-1", salePrice: 50_000m);
        var red = original.AddColorVariant(new Color("Merah", "RAL 3020"), "111", 55_000m, "user-1");
        var blue = original.AddColorVariant(new Color("Biru"), "222", null, "user-1");
        var green = original.AddColorVariant(new Color("Hijau"), "333", null, "user-1");
        original.UpdateColorVariant(blue, new Color("Biru Laut"), "999", 52_000m, "user-2");
        original.DeactivateColorVariant(green, "user-2");
        original.RemoveColorVariant(red, "user-2");

        var restored = Product.Reconstitute(original.DomainEvents.ToList());

        restored.ColorVariants.Should().HaveCount(2);
        restored.ColorVariants.Should().NotContain(v => v.Id == red);

        var restoredBlue = restored.ColorVariants.Single(v => v.Id == blue);
        restoredBlue.Color.Name.Should().Be("Biru Laut");
        restoredBlue.Barcode.Should().Be("999");
        restoredBlue.PriceOverride.Should().Be(52_000m);
        restoredBlue.IsActive.Should().BeTrue();

        var restoredGreen = restored.ColorVariants.Single(v => v.Id == green);
        restoredGreen.IsActive.Should().BeFalse();

        restored.DomainEvents.Should().BeEmpty();
    }
}
