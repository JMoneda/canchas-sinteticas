using System.ComponentModel.DataAnnotations;

namespace CanchasSinteticas.Infrastructure.Data.Models;

public class NoShowModel
{
    [Key] public string Id { get; set; } = default!;
    public string ReservationId { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string RecordedAt { get; set; } = default!;
}
