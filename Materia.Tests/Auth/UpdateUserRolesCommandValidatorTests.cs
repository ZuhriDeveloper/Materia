using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Validators.Auth;

namespace Materia.Tests.Auth;

public class UpdateUserRolesCommandValidatorTests
{
    private readonly UpdateUserRolesCommandValidator _validator = new();

    private static UpdateUserRolesCommand Valid(
        string userId = "user-1",
        params string[] roles)
        => new(userId, roles.Length == 0 ? ["Admin"] : roles);

    [Fact]
    public void ValidCommand_Passes() =>
        _validator.Validate(Valid(roles: ["Admin", "Gudang"])).IsValid.Should().BeTrue();

    [Fact]
    public void BlankUserId_Fails()
    {
        var result = _validator.Validate(Valid(userId: ""));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRolesCommand.UserId));
    }

    [Fact]
    public void EmptyRoles_Fails()
    {
        var result = _validator.Validate(new UpdateUserRolesCommand("user-1", []));
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateUserRolesCommand.Roles));
    }

    [Theory]
    [InlineData("SuperAdmin")]   // privilege escalation is not allowed here
    [InlineData("Manager")]      // unknown role
    public void UnassignableRole_Fails(string role)
    {
        var result = _validator.Validate(Valid(roles: [role]));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void MixOfValidAndInvalidRoles_Fails() =>
        _validator.Validate(Valid(roles: ["Admin", "SuperAdmin"])).IsValid.Should().BeFalse();
}
