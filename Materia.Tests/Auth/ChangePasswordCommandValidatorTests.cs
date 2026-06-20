using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Validators.Auth;

namespace Materia.Tests.Auth;

public class ChangePasswordCommandValidatorTests
{
    private readonly ChangePasswordCommandValidator _validator = new();

    private static ChangePasswordCommand Valid(
        string userId = "user-1",
        string current = "OldPass1",
        string @new = "NewPass1") => new(userId, current, @new);

    [Fact]
    public void ValidCommand_Passes()
    {
        _validator.Validate(Valid()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BlankUserId_Fails(string userId)
    {
        var result = _validator.Validate(Valid(userId: userId));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordCommand.UserId));
    }

    [Fact]
    public void BlankCurrentPassword_Fails()
    {
        var result = _validator.Validate(Valid(current: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordCommand.CurrentPassword));
    }

    [Theory]
    [InlineData("short1A")]      // < 8 chars
    [InlineData("nouppercase1")] // no uppercase
    [InlineData("NoDigitsHere")] // no digit
    public void WeakNewPassword_Fails(string newPassword)
    {
        var result = _validator.Validate(Valid(@new: newPassword));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }

    [Fact]
    public void NewPasswordEqualToCurrent_Fails()
    {
        var result = _validator.Validate(Valid(current: "SamePass1", @new: "SamePass1"));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ChangePasswordCommand.NewPassword));
    }
}
