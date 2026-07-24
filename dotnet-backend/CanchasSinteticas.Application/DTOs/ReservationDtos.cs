namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos para crear una reserva desde el marketplace (el cliente sale del token).</summary>
public record CreateReservationInput(
    string CourtId,
    string Date,
    string StartTime,
    string EndTime,
    string? PaymentMethod);

/// <summary>Datos para crear una reserva manual desde el panel del dueño.</summary>
public record ManualReservationInput(
    string CourtId,
    string Date,
    string StartTime,
    string EndTime,
    string? ClientName,
    string? ClientPhone);

/// <summary>Representación completa de una reserva.</summary>
public record ReservationOutput(
    string Id,
    string CourtId,
    string CourtName,
    string VenueId,
    string VenueName,
    string ClientId,
    string? ClientName,
    string? ClientPhone,
    string Date,
    string StartTime,
    string EndTime,
    decimal TotalPrice,
    string Status,
    string Channel,
    string PaymentStatus,
    string CreatedAt);

/// <summary>Resultado de cancelar una reserva.</summary>
public record CancelOutput(
    string ReservationId,
    string Status,
    bool NoShow,
    bool Refunded);
