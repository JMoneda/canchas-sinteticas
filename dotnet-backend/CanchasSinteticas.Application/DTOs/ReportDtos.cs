namespace CanchasSinteticas.Application.DTOs;

/// <summary>Reporte agregado para el dueño en un rango de fechas.</summary>
public record OwnerReportOutput(
    string From,
    string To,
    int TotalReservations,
    decimal TotalRevenue,
    double OccupancyRate,
    IReadOnlyList<CourtReportOutput> ByCourt,
    IReadOnlyList<HourStatOutput> TopHours);

/// <summary>Métricas por cancha dentro de un reporte.</summary>
public record CourtReportOutput(
    string CourtId,
    string CourtName,
    string VenueName,
    int Reservations,
    decimal Revenue);

/// <summary>Conteo de reservas por hora de inicio.</summary>
public record HourStatOutput(string Hour, int Count);
