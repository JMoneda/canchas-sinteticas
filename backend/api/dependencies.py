from sqlalchemy.orm import Session

from fastapi import Depends

from application.use_cases.cancel_reservation import CancelReservation
from application.use_cases.create_reservation import CreateReservation
from application.use_cases.list_available_slots import ListAvailableSlots
from application.use_cases.list_reservations import ListReservations
from infrastructure.database import SessionLocal
from infrastructure.repositories.sqlite_field_repository import SQLiteFieldRepository
from infrastructure.repositories.sqlite_reservation_repository import SQLiteReservationRepository


def get_db():
    db = SessionLocal()
    try:
        yield db
    finally:
        db.close()


def get_field_repo(db: Session = Depends(get_db)) -> SQLiteFieldRepository:
    return SQLiteFieldRepository(db)


def get_reservation_repo(db: Session = Depends(get_db)) -> SQLiteReservationRepository:
    return SQLiteReservationRepository(db)


def get_list_slots_uc(
    field_repo: SQLiteFieldRepository = Depends(get_field_repo),
    reservation_repo: SQLiteReservationRepository = Depends(get_reservation_repo),
) -> ListAvailableSlots:
    return ListAvailableSlots(field_repo, reservation_repo)


def get_create_reservation_uc(
    field_repo: SQLiteFieldRepository = Depends(get_field_repo),
    reservation_repo: SQLiteReservationRepository = Depends(get_reservation_repo),
) -> CreateReservation:
    return CreateReservation(field_repo, reservation_repo)


def get_list_reservations_uc(
    field_repo: SQLiteFieldRepository = Depends(get_field_repo),
    reservation_repo: SQLiteReservationRepository = Depends(get_reservation_repo),
) -> ListReservations:
    return ListReservations(reservation_repo, field_repo)


def get_cancel_reservation_uc(
    reservation_repo: SQLiteReservationRepository = Depends(get_reservation_repo),
) -> CancelReservation:
    return CancelReservation(reservation_repo)
