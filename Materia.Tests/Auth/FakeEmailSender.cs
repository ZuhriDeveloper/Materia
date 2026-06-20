using Materia.Application.Contracts.Email;

namespace Materia.Tests.Auth;

/// <summary>In-memory <see cref="IEmailSender"/> recording every send; can be armed to throw.</summary>
public class FakeEmailSender : IEmailSender
{
    public record SentEmail(string ToEmail, string? ToName, string Subject, string HtmlBody);

    public List<SentEmail> Sent { get; } = [];
    public bool ThrowOnSend { get; set; }

    public Task SendAsync(string toEmail, string? toName, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (ThrowOnSend)
            throw new InvalidOperationException("SMTP unavailable");

        Sent.Add(new SentEmail(toEmail, toName, subject, htmlBody));
        return Task.CompletedTask;
    }
}
