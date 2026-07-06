from dataclasses import dataclass
from datetime import date, time, datetime
from typing import Literal, Optional


@dataclass
class Reservation:
    id: str
    user_id: str
    field_id: int
    date: date
    start_time: time
    end_time: time
    status: Literal["active", "cancelled"]
    created_at: datetime
    cancelled_at: Optional[datetime] = None

    @property
    def start_datetime(self) -> datetime:
        return datetime.combine(self.date, self.start_time)

    @property
    def end_datetime(self) -> datetime:
        return datetime.combine(self.date, self.end_time)
