using CanchasSinteticas.Infrastructure.Data;
using CanchasSinteticas.Infrastructure.Data.Models;

namespace CanchasSinteticas.Infrastructure.Seed;

public static class DatabaseSeeder
{
    private static readonly (string Id, string Name)[] Fields =
    [
        ("field-a", "Cancha A"),
        ("field-b", "Cancha B"),
        ("field-c", "Cancha C"),
    ];

    public static void Seed(AppDbContext db)
    {
        db.Database.EnsureCreated();

        foreach (var (id, name) in Fields)
        {
            if (!db.Fields.Any(f => f.Id == id))
                db.Fields.Add(new FieldModel { Id = id, Name = name });
        }

        db.SaveChanges();
    }
}
