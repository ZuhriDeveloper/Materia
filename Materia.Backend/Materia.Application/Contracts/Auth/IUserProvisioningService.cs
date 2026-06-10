namespace Materia.Application.Contracts.Auth;

/// <summary>
/// Provisions store-scoped user accounts. Used by the platform SuperAdmin to create the
/// staff (Admin / Cashier / Helper) of a specific store.
/// </summary>
public interface IUserProvisioningService
{
    Task<UserProvisioningResult> ProvisionAsync(
        string email,
        string fullName,
        string password,
        string role,
        Guid? storeId,
        CancellationToken ct = default);
}

public record UserProvisioningResult(bool Succeeded, string? UserId, IReadOnlyList<string> Errors);
