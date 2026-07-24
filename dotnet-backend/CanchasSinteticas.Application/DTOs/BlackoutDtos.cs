namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos para crear un bloqueo de cancha.</summary>
public record CreateBlackoutInput(
    string Date,
    string StartTime,
    string EndTime,
    string Reason);

/// <summary>Representación de un bloqueo.</summary>
public record BlackoutOutput(
    string Id,
    string CourtId,
    string Date,
    string StartTime,
    string EndTime,
    string Reason);
