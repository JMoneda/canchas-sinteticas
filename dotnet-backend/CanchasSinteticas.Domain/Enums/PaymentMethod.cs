namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Medio de pago usado para una reserva.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Pasarela de pago en línea (simulada en este MVP).</summary>
    OnlineGateway,

    /// <summary>Pago en efectivo en la sede.</summary>
    Cash,
}
