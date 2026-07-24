using System.Text.Json;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Api.Middleware;

/// <summary>
/// Traduce las excepciones de dominio a respuestas HTTP con un cuerpo JSON
/// uniforme <c>{ error_type, message }</c>.
/// </summary>
public class DomainExceptionMiddleware(RequestDelegate next)
{
    private static readonly Dictionary<Type, (string Code, int Status)> ErrorMap = new()
    {
        [typeof(OverlapError)] = ("OVERLAP", 422),
        [typeof(DurationError)] = ("DURATION_INVALID", 422),
        [typeof(InvalidBlockError)] = ("INVALID_BLOCK", 422),
        [typeof(OperatingHoursError)] = ("OPERATING_HOURS", 422),
        [typeof(AdvanceNoticeError)] = ("ADVANCE_NOTICE", 422),
        [typeof(ActiveLimitError)] = ("ACTIVE_LIMIT", 422),
        [typeof(BlackoutConflictError)] = ("BLACKOUT_CONFLICT", 422),
        [typeof(NoPriceConfiguredError)] = ("NO_PRICE", 422),
        [typeof(ValidationError)] = ("VALIDATION", 422),
        [typeof(VenueNotFoundError)] = ("VENUE_NOT_FOUND", 404),
        [typeof(CourtNotFoundError)] = ("COURT_NOT_FOUND", 404),
        [typeof(NotFoundError)] = ("NOT_FOUND", 404),
        [typeof(NotAuthorizedError)] = ("NOT_AUTHORIZED", 403),
        [typeof(AlreadyCancelledError)] = ("ALREADY_CANCELLED", 400),
        [typeof(EmailAlreadyExistsError)] = ("EMAIL_EXISTS", 409),
        [typeof(InvalidCredentialsError)] = ("INVALID_CREDENTIALS", 401),
        [typeof(MatchNotOpenError)] = ("MATCH_NOT_OPEN", 422),
        [typeof(AlreadyJoinedError)] = ("ALREADY_JOINED", 409),
        [typeof(MatchFullError)] = ("MATCH_FULL", 409),
        [typeof(OrganizerCannotLeaveError)] = ("ORGANIZER_CANNOT_LEAVE", 422),
        [typeof(NotJoinedError)] = ("NOT_JOINED", 422),
        [typeof(InvalidPaymentTransitionError)] = ("INVALID_PAYMENT_TRANSITION", 409),
        [typeof(PaymentGatewayError)] = ("PAYMENT_GATEWAY_ERROR", 502),
    };

    /// <summary>Ejecuta el siguiente middleware capturando excepciones de dominio.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            var mapped = ErrorMap.TryGetValue(ex.GetType(), out var found)
                ? found
                : ("DOMAIN_ERROR", 422);

            context.Response.StatusCode = mapped.Item2;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new
            {
                error_type = mapped.Item1,
                message = ex.Message,
            });
            await context.Response.WriteAsync(body);
        }
    }
}
