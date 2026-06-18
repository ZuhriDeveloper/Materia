using FluentAssertions;
using Materia.Application.Commands.Inventory.ChangeProductBaseUnit;

namespace Materia.Tests.Inventory;

public class ChangeProductBaseUnitValidatorTests
{
    private readonly ChangeProductBaseUnitCommandValidator _validator = new();

    private static ChangeProductBaseUnitCommand Valid(
        Guid? productId = null,
        string baseUnit = "kg",
        string changedBy = "admin") =>
        new(productId ?? Guid.NewGuid(), baseUnit, changedBy);

    [Fact]
    public void ValidCommand_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyProductId_Fails()
    {
        var result = _validator.Validate(Valid(productId: Guid.Empty));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeProductBaseUnitCommand.ProductId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankBaseUnit_Fails(string unit)
    {
        var result = _validator.Validate(Valid(baseUnit: unit));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeProductBaseUnitCommand.BaseUnit));
    }

    [Fact]
    public void BaseUnitExceedsMaxLength_Fails()
    {
        var longUnit = new string('x', 51);
        var result = _validator.Validate(Valid(baseUnit: longUnit));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeProductBaseUnitCommand.BaseUnit));
    }

    [Fact]
    public void BaseUnitAtMaxLength_Passes()
    {
        var maxUnit = new string('x', 50);
        var result = _validator.Validate(Valid(baseUnit: maxUnit));
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankChangedBy_Fails(string changedBy)
    {
        var result = _validator.Validate(Valid(changedBy: changedBy));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(ChangeProductBaseUnitCommand.ChangedBy));
    }
}
