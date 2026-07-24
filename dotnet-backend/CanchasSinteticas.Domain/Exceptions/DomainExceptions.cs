namespace CanchasSinteticas.Domain.Exceptions;

/// <summary>Excepción base para violaciones de reglas de negocio.</summary>
public class DomainException(string message) : Exception(message);

/// <summary>La franja solicitada se solapa con una reserva existente.</summary>
public class OverlapError()
    : DomainException("La franja solicitada se solapa con una reserva existente.");

/// <summary>La duración de la reserva no es válida para la cancha.</summary>
public class DurationError()
    : DomainException("La duración de la reserva no coincide con la duración de bloque de la cancha.");

/// <summary>Las horas de inicio/fin no forman un bloque válido.</summary>
public class InvalidBlockError()
    : DomainException("Las horas de inicio y fin no forman un bloque válido.");

/// <summary>La reserva queda fuera del horario de operación de la sede.</summary>
public class OperatingHoursError()
    : DomainException("La reserva está fuera del horario de operación de la sede.");

/// <summary>No se respeta la anticipación mínima para reservar.</summary>
public class AdvanceNoticeError()
    : DomainException("La reserva debe hacerse con al menos 1 hora de anticipación.");

/// <summary>El cliente superó el límite de reservas activas.</summary>
public class ActiveLimitError()
    : DomainException("Ya tienes el máximo de reservas activas permitidas.");

/// <summary>La sede solicitada no existe.</summary>
public class VenueNotFoundError()
    : DomainException("La sede solicitada no existe.");

/// <summary>La cancha solicitada no existe.</summary>
public class CourtNotFoundError()
    : DomainException("La cancha solicitada no existe.");

/// <summary>La reserva no fue encontrada.</summary>
public class NotFoundError()
    : DomainException("El recurso solicitado no fue encontrado.");

/// <summary>El usuario no está autorizado a modificar el recurso.</summary>
public class NotAuthorizedError()
    : DomainException("No tienes autorización para modificar este recurso.");

/// <summary>La reserva ya estaba cancelada.</summary>
public class AlreadyCancelledError()
    : DomainException("Esta reserva ya fue cancelada.");

/// <summary>La franja está bloqueada por mantenimiento o evento.</summary>
public class BlackoutConflictError()
    : DomainException("La cancha está bloqueada en la franja solicitada.");

/// <summary>No hay una tarifa configurada para la franja solicitada.</summary>
public class NoPriceConfiguredError()
    : DomainException("No hay una tarifa configurada para la franja solicitada.");

/// <summary>Ya existe una cuenta con el correo indicado.</summary>
public class EmailAlreadyExistsError()
    : DomainException("Ya existe una cuenta registrada con ese correo.");

/// <summary>Las credenciales proporcionadas son inválidas.</summary>
public class InvalidCredentialsError()
    : DomainException("Correo o contraseña incorrectos.");

/// <summary>Error de validación de datos de entrada.</summary>
public class ValidationError(string message) : DomainException(message);

/// <summary>El partido no está abierto para unirse.</summary>
public class MatchNotOpenError()
    : DomainException("El partido no está abierto para unirse.");

/// <summary>El jugador ya está inscrito en el partido.</summary>
public class AlreadyJoinedError()
    : DomainException("Ya estás inscrito en este partido.");

/// <summary>El partido ya completó sus cupos.</summary>
public class MatchFullError()
    : DomainException("El partido ya está completo.");

/// <summary>El organizador no puede abandonar su propio partido.</summary>
public class OrganizerCannotLeaveError()
    : DomainException("El organizador no puede salir del partido; debe cancelar la reserva.");

/// <summary>El jugador no está inscrito en el partido.</summary>
public class NotJoinedError()
    : DomainException("No estás inscrito en este partido.");
