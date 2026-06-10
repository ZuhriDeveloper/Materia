using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Materia.Application.Contracts.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Materia.Infrastructure.Auth;

public class JwtTokenService(IConfiguration configuration) : ITokenService
{
    public string GenerateToken(UserAuthInfo user, IReadOnlyList<string> roles)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSection["Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("fullName", user.FullName ?? string.Empty),
        };

        // Store scope (multi-tenant). Absent for a platform SuperAdmin, who is unscoped.
        if (user.StoreId is Guid storeId)
            claims.Add(new Claim("storeId", storeId.ToString()));

        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: jwtSection["Issuer"],
            audience: jwtSection["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
