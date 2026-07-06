from dataclasses import dataclass
from datetime import date, time, datetime, timedelta

from domain.exceptions import DurationError, InvalidBlockError, OperatingHoursError

_OPERATING_START = time(6, 0)
_OPERATING_END = time(23, 0)
_VALID_MINUTES = {0, 30}


@dataclass
class TimeSlot:
    date: date
    start_time: time
    end_time: time

    def __post_init__(self) -> None:
        if self.start_time.minute not in _VALID_MINUTES or self.end_time.minute not in _VALID_MINUTES:
            raise InvalidBlockError()

        start_dt = datetime.combine(self.date, self.start_time)
        end_dt = datetime.combine(self.date, self.end_time)
        if end_dt - start_dt < timedelta(hours=1):
            raise DurationError()

        if self.start_time < _OPERATING_START or self.end_time > _OPERATING_END:
            raise OperatingHoursError()

    @property
    def start_datetime(self) -> datetime:
        return datetime.combine(self.date, self.start_time)

    @property
    def end_datetime(self) -> datetime:
        return datetime.combine(self.date, self.end_time)

    def is_bookable(self, now: datetime) -> bool:
        return self.start_datetime - now >= timedelta(hours=1)

    def overlaps_with(self, other: "TimeSlot") -> bool:
        if self.date != other.date:
            return False
        return self.start_time < other.end_time and other.start_time < self.end_time
