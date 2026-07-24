using CanchasSinteticas.Application.Abstractions;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Enums;

namespace CanchasSinteticas.Infrastructure.Payments;

/// <summary>
/// Resuelve las credenciales de Wompi según el modelo de recaudo de la sede. En modo cuenta directa
/// se usa la referencia de comercio del dueño; en marketplace, las credenciales de la plataforma.
/// Los secretos se leen de configuración segura, nunca del código.
/// </summary>
public class PaymentGatewayCredentialsResolver(PaymentsOptions options) : IPaymentGatewayCredentialsResolver
{
    private readonly PaymentsOptions options = options;

    /// <inheritdoc/>
    public PaymentGatewayCredentials Resolve(Venue venue)
    {
        var merchantRef = venue.SettlementMode == SettlementMode.Direct
            ? venue.GatewayMerchantRef
            : null;

        return Build(merchantRef);
    }

    /// <inheritdoc/>
    public PaymentGatewayCredentials ResolvePlatform() => Build(null);

    private PaymentGatewayCredentials Build(string? merchantRef)
    {
        var w = options.Wompi;
        return new PaymentGatewayCredentials(
            BaseUrl: w.BaseUrl,
            PublicKey: w.PublicKey,
            PrivateKey: w.PrivateKey,
            IntegritySecret: w.IntegritySecret,
            EventsSecret: w.EventsSecret,
            MerchantRef: merchantRef);
    }
}
