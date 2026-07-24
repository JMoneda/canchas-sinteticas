using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de pagos.</summary>
public interface IPaymentRepository
{
    /// <summary>Obtiene el pago asociado a una reserva.</summary>
    Payment? GetByReservation(string reservationId);

    /// <summary>Agrega un nuevo pago.</summary>
    void Add(Payment payment);

    /// <summary>Actualiza un pago existente.</summary>
    void Update(Payment payment);
}
