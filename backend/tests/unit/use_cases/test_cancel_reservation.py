import uuid
from datetime import date, datetime, time, timedelta
from typing import List, Optional

import pytest

from application.use_cases.cancel_reservation import CancelReservation
from domain.entities.reservation import Reservation
from domain.exceptions import AlreadyCancelledError, NotAuthorizedError, NotFoundError

NOW = datetime(2026, 7, 1, 10, 0, 0)
D = date(2026, 7, 1)
RES_ID = str(uuid.uuid4())


class FakeReservationRepo:
    def __init__(self, reservation: Optional[Reservation] = None):
        self._reservation = reservation
        self.cancelled_ids = []
        self.no_shows = []

    def save(self, r): return r
    def get_by_id(self, id): return self._reservation if self._reservation and self._reservation.id == id else None
    def count_active_by_user(self, *_): return 0
    def get_active_by_field_and_date(self, *_): return []
    def get_active_by_user(self, *_): return []

    def cancel(self, id: str, cancelled_at: datetime) -> None:
        self.cancelled_ids.append(id)
        if self._reservation:
            self._reservation.status = "cancelled"

    def add_no_show(self, reservation_id, user_id, cancelled_at):
        self.no_shows.append(reservation_id)


def _res(user_id="maria", start=time(14, 0), end=time(15, 0), status="active"):
    return Reservation(
        id=RES_ID, user_id=user_id, field_id=1, date=D,
        start_time=start, end_time=end, status=status,
        created_at=datetime(2026, 6, 1),
    )


def test_not_found_raises():
    uc = CancelReservation(FakeReservationRepo())
    with pytest.raises(NotFoundError):
        uc.execute("nonexistent", "maria", NOW)


def test_not_authorized_raises():
    repo = FakeReservationRepo(_res(user_id="pedro"))
    uc = CancelReservation(repo)
    with pytest.raises(NotAuthorizedError):
        uc.execute(RES_ID, "maria", NOW)


def test_already_cancelled_raises():
    repo = FakeReservationRepo(_res(status="cancelled"))
    uc = CancelReservation(repo)
    with pytest.raises(AlreadyCancelledError):
        uc.execute(RES_ID, "maria", NOW)


def test_clean_cancellation_no_show_false():
    repo = FakeReservationRepo(_res(start=time(14, 0), end=time(15, 0)))
    uc = CancelReservation(repo)
    result = uc.execute(RES_ID, "maria", NOW)
    assert result.status == "cancelled"
    assert result.no_show is False
    assert len(repo.no_shows) == 0


def test_late_cancellation_triggers_no_show():
    now = datetime(2026, 7, 1, 13, 0, 0)
    repo = FakeReservationRepo(_res(start=time(14, 0), end=time(15, 0)))
    uc = CancelReservation(repo)
    result = uc.execute(RES_ID, "maria", now)
    assert result.no_show is True
    assert RES_ID in repo.no_shows


def test_exactly_2h_notice_is_not_no_show():
    now = datetime(2026, 7, 1, 12, 0, 0)
    repo = FakeReservationRepo(_res(start=time(14, 0), end=time(15, 0)))
    uc = CancelReservation(repo)
    result = uc.execute(RES_ID, "maria", now)
    assert result.no_show is False
