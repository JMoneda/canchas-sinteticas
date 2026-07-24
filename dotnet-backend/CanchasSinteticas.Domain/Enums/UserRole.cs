namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Rol de un usuario dentro de la plataforma.
/// </summary>
public enum UserRole
{
    /// <summary>Administrador de la plataforma.</summary>
    SuperAdmin,

    /// <summary>Dueño de una o más sedes/canchas.</summary>
    Owner,

    /// <summary>Cliente que reserva canchas.</summary>
    Client,
}
