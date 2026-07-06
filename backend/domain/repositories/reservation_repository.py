from abc import ABC, abstractmethod
from datetime import date, datetime
from typing import List, Optional

from domain.entities.reservation import Reservation


class ReservationRepository(ABC):
    @abstractmethod
    def save(self, reservation: Reservation) -> Reservation:
        ...

    @abstractmethod
    def get_by_id(self, id: str) -> Optional[Reservation]:
        ...

    @abstractmethod
    def count_active_by_user(self, user_id: str, now: datetime) -> int:
        ...

    @abstractmethod
    def get_active_by_field_and_date(self, field_id: int, query_date: date) -> List[Reservation]:
        ...

    @abstractmethod
    def get_active_by_user(self, user_id: str, now: datetime) -> List[Reservation]:
        ...

    @abstractmethod
    def cancel(self, id: str, cancelled_at: datetime) -> None:
        ...

    @abstractmethod
    def add_no_show(self, reservation_id: str, user_id: str, cancelled_at: datetime) -> None:
        ...
