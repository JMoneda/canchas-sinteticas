import uuid
from datetime import datetime

from application.dtos import CreateReservationInput, ReservationOutput
from domain.entities.reservation import Reservation
from domain.exceptions import AdvanceNoticeError, ActiveLimitError, FieldNotFoundError, OverlapError
from domain.repositories.field_repository import FieldRepository
from domain.repositories.reservation_repository import ReservationRepository
from domain.value_objects.time_slot import TimeSlot


class CreateReservation:
    def __init__(self, field_repo: FieldRepository, reservation_repo: ReservationRepository):
        self._field_repo = field_repo
        self._reservation_repo = reservation_repo

    def execute(self, input: CreateReservationInput, now: datetime) -> ReservationOutput:
        field = next((f for f in self._field_repo.get_all() if f.id == input.field_id), None)
        if field is None:
            raise FieldNotFoundError()

        slot = TimeSlot(date=input.date, start_time=input.start_time, end_time=input.end_time)

        if not slot.is_bookable(now):
            raise AdvanceNoticeError()

        if self._reservation_repo.count_active_by_user(input.user_id, now) >= 2:
            raise ActiveLimitError()

        existing = self._reservation_repo.get_active_by_field_and_date(input.field_id, input.date)
        existing_slots = [
            TimeSlot(date=r.date, start_time=r.start_time, end_time=r.end_time)
            for r in existing
        ]
        if any(slot.overlaps_with(s) for s in existing_slots):
            raise OverlapError()

        reservation = Reservation(
            id=str(uuid.uuid4()),
            user_id=input.user_id,
            field_id=input.field_id,
            date=input.date,
            start_time=input.start_time,
            end_time=input.end_time,
            status="active",
            created_at=now,
            cancelled_at=None,
        )
        saved = self._reservation_repo.save(reservation)

        return ReservationOutput(
            reservation_id=saved.id,
            user_id=saved.user_id,
            field_id=saved.field_id,
            field_name=field.name,
            date=saved.date,
            start_time=saved.start_time,
            end_time=saved.end_time,
            status=saved.status,
        )
