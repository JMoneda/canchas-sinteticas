using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Persistence;

namespace CanchasSinteticas.Infrastructure.Repositories;

/// <summary>Repositorio de usuarios en memoria.</summary>
public class InMemoryUserRepository(InMemoryDatabase db) : IUserRepository
{
    /// <inheritdoc/>
    public User? GetById(string id) => db.Users.GetValueOrDefault(id);

    /// <inheritdoc/>
    public User? GetByEmail(string email) =>
        db.Users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    /// <inheritdoc/>
    public void Add(User user) => db.Users[user.Id] = user;

    /// <inheritdoc/>
    public void Update(User user) => db.Users[user.Id] = user;
}
