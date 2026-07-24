namespace CanchasSinteticas.Application.DTOs;

/// <summary>Datos para configurar el modelo de recaudo de una sede.</summary>
public record VenuePaymentConfigInput(string SettlementMode, string? GatewayMerchantRef);

/// <summary>Configuración de recaudo de una sede.</summary>
public record VenuePaymentConfigOutput(string VenueId, string SettlementMode, string? GatewayMerchantRef);
