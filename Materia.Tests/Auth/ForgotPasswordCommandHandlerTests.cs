using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;
using Microsoft.Extensions.Logging.Abstractions;

namespace Materia.Tests.Auth;

/// <summary>
/// The forgot-password use case must (1) email a reset link only when the account exists,
/// (2) never reveal whether an email is registered, and (3) treat SMTP failure as non-fatal.
/// </summary>
public class ForgotPasswordCommandHandlerTests
{
    private readonly FakeUserAccountService _accounts = new();
    private readonly FakeEmailSender _email = new();
    private ForgotPasswordCommandHandler CreateHandler() =>
        new(_accounts, new FakeAccountLinkBuilder(), _email, NullLogger<ForgotPasswordCommandHandler>.Instance);

    [Fact]
    public async Task KnownEmail_SendsResetEmailWithLink()
    {
        _accounts.ResetTokenToReturn =
            new PasswordResetTokenInfo("user-1", "budi@materia.local", "Budi", "tok-123");

        await CreateHandler().HandleAsync(new ForgotPasswordCommand("budi@materia.local"));

        _email.Sent.Should().ContainSingle();
        var sent = _email.Sent[0];
        sent.ToEmail.Should().Be("budi@materia.local");
        sent.HtmlBody.Should().Contain("tok-123");        // link carries the reset token
        sent.HtmlBody.Should().Contain("reset-password");
    }

    [Fact]
    public async Task UnknownEmail_SendsNothing_AndDoesNotThrow()
    {
        _accounts.ResetTokenToReturn = null; // no such user

        await CreateHandler().HandleAsync(new ForgotPasswordCommand("ghost@materia.local"));

        _email.Sent.Should().BeEmpty();
        _accounts.PasswordResetRequestedFor.Should().Be("ghost@materia.local");
    }

    [Fact]
    public async Task EmailDeliveryFailure_IsSwallowed()
    {
        _accounts.ResetTokenToReturn =
            new PasswordResetTokenInfo("user-1", "budi@materia.local", "Budi", "tok-123");
        _email.ThrowOnSend = true;

        var act = async () => await CreateHandler().HandleAsync(new ForgotPasswordCommand("budi@materia.local"));

        await act.Should().NotThrowAsync();
    }
}
