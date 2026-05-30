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
