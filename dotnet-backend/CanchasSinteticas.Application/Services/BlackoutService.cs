using CanchasSinteticas.Application.Common;
using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Entities;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.Services;

/// <summary>
/// Casos de uso de bloqueos de cancha (mantenimiento, eventos) del panel del dueño.
/// </summary>
public class BlackoutService(
    IVenueRepository venues,
    ICourtRepository courts,
    IBlackoutRepository blackouts)
{
    /// <summary>Lista los bloqueos de una cancha del dueño.</summary>
    public IReadOnlyList<BlackoutOutput> ListByCourt(string ownerId, string courtId)
    {
        Ownership.OwnedCourt(venues, courts, ownerId, courtId);
        return blackouts.GetByCourt(courtId).Select(Mappers.ToOutput).ToList();
    }

    /// <summary>Crea un bloqueo en una cancha del dueño.</summary>
    public BlackoutOutput Create(string ownerId, string courtId, CreateBlackoutInput input)
    {
        Ownership.OwnedCourt(venues, courts, ownerId, courtId);

        var date = Parsing.ParseDate(input.Date);
        var start = Parsing.ParseTime(input.StartTime);
        var end = Parsing.ParseTime(input.EndTime);
        if (end <= start)
            throw new ValidationError("La hora de fin del bloqueo debe ser posterior a la de inicio.");

        var blackout = new Blackout(
            Guid.NewGuid().ToString(),
            courtId,
            date,
            start,
            end,
            string.IsNullOrWhiteSpace(input.Reason) ? "Bloqueo" : input.Reason.Trim());

        blackouts.Add(blackout);
        return Mappers.ToOutput(blackout);
    }

    /// <summary>Elimina un bloqueo del dueño.</summary>
    public void Delete(string ownerId, string blackoutId)
    {
        var blackout = blackouts.GetById(blackoutId) ?? throw new NotFoundError();
        Ownership.OwnedCourt(venues, courts, ownerId, blackout.CourtId);
        blackouts.Delete(blackoutId);
    }
}
