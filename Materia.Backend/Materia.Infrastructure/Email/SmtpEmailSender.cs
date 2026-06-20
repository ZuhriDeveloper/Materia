using MailKit.Net.Smtp;
using MailKit.Security;
using Materia.Application.Contracts.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Materia.Infrastructure.Email;

/// <summary>
/// SMTP email sender (MailKit). Reads the <c>Email</c> configuration section directly,
/// matching the project's configuration style (see JwtTokenService). In development this
/// points at the Mailpit container wired up by the Aspire AppHost; in production it points
/// at a real SMTP provider whose credentials come from configuration / environment.
/// </summary>
public class SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(
        string toEmail, string? toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var section = configuration.GetSection("Email");
        var host = section["Host"] ?? "localhost";
        var port = int.TryParse(section["Port"], out var p) ? p : 1025;
        var useStartTls = bool.TryParse(section["UseStartTls"], out var t) && t;
        var user = section["User"];
        var password = section["Password"];
        var fromAddress = section["FromAddress"] ?? "no-reply@materia.local";
        var fromName = section["FromName"] ?? "Materia POS";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(toName ?? toEmail, toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();
        // StartTls for real providers (587); None for the local Mailpit catcher (1025).
        var socketOptions = useStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.None;
        await client.ConnectAsync(host, port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(user))
            await client.AuthenticateAsync(user, password, cancellationToken);

        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Sent email '{Subject}' to {Email} via {Host}:{Port}", subject, toEmail, host, port);
    }
}
