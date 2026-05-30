using Materia.Application.Contracts.Auth;

namespace Materia.Application.Commands.Auth;

public class LoginCommandHandler(
    IUserAuthRepository userRepository,
    ITokenService tokenService)
{
    public async Task<LoginResult> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await userRepository.FindByEmailAsync(command.Email, cancellationToken);
        if (user is null)
            return LoginResult.Fail("Invalid email or password.");

        if (await userRepository.IsLockedOutAsync(user.Id))
            return LoginResult.Fail("Account is temporarily locked. Please try again later.");

        var passwordValid = await userRepository.CheckPasswordAsync(user.Id, command.Password);
        if (!passwordValid)
        {
            await userRepository.RecordFailedAccessAsync(user.Id);
            return LoginResult.Fail("Invalid email or password.");
        }

        await userRepository.ResetAccessFailedCountAsync(user.Id);

        var token = tokenService.GenerateToken(user);
        return LoginResult.Ok(token, user.Email, user.FullName);
    }
}
