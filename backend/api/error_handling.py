from fastapi.responses import JSONResponse

from domain.exceptions import (
    ActiveLimitError,
    AdvanceNoticeError,
    AlreadyCancelledError,
    DomainError,
    DurationError,
    FieldNotFoundError,
    InvalidBlockError,
    NotAuthorizedError,
    NotFoundError,
    OperatingHoursError,
    OverlapError,
)

_ERROR_MAP = {
    OverlapError: ("OVERLAP", 422),
    DurationError: ("DURATION_INVALID", 422),
    InvalidBlockError: ("INVALID_BLOCK", 422),
    OperatingHoursError: ("OPERATING_HOURS", 422),
    AdvanceNoticeError: ("ADVANCE_NOTICE", 422),
    ActiveLimitError: ("ACTIVE_LIMIT", 422),
    FieldNotFoundError: ("FIELD_NOT_FOUND", 422),
    NotFoundError: ("NOT_FOUND", 404),
    NotAuthorizedError: ("NOT_AUTHORIZED", 403),
    AlreadyCancelledError: ("ALREADY_CANCELLED", 400),
}


def domain_error_response(exc: DomainError) -> JSONResponse:
    error_type, status_code = _ERROR_MAP.get(type(exc), ("DOMAIN_ERROR", 422))
    return JSONResponse(
        status_code=status_code,
        content={"error_type": error_type, "message": exc.message},
    )
