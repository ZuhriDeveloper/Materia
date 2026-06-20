using Materia.Application.Contracts.Auth;

namespace Materia.Tests.Auth;

/// <summary>
/// Configurable <see cref="IUserAccountService"/> double. Records calls and returns
/// whatever the test arranges, so Application handlers can be exercised without Identity.
/// </summary>
public class FakeUserAccountService : IUserAccountService
{
    // Arrange
    public PasswordResetTokenInfo? ResetTokenToReturn { get; set; }
    public EmailConfirmationTokenInfo? ConfirmTokenToReturn { get; set; }
    public AccountOperationResult ResultToReturn { get; set; } = AccountOperationResult.Ok();

    // Assert
    public string? ChangePasswordUserId { get; private set; }
    public (string Email, string Token, string NewPassword)? ResetPasswordCall { get; private set; }
    public (string UserId, string Token)? ConfirmEmailCall { get; private set; }
    public string? PasswordResetRequestedFor { get; private set; }

    public Task<AccountOperationResult> ChangePasswordAsync(string userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        ChangePasswordUserId = userId;
        return Task.FromResult(ResultToReturn);
    }

    public Task<PasswordResetTokenInfo?> CreatePasswordResetTokenAsync(string email, CancellationToken cancellationToken = default)
    {
        PasswordResetRequestedFor = email;
        return Task.FromResult(ResetTokenToReturn);
    }

    public Task<AccountOperationResult> ResetPasswordAsync(string email, string encodedToken, string newPassword, CancellationToken cancellationToken = default)
    {
        ResetPasswordCall = (email, encodedToken, newPassword);
        return Task.FromResult(ResultToReturn);
    }

    public Task<EmailConfirmationTokenInfo?> CreateEmailConfirmationTokenAsync(string userId, CancellationToken cancellationToken = default)
        => Task.FromResult(ConfirmTokenToReturn);

    public Task<AccountOperationResult> ConfirmEmailAsync(string userId, string encodedToken, CancellationToken cancellationToken = default)
    {
        ConfirmEmailCall = (userId, encodedToken);
        return Task.FromResult(ResultToReturn);
    }
}
