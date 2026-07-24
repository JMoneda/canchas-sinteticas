using CanchasSinteticas.Domain.Enums;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Domain.Entities;

/// <summary>
/// Reserva de una cancha por parte de un cliente en una franja concreta.
/// </summary>
public class Reservation
{
    /// <summary>Identificador único.</summary>
    public string Id { get; }

    /// <summary>Cancha reservada.</summary>
    public string CourtId { get; }

    /// <summary>Cliente que reserva. Para reservas manuales usa un identificador genérico.</summary>
    public string ClientId { get; }

    /// <summary>Nombre del cliente (usado en reservas manuales walk-in/teléfono).</summary>
    public string? ClientName { get; }

    /// <summary>Teléfono del cliente (usado en reservas manuales).</summary>
    public string? ClientPhone { get; }

    /// <summary>Fecha del turno.</summary>
    public DateOnly Date { get; }

    /// <summary>Hora de inicio.</summary>
    public TimeOnly StartTime { get; }

    /// <summary>Hora de fin.</summary>
    public TimeOnly EndTime { get; }

    /// <summary>Precio total calculado según las reglas de precio de la cancha.</summary>
    public decimal TotalPrice { get; }

    /// <summary>Estado actual de la reserva.</summary>
    public ReservationStatus Status { get; private set; }

    /// <summary>Canal de origen de la reserva.</summary>
    public ReservationChannel Channel { get; }

    /// <summary>Fecha y hora de creación.</summary>
    public DateTime CreatedAt { get; }

    /// <summary>Crea una reserva confirmada.</summary>
    public Reservation(
        string id,
        string courtId,
        string clientId,
        string? clientName,
        string? clientPhone,
        DateOnly date,
        TimeOnly startTime,
        TimeOnly endTime,
        decimal totalPrice,
        ReservationChannel channel,
        DateTime createdAt)
    {
        Id = id;
        CourtId = courtId;
        ClientId = clientId;
        ClientName = clientName;
        ClientPhone = clientPhone;
        Date = date;
        StartTime = startTime;
        EndTime = endTime;
        TotalPrice = totalPrice;
        Status = ReservationStatus.Confirmed;
        Channel = channel;
        CreatedAt = createdAt;
    }

    /// <summary>Fecha y hora de inicio combinadas.</summary>
    public DateTime StartDateTime => Date.ToDateTime(StartTime);

    /// <summary>Indica si la reserva está vigente (confirmada).</summary>
    public bool IsActive => Status == ReservationStatus.Confirmed;

    /// <summary>
    /// Cancela la reserva. Si <paramref name="isLate"/> es verdadero (fuera del plazo
    /// de cancelación permitido) queda marcada como no-show en lugar de cancelada.
    /// </summary>
    public void Cancel(bool isLate)
    {
        if (Status == ReservationStatus.Cancelled || Status == ReservationStatus.NoShow)
            throw new AlreadyCancelledError();

        Status = isLate ? ReservationStatus.NoShow : ReservationStatus.Cancelled;
    }

    /// <summary>Marca la reserva como completada (turno jugado).</summary>
    public void Complete()
    {
        if (Status == ReservationStatus.Confirmed)
            Status = ReservationStatus.Completed;
    }
}
