using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Repositories;
using CanchasSinteticas.Infrastructure.Data;

namespace CanchasSinteticas.Infrastructure.Repositories;

public class SqliteFieldRepository(AppDbContext db) : IFieldRepository
{
    public IReadOnlyList<Field> GetAll() =>
        db.Fields.Select(f => new Field(f.Id, f.Name)).ToList();
}
