using System.Collections.Concurrent;
using CanchasSinteticas.Domain.Entities;

namespace CanchasSinteticas.Infrastructure.Persistence;

/// <summary>
/// Almacén en memoria de la plataforma. Se registra como singleton y es compartido
/// por todos los repositorios. Reemplaza a la base de datos mientras se define la
/// persistencia definitiva; las interfaces de repositorio permiten enchufar EF Core
/// más adelante sin tocar Domain ni Application.
/// </summary>
public class InMemoryDatabase
{
    /// <summary>Usuarios por id.</summary>
    public ConcurrentDictionary<string, User> Users { get; } = new();

    /// <summary>Sedes por id.</summary>
    public ConcurrentDictionary<string, Venue> Venues { get; } = new();

    /// <summary>Canchas por id.</summary>
    public ConcurrentDictionary<string, Court> Courts { get; } = new();

    /// <summary>Reglas de precio por id.</summary>
    public ConcurrentDictionary<string, PriceRule> PriceRules { get; } = new();

    /// <summary>Bloqueos por id.</summary>
    public ConcurrentDictionary<string, Blackout> Blackouts { get; } = new();

    /// <summary>Reservas por id.</summary>
    public ConcurrentDictionary<string, Reservation> Reservations { get; } = new();

    /// <summary>Pagos indexados por ReservationId (relación 1:1 reserva↔pago).</summary>
    public ConcurrentDictionary<string, Payment> Payments { get; } = new();

    /// <summary>Partidos abiertos por id.</summary>
    public ConcurrentDictionary<string, Match> Matches { get; } = new();
}
