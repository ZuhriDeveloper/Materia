namespace Materia.Application.Contracts.Auth;

/// <summary>
/// Builds the absolute Web UI links embedded in account emails (email confirmation
/// and password reset). The base URL points at the Blazor app, not the API.
/// </summary>
public interface IAccountLinkBuilder
{
    string BuildEmailConfirmationLink(string userId, string encodedToken);
    string BuildPasswordResetLink(string email, string encodedToken);

    /// <summary>Link to the Web UI Change Password page (used in the temporary-password email).</summary>
    string BuildChangePasswordLink();
}
