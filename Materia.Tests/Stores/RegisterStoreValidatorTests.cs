using FluentAssertions;
using Materia.Application.Commands.Stores.RegisterStore;

namespace Materia.Tests.Stores;

public class RegisterStoreValidatorTests
{
    private readonly RegisterStoreCommandValidator _validator = new();

    private static RegisterStoreCommand Valid(
        string name = "Toko Pusat",
        string code = "S1",
        string registeredBy = "owner-1") =>
        new(name, code, registeredBy);

    [Fact]
    public void Valid_Passes()
    {
        var result = _validator.Validate(Valid());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankName_Fails(string name)
    {
        var result = _validator.Validate(Valid(name: name));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStoreCommand.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankCode_Fails(string code)
    {
        var result = _validator.Validate(Valid(code: code));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStoreCommand.Code));
    }

    [Fact]
    public void BlankRegisteredBy_Fails()
    {
        var result = _validator.Validate(Valid(registeredBy: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStoreCommand.RegisteredBy));
    }

    [Fact]
    public void NameTooLong_Fails()
    {
        var result = _validator.Validate(Valid(name: new string('x', 201)));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RegisterStoreCommand.Name));
    }
}
