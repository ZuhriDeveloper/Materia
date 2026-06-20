namespace Materia.Application.Contracts.Auth;

/// <summary>
/// Administrative operations a platform SuperAdmin performs on store-scoped staff accounts:
/// listing them, replacing their role set, and forcing a password reset. Implemented over
/// ASP.NET Identity in Infrastructure — these are plain Identity operations, not event-sourced
/// aggregates.
/// </summary>
public interface IUserAdminService
{
    /// <summary>
    /// Lists store-scoped users (<c>StoreId != null</c>), optionally filtered to a single store.
    /// Platform SuperAdmins (no store) are never included.
    /// </summary>
    Task<IReadOnlyList<UserSummary>> ListUsersAsync(
        Guid? storeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces a store-scoped user's full role set with <paramref name="roles"/>. Fails when the
    /// user does not exist or is not store-scoped (platform SuperAdmins cannot be edited here).
    /// </summary>
    Task<AccountOperationResult> ReplaceRolesAsync(
        string userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a policy-compliant temporary password, sets it on the account, and returns the
    /// details needed to email it — or <c>null</c> when the user does not exist or is not
    /// store-scoped. The temporary password is returned only so the caller can email it; it is
    /// never persisted in plaintext.
    /// </summary>
    Task<AdminPasswordResetInfo?> ResetPasswordToTemporaryAsync(
        string userId, CancellationToken cancellationToken = default);
}

/// <summary>A store-scoped user as seen by the platform admin directory.</summary>
public record UserSummary(
    string Id,
    string Email,
    string? FullName,
    Guid? StoreId,
    IReadOnlyList<string> Roles,
    bool EmailConfirmed);

/// <summary>Carries a freshly generated temporary password so the caller can email it.</summary>
public record AdminPasswordResetInfo(string UserId, string Email, string? FullName, string TemporaryPassword);
