using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Validators.Auth;

namespace Materia.Tests.Auth;

public class ForgotPasswordCommandValidatorTests
{
    private readonly ForgotPasswordCommandValidator _validator = new();

    [Fact]
    public void ValidEmail_Passes() =>
        _validator.Validate(new ForgotPasswordCommand("user@materia.local")).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    public void InvalidEmail_Fails(string email)
    {
        var result = _validator.Validate(new ForgotPasswordCommand(email));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(ForgotPasswordCommand.Email));
    }
}
