import uuid
from datetime import date, datetime, time
from typing import List, Optional

from application.use_cases.list_available_slots import ListAvailableSlots
from domain.entities.field import Field
from domain.entities.reservation import Reservation

D = date(2026, 7, 1)
FIELDS = [Field(id=1, name="Cancha A")]


class FakeFieldRepo:
    def get_all(self): return FIELDS


class FakeReservationRepo:
    def __init__(self, existing=None):
        self._existing = list(existing or [])

    def save(self, r): return r
    def get_by_id(self, id): return None
    def count_active_by_user(self, *_): return 0

    def get_active_by_field_and_date(self, field_id, query_date):
        return [r for r in self._existing if r.field_id == field_id and r.date == query_date]

    def get_active_by_user(self, *_): return []
    def cancel(self, *_): pass
    def add_no_show(self, *_): pass


def _res(start, end):
    return Reservation(
        id=str(uuid.uuid4()), user_id="pedro", field_id=1, date=D,
        start_time=start, end_time=end,
        status="active", created_at=datetime(2026, 6, 1),
    )


def test_all_slots_returned_when_no_reservations():
    now = datetime(2026, 7, 1, 0, 0, 0)
    result = ListAvailableSlots(FakeFieldRepo(), FakeReservationRepo()).execute(D, now)
    assert len(result) == 1
    assert result[0].field_id == 1
    assert len(result[0].available_slots) == 17  # 06:00–23:00 in 1-hour blocks


def test_booked_slots_excluded():
    now = datetime(2026, 7, 1, 0, 0, 0)
    repo = FakeReservationRepo([_res(time(10, 0), time(11, 0))])
    result = ListAvailableSlots(FakeFieldRepo(), repo).execute(D, now)
    slots = result[0].available_slots
    slot_starts = [s.start_time.strftime("%H:%M") for s in slots]
    assert "10:00" not in slot_starts
    assert "11:00" in slot_starts


def test_slots_within_1h_of_now_excluded():
    now = datetime(2026, 7, 1, 10, 0, 0)
    result = ListAvailableSlots(FakeFieldRepo(), FakeReservationRepo()).execute(D, now)
    slots = result[0].available_slots
    slot_starts = [s.start_time.strftime("%H:%M") for s in slots]
    assert "09:00" not in slot_starts
    assert "10:00" not in slot_starts
    assert "11:00" in slot_starts
