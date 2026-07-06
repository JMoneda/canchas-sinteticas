from datetime import date, datetime, time

from fastapi import APIRouter, Depends, Query
from pydantic import BaseModel

from api.dependencies import (
    get_cancel_reservation_uc,
    get_create_reservation_uc,
    get_list_reservations_uc,
)
from api.error_handling import domain_error_response
from application.dtos import CreateReservationInput
from application.use_cases.cancel_reservation import CancelReservation
from application.use_cases.create_reservation import CreateReservation
from application.use_cases.list_reservations import ListReservations
from domain.exceptions import DomainError

router = APIRouter()


class CreateReservationRequest(BaseModel):
    user_id: str
    field_id: int
    date: date
    start_time: time
    end_time: time


class CancelReservationRequest(BaseModel):
    user_id: str


@router.post("/reservations", status_code=201)
def create_reservation(
    body: CreateReservationRequest,
    use_case: CreateReservation = Depends(get_create_reservation_uc),
):
    try:
        result = use_case.execute(
            input=CreateReservationInput(
                user_id=body.user_id,
                field_id=body.field_id,
                date=body.date,
                start_time=body.start_time,
                end_time=body.end_time,
            ),
            now=datetime.now(),
        )
        return {
            "reservation_id": result.reservation_id,
            "user_id": result.user_id,
            "field_id": result.field_id,
            "field_name": result.field_name,
            "date": result.date.isoformat(),
            "start_time": result.start_time.strftime("%H:%M"),
            "end_time": result.end_time.strftime("%H:%M"),
            "status": result.status,
        }
    except DomainError as e:
        return domain_error_response(e)


@router.get("/reservations")
def list_reservations(
    user_id: str = Query(...),
    use_case: ListReservations = Depends(get_list_reservations_uc),
):
    result = use_case.execute(user_id=user_id, now=datetime.now())
    return [
        {
            "reservation_id": r.reservation_id,
            "field_name": r.field_name,
            "date": r.date.isoformat(),
            "start_time": r.start_time.strftime("%H:%M"),
            "end_time": r.end_time.strftime("%H:%M"),
            "status": r.status,
        }
        for r in result
    ]


@router.delete("/reservations/{reservation_id}")
def cancel_reservation(
    reservation_id: str,
    body: CancelReservationRequest,
    use_case: CancelReservation = Depends(get_cancel_reservation_uc),
):
    try:
        result = use_case.execute(
            reservation_id=reservation_id,
            user_id=body.user_id,
            now=datetime.now(),
        )
        return {
            "reservation_id": result.reservation_id,
            "status": result.status,
            "no_show": result.no_show,
        }
    except DomainError as e:
        return domain_error_response(e)
