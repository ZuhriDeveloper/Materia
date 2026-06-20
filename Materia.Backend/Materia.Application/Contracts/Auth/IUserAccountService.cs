namespace Materia.Application.Contracts.Auth;

/// <summary>
/// Self-service account operations on top of ASP.NET Identity: changing a known
/// password, and the token-based email-confirmation / password-reset flows.
/// Tokens are returned already URL-safe (Base64Url) so they can be embedded in links.
/// </summary>
public interface IUserAccountService
{
    /// <summary>Changes the password of a signed-in user (requires the current password).</summary>
    Task<AccountOperationResult> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a password-reset token for the account with this email, or <c>null</c>
    /// when no such account exists (so the caller can avoid leaking which emails are registered).
    /// </summary>
    Task<PasswordResetTokenInfo?> CreatePasswordResetTokenAsync(
        string email, CancellationToken cancellationToken = default);

    /// <summary>Resets a password using an email + reset token previously issued for it.</summary>
    Task<AccountOperationResult> ResetPasswordAsync(
        string email, string encodedToken, string newPassword, CancellationToken cancellationToken = default);

    /// <summary>Creates an email-confirmation token for a user, or <c>null</c> if the user does not exist.</summary>
    Task<EmailConfirmationTokenInfo?> CreateEmailConfirmationTokenAsync(
        string userId, CancellationToken cancellationToken = default);

    /// <summary>Confirms a user's email using a userId + confirmation token.</summary>
    Task<AccountOperationResult> ConfirmEmailAsync(
        string userId, string encodedToken, CancellationToken cancellationToken = default);
}

/// <summary>Outcome of an account mutation, carrying Identity error descriptions on failure.</summary>
public record AccountOperationResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static AccountOperationResult Ok() => new(true, []);
    public static AccountOperationResult Fail(string error) => new(false, [error]);
    public static AccountOperationResult Fail(IEnumerable<string> errors) => new(false, errors.ToList());
}

public record PasswordResetTokenInfo(string UserId, string Email, string? FullName, string EncodedToken);
public record EmailConfirmationTokenInfo(string UserId, string Email, string? FullName, string EncodedToken);
