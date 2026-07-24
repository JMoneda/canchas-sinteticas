using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Application.Abstractions;

/// <summary>
/// Servicio de emisión de tokens de acceso (JWT).
/// </summary>
public interface ITokenService
{
    /// <summary>Crea un token JWT firmado para el usuario indicado.</summary>
    string CreateToken(User user);
}
