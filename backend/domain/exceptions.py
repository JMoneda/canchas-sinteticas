class DomainError(Exception):
    def __init__(self, message: str = "A domain rule was violated"):
        super().__init__(message)
        self.message = message


class OverlapError(DomainError):
    def __init__(self):
        super().__init__("This time slot overlaps with an existing reservation on the selected field.")


class DurationError(DomainError):
    def __init__(self):
        super().__init__("Reservations must be at least 1 hour long.")


class InvalidBlockError(DomainError):
    def __init__(self):
        super().__init__("Reservation times must align to 30-minute boundaries (e.g., 10:00, 10:30, 11:00).")


class OperatingHoursError(DomainError):
    def __init__(self):
        super().__init__("Reservations must start at or after 06:00 and end at or before 23:00.")


class AdvanceNoticeError(DomainError):
    def __init__(self):
        super().__init__("Reservations must be created at least 1 hour before the start time.")


class ActiveLimitError(DomainError):
    def __init__(self):
        super().__init__("You already have 2 active reservations. Cancel one before making a new reservation.")


class FieldNotFoundError(DomainError):
    def __init__(self):
        super().__init__("The requested field does not exist.")


class NotFoundError(DomainError):
    def __init__(self):
        super().__init__("No reservation found with the provided identifier.")


class NotAuthorizedError(DomainError):
    def __init__(self):
        super().__init__("This reservation does not belong to the provided user identifier.")


class AlreadyCancelledError(DomainError):
    def __init__(self):
        super().__init__("This reservation has already been cancelled.")
