using CanchasSinteticas.Domain.Enums;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Usuario de la plataforma. Puede ser super-admin, dueño de canchas o cliente.
/// </summary>
public class User
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Nombre completo.</summary>
    public string Name { get; set; }

    /// <summary>Correo electrónico (único, usado como credencial de acceso).</summary>
    public string Email { get; }

    /// <summary>Teléfono de contacto.</summary>
    public string? Phone { get; set; }

    /// <summary>Hash de la contraseña.</summary>
    public string PasswordHash { get; set; }

    /// <summary>Rol del usuario.</summary>
    public UserRole Role { get; }

    /// <summary>Fecha de creación de la cuenta.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Crea un usuario.</summary>
    public User(
        string id,
        string name,
        string email,
        string? phone,
        string passwordHash,
        UserRole role,
        DateTime createdAt)
    {
        Id = id;
        Name = name;
        Email = email;
        Phone = phone;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = createdAt;
    }
}
