namespace Materia.Application.Commands.Auth;

public record LoginResult
{
    public bool Succeeded { get; init; }
    public string? Token { get; init; }
    public string? Email { get; init; }
    public string? FullName { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
    public string? ErrorMessage { get; init; }

    public static LoginResult Ok(string token, string email, string? fullName, IReadOnlyList<string> roles) =>
        new() { Succeeded = true, Token = token, Email = email, FullName = fullName, Roles = roles };

    public static LoginResult Fail(string message) =>
        new() { Succeeded = false, ErrorMessage = message };
}
