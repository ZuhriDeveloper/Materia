using Materia.Application.Contracts.Auth;
using Materia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace Materia.Infrastructure.Auth;

public class UserAuthRepository(UserManager<ApplicationUser> userManager) : IUserAuthRepository
{
    public async Task<UserAuthInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        return user is null ? null : new UserAuthInfo(user.Id, user.Email!, user.FullName);
    }

    public async Task<bool> CheckPasswordAsync(string userId, string password)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.CheckPasswordAsync(user, password);
    }

    public async Task<bool> IsLockedOutAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user is not null && await userManager.IsLockedOutAsync(user);
    }

    public async Task RecordFailedAccessAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
            await userManager.AccessFailedAsync(user);
    }

    public async Task ResetAccessFailedCountAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is not null)
            await userManager.ResetAccessFailedCountAsync(user);
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return [];
        return (IReadOnlyList<string>)await userManager.GetRolesAsync(user);
    }
}
