using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de usuarios.</summary>
public interface IUserRepository
{
    /// <summary>Obtiene un usuario por su identificador.</summary>
    User? GetById(string id);

    /// <summary>Obtiene un usuario por su correo (case-insensitive).</summary>
    User? GetByEmail(string email);

    /// <summary>Agrega un nuevo usuario.</summary>
    void Add(User user);

    /// <summary>Actualiza un usuario existente.</summary>
    void Update(User user);
}
