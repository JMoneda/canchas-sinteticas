using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de pagos.</summary>
public interface IPaymentRepository
{
    /// <summary>Obtiene un pago por su identificador.</summary>
    Payment? GetById(string id);

    /// <summary>Obtiene el pago de la reserva completa (sin parte de split) de una reserva.</summary>
    Payment? GetByReservation(string reservationId);

    /// <summary>Obtiene el pago asociado a una transacción del proveedor.</summary>
    Payment? GetByGatewayTransactionId(string gatewayTransactionId);

    /// <summary>Obtiene el pago de la parte de un jugador en un partido, si existe.</summary>
    Payment? GetByMatchAndPayer(string matchId, string payerUserId);

    /// <summary>Obtiene todas las partes (pagos) de un partido.</summary>
    IReadOnlyList<Payment> GetSharesByMatch(string matchId);

    /// <summary>Obtiene los pagos pendientes/en proceso cuyo plazo de expiración ya venció.</summary>
    IReadOnlyList<Payment> GetExpirable(DateTime now);

    /// <summary>Agrega un nuevo pago.</summary>
    void Add(Payment payment);

    /// <summary>Actualiza un pago existente.</summary>
    void Update(Payment payment);
}
