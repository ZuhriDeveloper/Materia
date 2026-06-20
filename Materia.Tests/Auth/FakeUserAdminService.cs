using Materia.Application.Contracts.Auth;

namespace Materia.Tests.Auth;

/// <summary>
/// Configurable <see cref="IUserAdminService"/> double. Records calls and returns whatever the
/// test arranges, so Application handlers can be exercised without ASP.NET Identity.
/// </summary>
public class FakeUserAdminService : IUserAdminService
{
    // Arrange
    public IReadOnlyList<UserSummary> UsersToReturn { get; set; } = [];
    public AccountOperationResult ReplaceRolesResult { get; set; } = AccountOperationResult.Ok();
    public AdminPasswordResetInfo? ResetInfoToReturn { get; set; }

    // Assert
    public Guid? ListStoreIdFilter { get; private set; }
    public bool ListCalled { get; private set; }
    public (string UserId, IReadOnlyList<string> Roles)? ReplaceRolesCall { get; private set; }
    public string? ResetRequestedFor { get; private set; }

    public Task<IReadOnlyList<UserSummary>> ListUsersAsync(Guid? storeId, CancellationToken cancellationToken = default)
    {
        ListCalled = true;
        ListStoreIdFilter = storeId;
        return Task.FromResult(UsersToReturn);
    }

    public Task<AccountOperationResult> ReplaceRolesAsync(
        string userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        ReplaceRolesCall = (userId, roles);
        return Task.FromResult(ReplaceRolesResult);
    }

    public Task<AdminPasswordResetInfo?> ResetPasswordToTemporaryAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        ResetRequestedFor = userId;
        return Task.FromResult(ResetInfoToReturn);
    }
}
