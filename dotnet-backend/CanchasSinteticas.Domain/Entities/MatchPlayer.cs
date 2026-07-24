namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Jugador inscrito en un partido abierto, con su parte del pago dividido y el estado de su cobro.
/// </summary>
public class MatchPlayer(string userId, string name, decimal shareAmount, DateTime joinedAt)
{
    /// <summary>Identificador del usuario.</summary>
    public string UserId { get; } = userId;

    /// <summary>Nombre para mostrar.</summary>
    public string Name { get; } = name;

    /// <summary>Parte que le corresponde pagar (exacta según su posición en el cupo).</summary>
    public decimal ShareAmount { get; } = shareAmount;

    /// <summary>Momento en que se unió.</summary>
    public DateTime JoinedAt { get; } = joinedAt;

    /// <summary>Indica si el jugador ya pagó su parte.</summary>
    public bool HasPaid { get; private set; }

    /// <summary>Pago (parte) asociado a este jugador, si existe.</summary>
    public string? PaymentId { get; private set; }

    /// <summary>Enlaza el pago de la parte del jugador (al iniciar el cobro).</summary>
    public void AttachPayment(string paymentId) => PaymentId = paymentId;

    /// <summary>Marca la parte del jugador como pagada tras la confirmación del proveedor.</summary>
    public void MarkPaid(string paymentId)
    {
        PaymentId = paymentId;
        HasPaid = true;
    }

    /// <summary>Revierte el pago de la parte (al abandonar o reembolsar).</summary>
    public void ClearPayment()
    {
        HasPaid = false;
        PaymentId = null;
    }
}
