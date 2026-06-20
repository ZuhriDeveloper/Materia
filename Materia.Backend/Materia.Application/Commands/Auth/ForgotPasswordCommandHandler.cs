using Materia.Application.Contracts.Auth;
using Materia.Application.Contracts.Email;
using Microsoft.Extensions.Logging;

namespace Materia.Application.Commands.Auth;

/// <summary>
/// Issues a password-reset email. Always completes successfully regardless of whether the
/// email is registered (no account enumeration) and treats email-delivery failure as
/// non-fatal — the caller returns the same generic response either way.
/// </summary>
public class ForgotPasswordCommandHandler(
    IUserAccountService accounts,
    IAccountLinkBuilder links,
    IEmailSender emailSender,
    ILogger<ForgotPasswordCommandHandler> logger)
{
    public async Task HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var info = await accounts.CreatePasswordResetTokenAsync(command.Email, cancellationToken);
        if (info is null)
            return; // Unknown email — say nothing, do nothing.

        var link = links.BuildPasswordResetLink(info.Email, info.EncodedToken);
        var (subject, body) = AccountEmailTemplates.PasswordReset(info.FullName, link);

        try
        {
            await emailSender.SendAsync(info.Email, info.FullName, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send password reset email to {Email}", info.Email);
        }
    }
}
