from dataclasses import dataclass, field
from datetime import date, time
from typing import List, Optional


@dataclass
class CreateReservationInput:
    user_id: str
    field_id: int
    date: date
    start_time: time
    end_time: time


@dataclass
class SlotOutput:
    start_time: time
    end_time: time


@dataclass
class FieldAvailabilityOutput:
    field_id: int
    field_name: str
    available_slots: List[SlotOutput]


@dataclass
class ReservationOutput:
    reservation_id: str
    user_id: str
    field_id: int
    field_name: str
    date: date
    start_time: time
    end_time: time
    status: str


@dataclass
class CancelOutput:
    reservation_id: str
    status: str
    no_show: bool
