using CanchasSinteticas.Domain.Enums;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Pago asociado a una reserva. En este MVP la pasarela en línea es simulada.
/// </summary>
public class Payment
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Reserva a la que corresponde el pago.</summary>
    public string ReservationId { get; }

    /// <summary>Monto del pago.</summary>
    public decimal Amount { get; }

    /// <summary>Medio de pago.</summary>
    public PaymentMethod Method { get; }

    /// <summary>Estado del pago.</summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>Referencia devuelta por la pasarela (simulada).</summary>
    public string? Reference { get; private set; }

    /// <summary>Fecha de creación.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Crea un pago en el estado inicial indicado.</summary>
    public Payment(
        string id,
        string reservationId,
        decimal amount,
        PaymentMethod method,
        PaymentStatus status,
        string? reference,
        DateTime createdAt)
    {
        Id = id;
        ReservationId = reservationId;
        Amount = amount;
        Method = method;
        Status = status;
        Reference = reference;
        CreatedAt = createdAt;
    }

    /// <summary>Marca el pago como realizado con la referencia de la pasarela.</summary>
    public void MarkPaid(string reference)
    {
        Status = PaymentStatus.Paid;
        Reference = reference;
    }

    /// <summary>Marca el pago como reembolsado.</summary>
    public void Refund() => Status = PaymentStatus.Refunded;
}
