using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Domain.Repositories;

/// <summary>Acceso a la persistencia de reservas.</summary>
public interface IReservationRepository
{
    /// <summary>Obtiene una reserva por su identificador.</summary>
    Reservation? GetById(string id);

    /// <summary>Obtiene las reservas activas y futuras de un cliente.</summary>
    IReadOnlyList<Reservation> GetActiveByClient(string clientId, DateTime now);

    /// <summary>Obtiene el historial completo de reservas de un cliente.</summary>
    IReadOnlyList<Reservation> GetByClient(string clientId);

    /// <summary>Obtiene las reservas activas de una cancha en una fecha.</summary>
    IReadOnlyList<Reservation> GetActiveByCourtAndDate(string courtId, DateOnly date);

    /// <summary>Obtiene todas las reservas de una cancha.</summary>
    IReadOnlyList<Reservation> GetByCourt(string courtId);

    /// <summary>Cuenta las reservas activas y futuras de un cliente.</summary>
    int CountActiveByClient(string clientId, DateTime now);

    /// <summary>Agrega una nueva reserva.</summary>
    void Add(Reservation reservation);

    /// <summary>Actualiza una reserva existente.</summary>
    void Update(Reservation reservation);
}
