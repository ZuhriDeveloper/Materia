namespace Materia.Application.Contracts.Auth;

public interface IUserAuthRepository
{
    Task<UserAuthInfo?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> CheckPasswordAsync(string userId, string password);
    Task<bool> IsLockedOutAsync(string userId);
    Task RecordFailedAccessAsync(string userId);
    Task ResetAccessFailedCountAsync(string userId);
    Task<IReadOnlyList<string>> GetRolesAsync(string userId, CancellationToken cancellationToken = default);
}

public record UserAuthInfo(string Id, string Email, string? FullName);
