namespace CanchasSinteticas.Application.Abstractions;

/// <summary>Ajustes de pago independientes del proveedor, provistos desde la configuración.</summary>
public record PaymentSettings(int ExpiryMinutes);
