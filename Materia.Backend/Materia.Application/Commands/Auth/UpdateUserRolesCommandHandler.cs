using Materia.Application.Contracts.Auth;

namespace Materia.Application.Commands.Auth;

/// <summary>
/// Replaces a store-scoped user's roles. The set of assignable roles is enforced by
/// <see cref="Materia.Application.Validators.Auth.UpdateUserRolesCommandValidator"/>; the
/// store-scope guard lives in <see cref="IUserAdminService.ReplaceRolesAsync"/>.
/// </summary>
public class UpdateUserRolesCommandHandler(IUserAdminService users)
{
    public Task<AccountOperationResult> HandleAsync(
        UpdateUserRolesCommand command, CancellationToken cancellationToken = default)
        => users.ReplaceRolesAsync(command.UserId, command.Roles, cancellationToken);
}
