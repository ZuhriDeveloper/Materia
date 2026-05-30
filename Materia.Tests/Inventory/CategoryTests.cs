using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;

namespace Materia.Tests.Inventory;

public class CategoryTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_RaisesCategoryCreatedAndSetsState()
    {
        var category = Category.Create("Bahan Bangunan", "Material konstruksi", "user-1");

        var evt = category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryCreated>().Subject;

        evt.Name.Should().Be("Bahan Bangunan");
        evt.Description.Should().Be("Material konstruksi");
        evt.CreatedBy.Should().Be("user-1");

        category.IsActive.Should().BeTrue();
        category.Name.Should().Be("Bahan Bangunan");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        Action act = () => Category.Create(name, null, "user-1");

        act.Should().Throw<DomainException>().WithMessage("*name*");
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_WithNewName_RaisesCategoryNameUpdated()
    {
        var category = Category.Create("Bahan", null, "user-1");
        category.ClearDomainEvents();

        category.Update("Bahan Bangunan", "Desc", "user-2");

        category.DomainEvents.Should().Contain(e => e is CategoryNameUpdated);
        category.Name.Should().Be("Bahan Bangunan");
    }

    [Fact]
    public void Update_WithSameName_RaisesNoNameEvent()
    {
        var category = Category.Create("Bahan Bangunan", null, "user-1");
        category.ClearDomainEvents();

        category.Update("Bahan Bangunan", null, "user-2");

        category.DomainEvents.Should().NotContain(e => e is CategoryNameUpdated);
    }

    [Fact]
    public void Update_WhenInactive_ThrowsDomainException()
    {
        var category = Category.Create("Bahan", null, "user-1");
        category.Deactivate("user-1");
        category.ClearDomainEvents();

        Action act = () => category.Update("Baru", null, "user-2");

        act.Should().Throw<DomainException>();
    }

    // ── Deactivate / Activate ─────────────────────────────────────────────────

    [Fact]
    public void Deactivate_WhenActive_RaisesCategoryDeactivatedAndSetsInactive()
    {
        var category = Category.Create("Bahan", null, "user-1");
        category.ClearDomainEvents();

        category.Deactivate("user-1");

        category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryDeactivated>();
        category.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_ThrowsDomainException()
    {
        var category = Category.Create("Bahan", null, "user-1");
        category.Deactivate("user-1");

        Action act = () => category.Deactivate("user-1");

        act.Should().Throw<DomainException>().WithMessage("*already inactive*");
    }

    [Fact]
    public void Activate_WhenInactive_RaisesCategoryActivatedAndSetsActive()
    {
        var category = Category.Create("Bahan", null, "user-1");
        category.Deactivate("user-1");
        category.ClearDomainEvents();

        category.Activate("user-2");

        category.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<CategoryActivated>();
        category.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_ThrowsDomainException()
    {
        var category = Category.Create("Bahan", null, "user-1");

        Action act = () => category.Activate("user-1");

        act.Should().Throw<DomainException>().WithMessage("*already active*");
    }

    // ── Event Sourcing Reconstruction ─────────────────────────────────────────

    [Fact]
    public void Reconstitute_FromEvents_RestoresFullStateWithNoUncommittedEvents()
    {
        var original = Category.Create("Bahan", null, "user-1");
        original.Update("Bahan Bangunan", "Updated", "user-2");
        original.Deactivate("user-1");

        var restored = Category.Reconstitute(original.DomainEvents.ToList());

        restored.Id.Should().Be(original.Id);
        restored.Name.Should().Be("Bahan Bangunan");
        restored.Description.Should().Be("Updated");
        restored.IsActive.Should().BeFalse();
        restored.DomainEvents.Should().BeEmpty();
    }
}
