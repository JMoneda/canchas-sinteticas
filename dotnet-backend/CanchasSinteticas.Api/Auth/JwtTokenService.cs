using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Entities;
using Microsoft.IdentityModel.Tokens;

namespace CanchasSinteticas.Api.Auth;

/// <summary>
/// Emisión de tokens JWT firmados con HMAC-SHA256 a partir de la configuración.
/// </summary>
public class JwtTokenService(IConfiguration configuration) : ITokenService
{
    /// <inheritdoc/>
    public string CreateToken(User user)
    {
        var section = configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(section["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresHours = int.TryParse(section["ExpiresHours"], out var hours) ? hours : 8;

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: section["Issuer"],
            audience: section["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(expiresHours),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
