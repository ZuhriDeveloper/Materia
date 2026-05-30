namespace Materia.Application.Contracts.Auth;

public interface ITokenService
{
    string GenerateToken(UserAuthInfo user, IReadOnlyList<string> roles);
}
