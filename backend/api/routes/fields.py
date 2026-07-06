from datetime import date, datetime

from fastapi import APIRouter, Depends, Query
from fastapi.responses import JSONResponse

from api.dependencies import get_list_slots_uc
from api.error_handling import domain_error_response
from application.use_cases.list_available_slots import ListAvailableSlots
from domain.exceptions import DomainError

router = APIRouter()


@router.get("/fields/availability")
def get_field_availability(
    date: date = Query(...),
    use_case: ListAvailableSlots = Depends(get_list_slots_uc),
):
    now = datetime.now()
    if date < now.date():
        return JSONResponse(
            status_code=400,
            content={
                "error_type": "INVALID_DATE",
                "message": "The requested date is in the past. Please choose today or a future date.",
            },
        )
    try:
        result = use_case.execute(query_date=date, now=now)
        return [
            {
                "field_id": f.field_id,
                "field_name": f.field_name,
                "available_slots": [
                    {
                        "start_time": s.start_time.strftime("%H:%M"),
                        "end_time": s.end_time.strftime("%H:%M"),
                    }
                    for s in f.available_slots
                ],
            }
            for f in result
        ]
    except DomainError as e:
        return domain_error_response(e)
