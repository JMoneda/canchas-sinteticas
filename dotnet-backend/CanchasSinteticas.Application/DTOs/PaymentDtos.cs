namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos para pagar una reserva.</summary>
public record PayInput(string Method);

/// <summary>Resultado de un pago.</summary>
public record PaymentOutput(
    string ReservationId,
    decimal Amount,
    string Method,
    string Status,
    string? Reference);
