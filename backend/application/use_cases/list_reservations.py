from datetime import datetime
from typing import List

from application.dtos import ReservationOutput
from domain.repositories.field_repository import FieldRepository
from domain.repositories.reservation_repository import ReservationRepository


class ListReservations:
    def __init__(self, reservation_repo: ReservationRepository, field_repo: FieldRepository):
        self._reservation_repo = reservation_repo
        self._field_repo = field_repo

    def execute(self, user_id: str, now: datetime) -> List[ReservationOutput]:
        reservations = self._reservation_repo.get_active_by_user(user_id, now)
        fields = {f.id: f for f in self._field_repo.get_all()}

        return [
            ReservationOutput(
                reservation_id=r.id,
                user_id=r.user_id,
                field_id=r.field_id,
                field_name=fields[r.field_id].name if r.field_id in fields else "Unknown",
                date=r.date,
                start_time=r.start_time,
                end_time=r.end_time,
                status=r.status,
            )
            for r in reservations
        ]
