using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;

namespace Materia.Tests.Auth;

public class UpdateUserRolesCommandHandlerTests
{
    private readonly FakeUserAdminService _users = new();
    private UpdateUserRolesCommandHandler CreateHandler() => new(_users);

    [Fact]
    public async Task ForwardsUserIdAndRolesToService()
    {
        await CreateHandler().HandleAsync(new UpdateUserRolesCommand("user-1", ["Admin", "Gudang"]));

        _users.ReplaceRolesCall.Should().NotBeNull();
        _users.ReplaceRolesCall!.Value.UserId.Should().Be("user-1");
        _users.ReplaceRolesCall!.Value.Roles.Should().BeEquivalentTo(["Admin", "Gudang"]);
    }

    [Fact]
    public async Task ReturnsServiceSuccess()
    {
        _users.ReplaceRolesResult = AccountOperationResult.Ok();

        var result = await CreateHandler().HandleAsync(new UpdateUserRolesCommand("user-1", ["Admin"]));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task SurfacesServiceFailure()
    {
        _users.ReplaceRolesResult = AccountOperationResult.Fail("User not found.");

        var result = await CreateHandler().HandleAsync(new UpdateUserRolesCommand("ghost", ["Admin"]));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("User not found.");
    }
}
