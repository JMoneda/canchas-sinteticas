namespace CanchasSinteticas.Domain.Enums;

/// <summary>
/// Medio de pago usado para una reserva o para una parte de pago dividido.
/// Los valores en línea corresponden a los instrumentos más usados en Colombia.
/// </summary>
public enum PaymentMethod
{
    /// <summary>Pasarela de pago en línea genérica (instrumento no especificado).</summary>
    OnlineGateway,

    /// <summary>Pago en efectivo en la sede.</summary>
    Cash,

    /// <summary>Tarjeta de crédito o débito.</summary>
    Card,

    /// <summary>Nequi.</summary>
    Nequi,

    /// <summary>PSE (débito desde cuenta bancaria).</summary>
    Pse,

    /// <summary>Transferencia Bancolombia.</summary>
    BancolombiaTransfer,

    /// <summary>Botón Bancolombia.</summary>
    BancolombiaButton,

    /// <summary>Bancolombia QR.</summary>
    BancolombiaQr,
}
