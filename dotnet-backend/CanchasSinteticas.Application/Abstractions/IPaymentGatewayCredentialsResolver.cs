using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Application.Abstractions;

/// <summary>
/// Resuelve las credenciales del proveedor de pagos según el modelo de recaudo de la sede:
/// cuenta directa del dueño o cuenta central de la plataforma (marketplace).
/// </summary>
public interface IPaymentGatewayCredentialsResolver
{
    /// <summary>Devuelve las credenciales a usar para cobrar en la sede indicada.</summary>
    PaymentGatewayCredentials Resolve(Venue venue);

    /// <summary>Devuelve las credenciales de la plataforma (marketplace), para operaciones sin sede resuelta.</summary>
    PaymentGatewayCredentials ResolvePlatform();
}
