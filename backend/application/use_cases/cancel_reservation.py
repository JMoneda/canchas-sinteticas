from datetime import datetime, timedelta

from application.dtos import CancelOutput
from domain.exceptions import AlreadyCancelledError, NotAuthorizedError, NotFoundError
from domain.repositories.reservation_repository import ReservationRepository


class CancelReservation:
    def __init__(self, reservation_repo: ReservationRepository):
        self._reservation_repo = reservation_repo

    def execute(self, reservation_id: str, user_id: str, now: datetime) -> CancelOutput:
        reservation = self._reservation_repo.get_by_id(reservation_id)
        if reservation is None:
            raise NotFoundError()

        if reservation.user_id != user_id:
            raise NotAuthorizedError()

        if reservation.status == "cancelled":
            raise AlreadyCancelledError()

        no_show = reservation.start_datetime - now < timedelta(hours=2)

        self._reservation_repo.cancel(reservation_id, now)

        if no_show:
            self._reservation_repo.add_no_show(reservation_id, user_id, now)

        return CancelOutput(
            reservation_id=reservation_id,
            status="cancelled",
            no_show=no_show,
        )
