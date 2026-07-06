from datetime import date as date_type, datetime, time
from typing import List, Optional

from sqlalchemy import and_, or_
from sqlalchemy.orm import Session

from domain.entities.reservation import Reservation
from domain.repositories.reservation_repository import ReservationRepository
from infrastructure.models.no_show_model import NoShowModel
from infrastructure.models.reservation_model import ReservationModel


class SQLiteReservationRepository(ReservationRepository):
    def __init__(self, session: Session):
        self._session = session

    def save(self, r: Reservation) -> Reservation:
        model = ReservationModel(
            id=r.id,
            user_id=r.user_id,
            field_id=r.field_id,
            date=r.date.isoformat(),
            start_time=r.start_time.strftime("%H:%M"),
            end_time=r.end_time.strftime("%H:%M"),
            status=r.status,
            created_at=r.created_at.isoformat(),
            cancelled_at=None,
        )
        self._session.add(model)
        self._session.commit()
        self._session.refresh(model)
        return self._to_domain(model)

    def get_by_id(self, id: str) -> Optional[Reservation]:
        model = self._session.query(ReservationModel).filter_by(id=id).first()
        return self._to_domain(model) if model else None

    def count_active_by_user(self, user_id: str, now: datetime) -> int:
        today = now.date().isoformat()
        current_time = now.strftime("%H:%M")
        return self._session.query(ReservationModel).filter(
            ReservationModel.user_id == user_id,
            ReservationModel.status == "active",
            or_(
                ReservationModel.date > today,
                and_(
                    ReservationModel.date == today,
                    ReservationModel.end_time > current_time,
                ),
            ),
        ).count()

    def get_active_by_field_and_date(self, field_id: int, query_date: date_type) -> List[Reservation]:
        models = self._session.query(ReservationModel).filter(
            ReservationModel.field_id == field_id,
            ReservationModel.date == query_date.isoformat(),
            ReservationModel.status == "active",
        ).all()
        return [self._to_domain(m) for m in models]

    def get_active_by_user(self, user_id: str, now: datetime) -> List[Reservation]:
        today = now.date().isoformat()
        current_time = now.strftime("%H:%M")
        models = self._session.query(ReservationModel).filter(
            ReservationModel.user_id == user_id,
            ReservationModel.status == "active",
            or_(
                ReservationModel.date > today,
                and_(
                    ReservationModel.date == today,
                    ReservationModel.end_time > current_time,
                ),
            ),
        ).all()
        return [self._to_domain(m) for m in models]

    def cancel(self, id: str, cancelled_at: datetime) -> None:
        self._session.query(ReservationModel).filter_by(id=id).update({
            "status": "cancelled",
            "cancelled_at": cancelled_at.isoformat(),
        })
        self._session.commit()

    def add_no_show(self, reservation_id: str, user_id: str, cancelled_at: datetime) -> None:
        self._session.add(NoShowModel(
            reservation_id=reservation_id,
            user_id=user_id,
            cancelled_at=cancelled_at.isoformat(),
        ))
        self._session.commit()

    def _to_domain(self, model: ReservationModel) -> Reservation:
        return Reservation(
            id=model.id,
            user_id=model.user_id,
            field_id=model.field_id,
            date=date_type.fromisoformat(model.date),
            start_time=time.fromisoformat(model.start_time),
            end_time=time.fromisoformat(model.end_time),
            status=model.status,
            created_at=datetime.fromisoformat(model.created_at),
            cancelled_at=datetime.fromisoformat(model.cancelled_at) if model.cancelled_at else None,
        )
