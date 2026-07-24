namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos para iniciar el pago de una reserva.</summary>
public record PayInput(string Method, string? ReturnUrl = null);

/// <summary>Resultado de iniciar un pago: información de checkout del proveedor.</summary>
public record PaymentInitiationOutput(
    string PaymentId,
    string ReservationId,
    string Status,
    decimal Amount,
    string Method,
    string? CheckoutUrl,
    string? ExpiresAt);

/// <summary>Estado actual de un pago (para consulta/polling desde el frontend).</summary>
public record PaymentStatusOutput(
    string PaymentId,
    string ReservationId,
    string Status,
    decimal Amount,
    string Method,
    string? GatewayReference,
    string? PaidAt,
    bool HasReceipt);
