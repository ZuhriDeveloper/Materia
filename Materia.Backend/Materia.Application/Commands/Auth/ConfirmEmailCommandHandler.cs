using Materia.Application.Contracts.Auth;

namespace Materia.Application.Commands.Auth;

public class ConfirmEmailCommandHandler(IUserAccountService accounts)
{
    public Task<AccountOperationResult> HandleAsync(ConfirmEmailCommand command, CancellationToken cancellationToken = default)
        => accounts.ConfirmEmailAsync(command.UserId, command.Token, cancellationToken);
}
