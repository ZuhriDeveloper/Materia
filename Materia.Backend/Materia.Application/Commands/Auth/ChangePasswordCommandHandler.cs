using Materia.Application.Contracts.Auth;

namespace Materia.Application.Commands.Auth;

public class ChangePasswordCommandHandler(IUserAccountService accounts)
{
    public Task<AccountOperationResult> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
        => accounts.ChangePasswordAsync(command.UserId, command.CurrentPassword, command.NewPassword, cancellationToken);
}
