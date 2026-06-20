using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;

namespace Materia.Tests.Auth;

public class ResetPasswordCommandHandlerTests
{
    private readonly FakeUserAccountService _accounts = new();

    [Fact]
    public async Task ForwardsArgumentsToAccountService()
    {
        var handler = new ResetPasswordCommandHandler(_accounts);

        await handler.HandleAsync(new ResetPasswordCommand("budi@materia.local", "tok-123", "NewPass1"));

        _accounts.ResetPasswordCall.Should().Be(("budi@materia.local", "tok-123", "NewPass1"));
    }

    [Fact]
    public async Task PropagatesFailureFromAccountService()
    {
        _accounts.ResultToReturn = AccountOperationResult.Fail("Invalid or expired password reset link.");
        var handler = new ResetPasswordCommandHandler(_accounts);

        var result = await handler.HandleAsync(new ResetPasswordCommand("budi@materia.local", "bad", "NewPass1"));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain("Invalid or expired password reset link.");
    }
}
