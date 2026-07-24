using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de comprobantes.</summary>
public interface IReceiptRepository
{
    /// <summary>Agrega un comprobante.</summary>
    void Add(Receipt receipt);

    /// <summary>Obtiene un comprobante por su identificador.</summary>
    Receipt? GetById(string id);

    /// <summary>Obtiene el comprobante de un pago.</summary>
    Receipt? GetByPayment(string paymentId);

    /// <summary>Obtiene el comprobante de la reserva completa (sin parte de split).</summary>
    Receipt? GetByReservation(string reservationId);

    /// <summary>Obtiene el comprobante de la parte de un jugador en un partido.</summary>
    Receipt? GetByMatchAndPayer(string matchId, string payerUserId);
}
