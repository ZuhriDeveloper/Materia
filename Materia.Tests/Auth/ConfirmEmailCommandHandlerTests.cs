using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;

namespace Materia.Tests.Auth;

public class ConfirmEmailCommandHandlerTests
{
    private readonly FakeUserAccountService _accounts = new();

    [Fact]
    public async Task ForwardsUserIdAndToken()
    {
        var handler = new ConfirmEmailCommandHandler(_accounts);

        await handler.HandleAsync(new ConfirmEmailCommand("user-1", "tok-9"));

        _accounts.ConfirmEmailCall.Should().Be(("user-1", "tok-9"));
    }

    [Fact]
    public async Task PropagatesFailure()
    {
        _accounts.ResultToReturn = AccountOperationResult.Fail("Invalid or expired confirmation link.");
        var handler = new ConfirmEmailCommandHandler(_accounts);

        var result = await handler.HandleAsync(new ConfirmEmailCommand("user-1", "bad"));

        result.Succeeded.Should().BeFalse();
    }
}
