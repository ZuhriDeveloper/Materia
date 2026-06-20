namespace Materia.Application.Contracts.Email;

/// <summary>
/// Sends a single transactional email. Implemented in Infrastructure over SMTP
/// (Mailpit in development, a real SMTP provider in production).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(
        string toEmail,
        string? toName,
        string subject,
        string htmlBody,
        CancellationToken cancellationToken = default);
}
