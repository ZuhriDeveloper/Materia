using Materia.Application.Contracts.Auth;

namespace Materia.Application.Commands.Auth;

public class ResetPasswordCommandHandler(IUserAccountService accounts)
{
    public Task<AccountOperationResult> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
        => accounts.ResetPasswordAsync(command.Email, command.Token, command.NewPassword, cancellationToken);
}
