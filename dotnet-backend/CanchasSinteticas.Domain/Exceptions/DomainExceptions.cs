namespace CanchasSinteticas.Domain.Exceptions;

public class DomainException(string message) : Exception(message);

public class OverlapError()
    : DomainException("The requested slot overlaps with an existing reservation.");

public class DurationError()
    : DomainException("Reservation must be at least 1 hour long.");

public class InvalidBlockError()
    : DomainException("Start and end times must be aligned to 30-minute blocks.");

public class OperatingHoursError()
    : DomainException("Reservations must be within operating hours (06:00–23:00).");

public class AdvanceNoticeError()
    : DomainException("Reservations must be made at least 1 hour in advance.");

public class ActiveLimitError()
    : DomainException("You already have 2 active reservations.");

public class FieldNotFoundError()
    : DomainException("The requested field does not exist.");

public class NotFoundError()
    : DomainException("The reservation was not found.");

public class NotAuthorizedError()
    : DomainException("You are not authorized to modify this reservation.");

public class AlreadyCancelledError()
    : DomainException("This reservation has already been cancelled.");
