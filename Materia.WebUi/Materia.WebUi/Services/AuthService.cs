using System.Net.Http.Json;

namespace Materia.WebUi.Services;

public class AuthService(HttpClient httpClient)
{
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/login", new { email, password });

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                return LoginResult.Fail(error?.Message ?? "Invalid credentials.");
            }

            var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
            return LoginResult.Ok(result!);
        }
        catch
        {
            return LoginResult.Fail("Unable to connect to the server. Please try again.");
        }
    }

    // ── Account recovery (anonymous endpoints) ───────────────────────────────────

    public async Task<AccountResult> ForgotPasswordAsync(string email)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/forgot-password", new { email });
            return await AccountResult.FromResponseAsync(response);
        }
        catch
        {
            return AccountResult.Failure(["Tidak dapat terhubung ke server. Silakan coba lagi."]);
        }
    }

    public async Task<AccountResult> ResetPasswordAsync(string email, string token, string newPassword)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/reset-password",
                new { email, token, newPassword });
            return await AccountResult.FromResponseAsync(response);
        }
        catch
        {
            return AccountResult.Failure(["Tidak dapat terhubung ke server. Silakan coba lagi."]);
        }
    }

    public async Task<AccountResult> ConfirmEmailAsync(string userId, string token)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync("api/auth/confirm-email",
                new { userId, token });
            return await AccountResult.FromResponseAsync(response);
        }
        catch
        {
            return AccountResult.Failure(["Tidak dapat terhubung ke server. Silakan coba lagi."]);
        }
    }
}

public record LoginResponse(string Token, string Email, string? FullName, IReadOnlyList<string> Roles);
public record ErrorResponse(string Message);

public class LoginResult
{
    public bool Succeeded { get; private init; }
    public string? ErrorMessage { get; private init; }
    public LoginResponse? Data { get; private init; }

    public static LoginResult Ok(LoginResponse data) => new() { Succeeded = true, Data = data };
    public static LoginResult Fail(string message) => new() { Succeeded = false, ErrorMessage = message };
}

/// <summary>Result of an account action; carries the server's success message or its error list.</summary>
public record AccountResult(bool Ok, List<string> Errors, string? Message)
{
    public static AccountResult Success(string? message) => new(true, [], message);
    public static AccountResult Failure(List<string> errors) => new(false, errors, null);

    public static async Task<AccountResult> FromResponseAsync(HttpResponseMessage response)
    {
        AccountResponseBody? body = null;
        try { body = await response.Content.ReadFromJsonAsync<AccountResponseBody>(); }
        catch { /* empty / non-JSON body */ }

        if (response.IsSuccessStatusCode)
            return Success(body?.Message);

        if (body?.Errors is { Count: > 0 })
            return Failure(body.Errors);

        return Failure([body?.Message ?? response.ReasonPhrase ?? "Terjadi kesalahan."]);
    }

    private record AccountResponseBody(string? Message, List<string>? Errors);
}
