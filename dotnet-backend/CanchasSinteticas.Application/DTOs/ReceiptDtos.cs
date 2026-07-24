namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos de un comprobante para render en la aplicación.</summary>
public record ReceiptOutput(
    string Number,
    decimal Amount,
    string Method,
    string GatewayReference,
    string IssuedAt,
    string VenueName,
    string CourtName,
    string PayerName);
