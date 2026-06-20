using Materia.Application.Contracts.Auth;
using Materia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Materia.Infrastructure.Auth;

/// <summary>
/// Platform-admin operations on store-scoped staff accounts over ASP.NET Identity. Every
/// operation guards on <see cref="ApplicationUser.StoreId"/> being non-null so a platform
/// SuperAdmin can never be listed, re-roled, or reset through the per-store management UI.
/// </summary>
public class UserAdminService(UserManager<ApplicationUser> userManager) : IUserAdminService
{
    public async Task<IReadOnlyList<UserSummary>> ListUsersAsync(
        Guid? storeId, CancellationToken cancellationToken = default)
    {
        var query = userManager.Users.Where(u => u.StoreId != null);
        if (storeId is Guid id)
            query = query.Where(u => u.StoreId == id);

        var users = await query
            .OrderBy(u => u.Email)
            .ToListAsync(cancellationToken);

        // Roles live in a join table; fetch per user (admin screen, modest cardinality).
        var summaries = new List<UserSummary>(users.Count);
        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            summaries.Add(new UserSummary(
                user.Id, user.Email!, user.FullName, user.StoreId, roles.ToList(), user.EmailConfirmed));
        }

        return summaries;
    }

    public async Task<AccountOperationResult> ReplaceRolesAsync(
        string userId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.StoreId is null)
            return AccountOperationResult.Fail("User not found.");

        var desired = roles.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var current = await userManager.GetRolesAsync(user);

        var toRemove = current.Except(desired, StringComparer.OrdinalIgnoreCase).ToList();
        var toAdd = desired.Except(current, StringComparer.OrdinalIgnoreCase).ToList();

        if (toRemove.Count > 0)
        {
            var removed = await userManager.RemoveFromRolesAsync(user, toRemove);
            if (!removed.Succeeded)
                return ToResult(removed);
        }

        if (toAdd.Count > 0)
        {
            var added = await userManager.AddToRolesAsync(user, toAdd);
            if (!added.Succeeded)
                return ToResult(added);
        }

        return AccountOperationResult.Ok();
    }

    public async Task<AdminPasswordResetInfo?> ResetPasswordToTemporaryAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || user.StoreId is null)
            return null;

        var tempPassword = TemporaryPasswordGenerator.Generate();

        // Reset via a freshly issued token so it works whether or not a password is set, and
        // without needing the old one. The generator guarantees the policy is met, so a failure
        // here is not an expected outcome.
        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, tempPassword);
        if (!result.Succeeded)
            return null;

        return new AdminPasswordResetInfo(user.Id, user.Email!, user.FullName, tempPassword);
    }

    private static AccountOperationResult ToResult(IdentityResult result) =>
        result.Succeeded
            ? AccountOperationResult.Ok()
            : AccountOperationResult.Fail(result.Errors.Select(e => e.Description));
}
