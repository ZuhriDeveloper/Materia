using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Validators.Auth;

namespace Materia.Tests.Auth;

public class ResetPasswordCommandValidatorTests
{
    private readonly ResetPasswordCommandValidator _validator = new();

    private static ResetPasswordCommand Valid(
        string email = "user@materia.local",
        string token = "abc123",
        string newPassword = "NewPass1") => new(email, token, newPassword);

    [Fact]
    public void ValidCommand_Passes() =>
        _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void InvalidEmail_Fails(string email)
    {
        var result = _validator.Validate(Valid(email: email));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordCommand.Email));
    }

    [Fact]
    public void BlankToken_Fails()
    {
        var result = _validator.Validate(Valid(token: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordCommand.Token));
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("nouppercase1")]
    [InlineData("NoDigitsHere")]
    public void WeakNewPassword_Fails(string newPassword)
    {
        var result = _validator.Validate(Valid(newPassword: newPassword));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ResetPasswordCommand.NewPassword));
    }
}
