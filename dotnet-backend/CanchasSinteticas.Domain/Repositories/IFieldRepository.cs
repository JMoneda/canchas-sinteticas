using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

public interface IFieldRepository
{
    IReadOnlyList<Field> GetAll();
}
