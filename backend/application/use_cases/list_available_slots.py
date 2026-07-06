from datetime import date, datetime, time, timedelta
from typing import List

from application.dtos import FieldAvailabilityOutput, SlotOutput
from domain.repositories.field_repository import FieldRepository
from domain.repositories.reservation_repository import ReservationRepository

_OPERATING_START = time(6, 0)
_OPERATING_END = time(23, 0)
_SLOT_DURATION = timedelta(hours=1)


class ListAvailableSlots:
    def __init__(self, field_repo: FieldRepository, reservation_repo: ReservationRepository):
        self._field_repo = field_repo
        self._reservation_repo = reservation_repo

    def execute(self, query_date: date, now: datetime) -> List[FieldAvailabilityOutput]:
        fields = self._field_repo.get_all()
        result = []
        for field in fields:
            active = self._reservation_repo.get_active_by_field_and_date(field.id, query_date)
            slots = self._build_slots(query_date, now, active)
            result.append(FieldAvailabilityOutput(
                field_id=field.id,
                field_name=field.name,
                available_slots=slots,
            ))
        return result

    def _build_slots(self, query_date: date, now: datetime, reservations) -> List[SlotOutput]:
        slots = []
        current = datetime.combine(query_date, _OPERATING_START)
        end_dt = datetime.combine(query_date, _OPERATING_END)

        while current + _SLOT_DURATION <= end_dt:
            slot_start = current.time()
            slot_end = (current + _SLOT_DURATION).time()

            occupied = any(
                r.start_time < slot_end and slot_start < r.end_time
                for r in reservations
            )
            bookable = current - now >= timedelta(hours=1)

            if not occupied and bookable:
                slots.append(SlotOutput(start_time=slot_start, end_time=slot_end))

            current += _SLOT_DURATION

        return slots
