using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;

namespace Materia.Tests.Auth;

public class ChangePasswordCommandHandlerTests
{
    private readonly FakeUserAccountService _accounts = new();

    [Fact]
    public async Task DelegatesToAccountService_WithUserId()
    {
        var handler = new ChangePasswordCommandHandler(_accounts);

        var result = await handler.HandleAsync(new ChangePasswordCommand("user-7", "OldPass1", "NewPass1"));

        _accounts.ChangePasswordUserId.Should().Be("user-7");
        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task PropagatesFailure()
    {
        _accounts.ResultToReturn = AccountOperationResult.Fail("Incorrect password.");
        var handler = new ChangePasswordCommandHandler(_accounts);

        var result = await handler.HandleAsync(new ChangePasswordCommand("user-7", "wrong", "NewPass1"));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Incorrect password.");
    }
}
