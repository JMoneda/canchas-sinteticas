using System.ComponentModel.DataAnnotations;

namespace CanchasSinteticas.Infrastructure.Data.Models;

public class FieldModel
{
    [Key] public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
}
