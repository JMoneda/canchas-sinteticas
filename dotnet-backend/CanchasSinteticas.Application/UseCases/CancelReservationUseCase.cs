using CanchasSinteticas.Application.DTOs;
using CanchasSinteticas.Domain.Exceptions;
using CanchasSinteticas.Domain.Repositories;

namespace CanchasSinteticas.Application.UseCases;

public class CancelReservationUseCase(IReservationRepository reservationRepo)
{
    public CancelOutput Execute(string reservationId, string userId, DateTime now)
    {
        var reservation = reservationRepo.GetById(reservationId)
            ?? throw new NotFoundError();

        if (reservation.UserId != userId)
            throw new NotAuthorizedError();

        if (reservation.Status == "cancelled")
            throw new AlreadyCancelledError();

        var noShow = reservation.StartDateTime - now < TimeSpan.FromHours(2);

        reservationRepo.Cancel(reservationId);
        if (noShow)
            reservationRepo.AddNoShow(reservationId, userId);

        return new CancelOutput(reservationId, "cancelled", noShow);
    }
}
