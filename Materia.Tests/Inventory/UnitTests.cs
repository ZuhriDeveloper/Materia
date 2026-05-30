using FluentAssertions;
using Materia.Domain.Common;
using Materia.Domain.Inventory;
using Materia.Domain.Inventory.Events;

namespace Materia.Tests.Inventory;

public class UnitTests
{
    [Fact]
    public void Create_WithValidData_RaisesUnitCreated()
    {
        var unit = Unit.Create("kilogram", "kg", "user-1");

        var evt = unit.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UnitCreated>().Subject;

        evt.Name.Should().Be("kilogram");
        evt.Symbol.Should().Be("kg");
        evt.CreatedBy.Should().Be("user-1");
    }

    [Fact]
    public void Create_WithValidData_IsActiveWithCorrectState()
    {
        var unit = Unit.Create("sak", null, "user-1");

        unit.IsActive.Should().BeTrue();
        unit.Name.Should().Be("sak");
        unit.Symbol.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithBlankName_ThrowsDomainException(string name)
    {
        Action act = () => Unit.Create(name, null, "user-1");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_ChangesNameAndSymbol_RaisesUnitUpdated()
    {
        var unit = Unit.Create("kilogram", "kg", "user-1");
        unit.ClearDomainEvents();

        unit.Update("kilogram baru", "kgb", "user-2");

        var evt = unit.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<UnitUpdated>().Subject;

        evt.Name.Should().Be("kilogram baru");
        evt.Symbol.Should().Be("kgb");
        unit.Name.Should().Be("kilogram baru");
    }

    [Fact]
    public void Update_WhenInactive_ThrowsDomainException()
    {
        var unit = Unit.Create("sak", null, "user-1");
        unit.Deactivate("user-1");

        Action act = () => unit.Update("sak baru", null, "user-1");
        act.Should().Throw<DomainException>().WithMessage("*inactive*");
    }

    [Fact]
    public void Deactivate_ActiveUnit_RaisesUnitDeactivated()
    {
        var unit = Unit.Create("pcs", null, "user-1");
        unit.ClearDomainEvents();

        unit.Deactivate("user-1");

        unit.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UnitDeactivated>();
        unit.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_AlreadyInactive_ThrowsDomainException()
    {
        var unit = Unit.Create("pcs", null, "user-1");
        unit.Deactivate("user-1");

        Action act = () => unit.Deactivate("user-1");
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_InactiveUnit_RaisesUnitActivated()
    {
        var unit = Unit.Create("pcs", null, "user-1");
        unit.Deactivate("user-1");
        unit.ClearDomainEvents();

        unit.Activate("user-2");

        unit.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<UnitActivated>();
        unit.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Reconstitute_FromEvents_RestoresState()
    {
        var original = Unit.Create("kilogram", "kg", "user-1");
        original.Update("kilogram updated", "KG", "user-2");
        original.Deactivate("user-3");

        var reconstituted = Unit.Reconstitute(original.DomainEvents);

        reconstituted.Name.Should().Be("kilogram updated");
        reconstituted.Symbol.Should().Be("KG");
        reconstituted.IsActive.Should().BeFalse();
        reconstituted.DomainEvents.Should().BeEmpty();
    }
}
