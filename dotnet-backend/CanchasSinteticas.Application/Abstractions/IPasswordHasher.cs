namespace CanchasSinteticas.Application.Abstractions;

/// <summary>
/// Servicio de hashing de contraseñas.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Genera el hash de una contraseña en texto plano.</summary>
    string Hash(string password);

    /// <summary>Verifica una contraseña contra su hash almacenado.</summary>
    bool Verify(string password, string hash);
}
