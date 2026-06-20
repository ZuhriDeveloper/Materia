using FluentAssertions;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;
using Microsoft.Extensions.Logging.Abstractions;

namespace Materia.Tests.Auth;

/// <summary>
/// The force-reset use case must (1) email the freshly minted temporary password to the user,
/// (2) fail (and send nothing) for an unknown / non-store user, and (3) report a delivery
/// failure as an error — the temp password is never shown in the UI, so a silent failure would
/// strand the account.
/// </summary>
public class AdminResetPasswordCommandHandlerTests
{
    private readonly FakeUserAdminService _users = new();
    private readonly FakeEmailSender _email = new();

    private AdminResetPasswordCommandHandler CreateHandler() =>
        new(_users, new FakeAccountLinkBuilder(), _email,
            NullLogger<AdminResetPasswordCommandHandler>.Instance);

    [Fact]
    public async Task KnownUser_EmailsTemporaryPassword()
    {
        _users.ResetInfoToReturn =
            new AdminPasswordResetInfo("user-1", "budi@materia.local", "Budi", "Temp1234ABcd");

        var result = await CreateHandler().HandleAsync(new AdminResetPasswordCommand("user-1"));

        result.Succeeded.Should().BeTrue();
        _email.Sent.Should().ContainSingle();
        var sent = _email.Sent[0];
        sent.ToEmail.Should().Be("budi@materia.local");
        sent.HtmlBody.Should().Contain("Temp1234ABcd");                  // the temp password itself
        sent.HtmlBody.Should().Contain("account/change-password");       // the change-password link
    }

    [Fact]
    public async Task UnknownUser_SendsNothing_AndFails()
    {
        _users.ResetInfoToReturn = null; // not found / not store-scoped

        var result = await CreateHandler().HandleAsync(new AdminResetPasswordCommand("ghost"));

        result.Succeeded.Should().BeFalse();
        _email.Sent.Should().BeEmpty();
        _users.ResetRequestedFor.Should().Be("ghost");
    }

    [Fact]
    public async Task EmailDeliveryFailure_IsReportedAsError()
    {
        _users.ResetInfoToReturn =
            new AdminPasswordResetInfo("user-1", "budi@materia.local", "Budi", "Temp1234ABcd");
        _email.ThrowOnSend = true;

        var result = await CreateHandler().HandleAsync(new AdminResetPasswordCommand("user-1"));

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }
}
