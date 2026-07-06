import uuid
from datetime import date, datetime, time
from typing import List, Optional

from application.use_cases.list_reservations import ListReservations
from domain.entities.field import Field
from domain.entities.reservation import Reservation

NOW = datetime(2026, 7, 1, 10, 0, 0)
D = date(2026, 7, 2)
FIELDS = [Field(id=1, name="Cancha A")]


class FakeFieldRepo:
    def get_all(self): return FIELDS


class FakeReservationRepo:
    def __init__(self, reservations=None):
        self._reservations = list(reservations or [])

    def save(self, r): return r
    def get_by_id(self, id): return None
    def count_active_by_user(self, *_): return 0
    def get_active_by_field_and_date(self, *_): return []

    def get_active_by_user(self, user_id, now):
        return [r for r in self._reservations if r.user_id == user_id and r.end_datetime > now]

    def cancel(self, *_): pass
    def add_no_show(self, *_): pass


def _res(user_id="maria"):
    return Reservation(
        id=str(uuid.uuid4()), user_id=user_id, field_id=1, date=D,
        start_time=time(10, 0), end_time=time(11, 0),
        status="active", created_at=datetime(2026, 6, 1),
    )


def test_returns_user_reservations():
    repo = FakeReservationRepo([_res("maria"), _res("pedro")])
    result = ListReservations(repo, FakeFieldRepo()).execute("maria", NOW)
    assert len(result) == 1
    assert result[0].user_id == "maria"
    assert result[0].field_name == "Cancha A"


def test_empty_when_no_reservations():
    repo = FakeReservationRepo()
    result = ListReservations(repo, FakeFieldRepo()).execute("newuser", NOW)
    assert result == []
