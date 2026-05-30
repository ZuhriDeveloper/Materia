using Materia.Application.Contracts.Auth;

namespace Materia.Application.Contracts.Auth;

public interface ITokenService
{
    string GenerateToken(UserAuthInfo user);
}
