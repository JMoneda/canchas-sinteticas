namespace CanchasSinteticas.Application.DTOs;

/// <summary>
/// Slot de disponibilidad de una cancha, con su precio y estado.
/// </summary>
/// <param name="StartTime">Hora de inicio (HH:mm).</param>
/// <param name="EndTime">Hora de fin (HH:mm).</param>
/// <param name="Price">Precio total del slot.</param>
/// <param name="Available">Indica si el slot puede reservarse.</param>
/// <param name="Status">Estado: available, reserved, blocked o past.</param>
public record SlotOutput(
    string StartTime,
    string EndTime,
    decimal Price,
    bool Available,
    string Status);

/// <summary>
/// Disponibilidad de una cancha para una fecha concreta.
/// </summary>
public record CourtAvailabilityOutput(
    string CourtId,
    string CourtName,
    string Type,
    string Date,
    IReadOnlyList<SlotOutput> Slots);
