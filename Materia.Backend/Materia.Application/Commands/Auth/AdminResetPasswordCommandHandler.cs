using Materia.Application.Contracts.Auth;
using Materia.Application.Contracts.Email;
using Microsoft.Extensions.Logging;

namespace Materia.Application.Commands.Auth;

/// <summary>
/// Forces a password reset: generates a temporary password, sets it on the account, and emails
/// the plaintext password to the user with a reminder to change it soon. Unlike the best-effort
/// confirmation email, delivery here is the whole mechanism — the temporary password is never
/// shown in the UI — so a send failure is reported as an error. Retrying simply mints a fresh
/// temporary password, so a failed send leaves no usable-but-unknown credential dangling.
/// </summary>
public class AdminResetPasswordCommandHandler(
    IUserAdminService users,
    IAccountLinkBuilder links,
    IEmailSender emailSender,
    ILogger<AdminResetPasswordCommandHandler> logger)
{
    public async Task<AccountOperationResult> HandleAsync(
        AdminResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        var info = await users.ResetPasswordToTemporaryAsync(command.UserId, cancellationToken);
        if (info is null)
            return AccountOperationResult.Fail("User not found.");

        var link = links.BuildChangePasswordLink();
        var (subject, body) = AccountEmailTemplates.TemporaryPassword(info.FullName, info.TemporaryPassword, link);

        try
        {
            await emailSender.SendAsync(info.Email, info.FullName, subject, body, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send temporary-password email to {Email}", info.Email);
            return AccountOperationResult.Fail(
                "Kata sandi sementara dibuat, tetapi email gagal dikirim. Silakan coba lagi.");
        }

        return AccountOperationResult.Ok();
    }
}
