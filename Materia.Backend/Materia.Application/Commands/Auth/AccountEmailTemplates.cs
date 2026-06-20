using System.Net;

namespace Materia.Application.Commands.Auth;

/// <summary>
/// Builds the subject + HTML body for account emails. Copy is in Indonesian to match
/// the rest of the UI. Kept here (Application) so handlers stay free of presentation detail.
/// </summary>
public static class AccountEmailTemplates
{
    public static (string Subject, string HtmlBody) PasswordReset(string? fullName, string resetLink)
    {
        const string subject = "Atur Ulang Kata Sandi — Materia";
        var body = Wrap($"""
            <h2 style="margin:0 0 16px">Atur Ulang Kata Sandi</h2>
            <p>Halo {Greeting(fullName)},</p>
            <p>Kami menerima permintaan untuk mengatur ulang kata sandi akun Materia Anda.
               Klik tombol di bawah untuk membuat kata sandi baru.</p>
            {Button("Atur Ulang Kata Sandi", resetLink)}
            <p style="color:#6c757d;font-size:13px">Tautan ini akan kedaluwarsa dalam beberapa jam.
               Jika Anda tidak meminta perubahan ini, abaikan email ini — kata sandi Anda tetap aman.</p>
            {FallbackLink(resetLink)}
            """);
        return (subject, body);
    }

    public static (string Subject, string HtmlBody) TemporaryPassword(
        string? fullName, string temporaryPassword, string changePasswordLink)
    {
        const string subject = "Kata Sandi Sementara — Materia";
        var body = Wrap($"""
            <h2 style="margin:0 0 16px">Kata Sandi Sementara</h2>
            <p>Halo {Greeting(fullName)},</p>
            <p>Administrator telah mengatur ulang kata sandi akun Materia Anda. Gunakan kata sandi
               sementara berikut untuk masuk:</p>
            <p style="margin:24px 0;text-align:center">
              <span style="display:inline-block;font-family:Consolas,'Courier New',monospace;
                           font-size:20px;font-weight:700;letter-spacing:2px;background:#f1f3f5;
                           color:#212529;padding:12px 20px;border-radius:6px">{WebUtility.HtmlEncode(temporaryPassword)}</span>
            </p>
            <p><strong>Demi keamanan, segera ganti kata sandi ini setelah Anda masuk.</strong></p>
            {Button("Ganti Kata Sandi", changePasswordLink)}
            <p style="color:#6c757d;font-size:13px">Jika Anda tidak mengenali permintaan ini,
               segera hubungi administrator Anda.</p>
            {FallbackLink(changePasswordLink)}
            """);
        return (subject, body);
    }

    public static (string Subject, string HtmlBody) EmailConfirmation(string? fullName, string confirmLink)
    {
        const string subject = "Konfirmasi Alamat Email — Materia";
        var body = Wrap($"""
            <h2 style="margin:0 0 16px">Konfirmasi Email Anda</h2>
            <p>Halo {Greeting(fullName)},</p>
            <p>Akun Materia telah dibuat untuk alamat email ini. Klik tombol di bawah untuk
               mengonfirmasi bahwa alamat email Anda valid.</p>
            {Button("Konfirmasi Email", confirmLink)}
            <p style="color:#6c757d;font-size:13px">Jika Anda tidak mengenali akun ini, abaikan email ini.</p>
            {FallbackLink(confirmLink)}
            """);
        return (subject, body);
    }

    private static string Greeting(string? fullName) =>
        string.IsNullOrWhiteSpace(fullName) ? "Pengguna Materia" : WebUtility.HtmlEncode(fullName);

    private static string Button(string text, string href) =>
        $"""
        <p style="margin:24px 0">
          <a href="{WebUtility.HtmlEncode(href)}"
             style="background:#0d6efd;color:#fff;text-decoration:none;
                    padding:12px 20px;border-radius:6px;display:inline-block;font-weight:600">
            {text}
          </a>
        </p>
        """;

    private static string FallbackLink(string href) =>
        $"""
        <p style="color:#6c757d;font-size:13px">Atau salin tautan ini ke peramban Anda:<br>
          <span style="word-break:break-all">{WebUtility.HtmlEncode(href)}</span></p>
        """;

    private static string Wrap(string inner) =>
        $"""
        <div style="font-family:Segoe UI,Arial,sans-serif;max-width:520px;margin:0 auto;
                    color:#212529;line-height:1.5">
          {inner}
          <hr style="border:none;border-top:1px solid #e9ecef;margin:24px 0">
          <p style="color:#adb5bd;font-size:12px;margin:0">Materia — Point of Sale</p>
        </div>
        """;
}
