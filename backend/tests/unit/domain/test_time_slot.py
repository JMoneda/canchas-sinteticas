import pytest
from datetime import date, datetime, time

from domain.exceptions import DurationError, InvalidBlockError, OperatingHoursError
from domain.value_objects.time_slot import TimeSlot

D = date(2026, 7, 1)


class TestBlockAlignment:
    def test_start_misaligned_raises(self):
        with pytest.raises(InvalidBlockError):
            TimeSlot(D, time(10, 15), time(11, 15))

    def test_end_misaligned_raises(self):
        with pytest.raises(InvalidBlockError):
            TimeSlot(D, time(10, 0), time(11, 15))

    def test_both_on_hour_valid(self):
        assert TimeSlot(D, time(10, 0), time(11, 0)) is not None

    def test_both_on_half_hour_valid(self):
        assert TimeSlot(D, time(10, 30), time(11, 30)) is not None


class TestDuration:
    def test_30_min_raises(self):
        with pytest.raises(DurationError):
            TimeSlot(D, time(10, 0), time(10, 30))

    def test_end_equal_start_raises(self):
        with pytest.raises(DurationError):
            TimeSlot(D, time(10, 0), time(10, 0))

    def test_1_hour_valid(self):
        assert TimeSlot(D, time(10, 0), time(11, 0)) is not None

    def test_90_min_valid(self):
        assert TimeSlot(D, time(10, 0), time(11, 30)) is not None


class TestOperatingHours:
    def test_start_before_6am_raises(self):
        with pytest.raises(OperatingHoursError):
            TimeSlot(D, time(5, 30), time(6, 30))

    def test_end_after_23_raises(self):
        with pytest.raises(OperatingHoursError):
            TimeSlot(D, time(22, 0), time(23, 30))

    def test_start_at_6am_valid(self):
        assert TimeSlot(D, time(6, 0), time(7, 0)) is not None

    def test_end_at_23_valid(self):
        assert TimeSlot(D, time(22, 0), time(23, 0)) is not None


class TestIsBookable:
    def test_exactly_60_min_ahead_is_bookable(self):
        now = datetime(2026, 7, 1, 10, 0, 0)
        slot = TimeSlot(D, time(11, 0), time(12, 0))
        assert slot.is_bookable(now) is True

    def test_59_min_ahead_is_not_bookable(self):
        now = datetime(2026, 7, 1, 10, 1, 0)
        slot = TimeSlot(D, time(11, 0), time(12, 0))
        assert slot.is_bookable(now) is False

    def test_past_slot_is_not_bookable(self):
        now = datetime(2026, 7, 1, 15, 0, 0)
        slot = TimeSlot(D, time(10, 0), time(11, 0))
        assert slot.is_bookable(now) is False


class TestOverlaps:
    def test_overlapping_slots(self):
        a = TimeSlot(D, time(10, 0), time(12, 0))
        b = TimeSlot(D, time(11, 0), time(13, 0))
        assert a.overlaps_with(b) is True

    def test_adjacent_slots_do_not_overlap(self):
        a = TimeSlot(D, time(10, 0), time(11, 0))
        b = TimeSlot(D, time(11, 0), time(12, 0))
        assert a.overlaps_with(b) is False

    def test_non_overlapping_slots(self):
        a = TimeSlot(D, time(10, 0), time(11, 0))
        b = TimeSlot(D, time(12, 0), time(13, 0))
        assert a.overlaps_with(b) is False

    def test_different_dates_do_not_overlap(self):
        a = TimeSlot(D, time(10, 0), time(11, 0))
        b = TimeSlot(date(2026, 7, 2), time(10, 0), time(11, 0))
        assert a.overlaps_with(b) is False
