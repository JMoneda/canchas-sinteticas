import uuid
from datetime import date, datetime, time
from typing import List, Optional

import pytest

from application.dtos import CreateReservationInput
from application.use_cases.create_reservation import CreateReservation
from domain.entities.field import Field
from domain.entities.reservation import Reservation
from domain.exceptions import (
    ActiveLimitError,
    AdvanceNoticeError,
    DurationError,
    FieldNotFoundError,
    InvalidBlockError,
    OperatingHoursError,
    OverlapError,
)

NOW = datetime(2026, 7, 1, 8, 0, 0)
FUTURE_DATE = date(2026, 7, 1)
FIELDS = [Field(id=1, name="Cancha A"), Field(id=2, name="Cancha B")]


class FakeFieldRepo:
    def __init__(self, fields=None):
        self._fields = fields if fields is not None else list(FIELDS)

    def get_all(self) -> List[Field]:
        return self._fields


class FakeReservationRepo:
    def __init__(self, existing=None):
        self._reservations: List[Reservation] = list(existing or [])
        self.no_shows = []

    def save(self, r: Reservation) -> Reservation:
        self._reservations.append(r)
        return r

    def get_by_id(self, id: str) -> Optional[Reservation]:
        return next((r for r in self._reservations if r.id == id), None)

    def count_active_by_user(self, user_id: str, now: datetime) -> int:
        return sum(
            1 for r in self._reservations
            if r.user_id == user_id and r.status == "active" and r.end_datetime > now
        )

    def get_active_by_field_and_date(self, field_id: int, query_date: date) -> List[Reservation]:
        return [
            r for r in self._reservations
            if r.field_id == field_id and r.date == query_date and r.status == "active"
        ]

    def get_active_by_user(self, user_id: str, now: datetime) -> List[Reservation]:
        return []

    def cancel(self, id: str, cancelled_at: datetime) -> None:
        pass

    def add_no_show(self, reservation_id: str, user_id: str, cancelled_at: datetime) -> None:
        self.no_shows.append(reservation_id)


def _uc(fields=None, existing=None):
    return CreateReservation(FakeFieldRepo(fields), FakeReservationRepo(existing))


def _input(**kwargs):
    defaults = dict(user_id="maria", field_id=1, date=FUTURE_DATE, start_time=time(10, 0), end_time=time(12, 0))
    defaults.update(kwargs)
    return CreateReservationInput(**defaults)


def _reservation(user_id="other", field_id=2, start=time(14, 0), end=time(15, 0)):
    return Reservation(
        id=str(uuid.uuid4()), user_id=user_id, field_id=field_id,
        date=FUTURE_DATE, start_time=start, end_time=end,
        status="active", created_at=datetime(2026, 6, 1),
    )


def test_valid_reservation_created():
    result = _uc().execute(_input(), now=NOW)
    assert result.reservation_id is not None
    assert result.status == "active"
    assert result.field_name == "Cancha A"


def test_field_not_found():
    with pytest.raises(FieldNotFoundError):
        _uc().execute(_input(field_id=99), now=NOW)


def test_duration_too_short():
    with pytest.raises(DurationError):
        _uc().execute(_input(start_time=time(10, 0), end_time=time(10, 30)), now=NOW)


def test_misaligned_block():
    with pytest.raises(InvalidBlockError):
        _uc().execute(_input(start_time=time(10, 15), end_time=time(11, 15)), now=NOW)


def test_outside_operating_hours():
    with pytest.raises(OperatingHoursError):
        _uc().execute(_input(start_time=time(5, 0), end_time=time(6, 0)), now=NOW)


def test_insufficient_advance_notice():
    with pytest.raises(AdvanceNoticeError):
        _uc().execute(_input(start_time=time(8, 30), end_time=time(9, 30)), now=NOW)


def test_active_limit_reached():
    existing = [_reservation(user_id="maria", field_id=2, start=time(14, 0), end=time(15, 0)),
                _reservation(user_id="maria", field_id=2, start=time(16, 0), end=time(17, 0))]
    with pytest.raises(ActiveLimitError):
        _uc(existing=existing).execute(_input(), now=NOW)


def test_overlap_rejected():
    existing = [_reservation(user_id="pedro", field_id=1, start=time(11, 0), end=time(13, 0))]
    with pytest.raises(OverlapError):
        _uc(existing=existing).execute(_input(start_time=time(10, 0), end_time=time(12, 0)), now=NOW)


def test_exactly_60_min_notice_accepted():
    now = datetime(2026, 7, 1, 9, 0, 0)
    result = _uc().execute(_input(start_time=time(10, 0), end_time=time(11, 0)), now=now)
    assert result.status == "active"
