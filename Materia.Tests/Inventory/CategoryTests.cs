using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;

namespace Materia.Tests.Inventory;

public class CategoryTests
{
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

        act.Should().Throw<DomainException>()
            .WithMessage("*name*");
    }

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
    public void Reconstitute_FromEvents_RestoresStateWithNoUncommittedEvents()
    {
        var original = Category.Create("Bahan", null, "user-1");
        original.Update("Bahan Bangunan", "Updated", "user-2");

        var restored = Category.Reconstitute(original.DomainEvents.ToList());

        restored.Id.Should().Be(original.Id);
        restored.Name.Should().Be("Bahan Bangunan");
        restored.Description.Should().Be("Updated");
        restored.DomainEvents.Should().BeEmpty();
    }
}
