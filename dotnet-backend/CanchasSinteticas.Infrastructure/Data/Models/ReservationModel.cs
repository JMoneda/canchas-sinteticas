using System.ComponentModel.DataAnnotations;

namespace CanchasSinteticas.Infrastructure.Data.Models;

public class ReservationModel
{
    [Key] public string Id { get; set; } = default!;
    public string FieldId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string Date { get; set; } = default!;
    public string StartTime { get; set; } = default!;
    public string EndTime { get; set; } = default!;
    public string Status { get; set; } = default!;
}
