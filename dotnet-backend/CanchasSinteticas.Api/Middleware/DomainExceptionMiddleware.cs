using System.Text.Json;
using CanchasSinteticas.Domain.Exceptions;

namespace CanchasSinteticas.Api.Middleware;

public class DomainExceptionMiddleware(RequestDelegate next)
{
    private static readonly Dictionary<Type, (string Code, int Status)> ErrorMap = new()
    {
        [typeof(OverlapError)]         = ("OVERLAP", 422),
        [typeof(DurationError)]        = ("DURATION_INVALID", 422),
        [typeof(InvalidBlockError)]    = ("INVALID_BLOCK", 422),
        [typeof(OperatingHoursError)]  = ("OPERATING_HOURS", 422),
        [typeof(AdvanceNoticeError)]   = ("ADVANCE_NOTICE", 422),
        [typeof(ActiveLimitError)]     = ("ACTIVE_LIMIT", 422),
        [typeof(FieldNotFoundError)]   = ("FIELD_NOT_FOUND", 422),
        [typeof(NotFoundError)]        = ("NOT_FOUND", 404),
        [typeof(NotAuthorizedError)]   = ("NOT_AUTHORIZED", 403),
        [typeof(AlreadyCancelledError)]= ("ALREADY_CANCELLED", 400),
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex) when (ErrorMap.TryGetValue(ex.GetType(), out var mapped))
        {
            context.Response.StatusCode = mapped.Status;
            context.Response.ContentType = "application/json";
            var body = JsonSerializer.Serialize(new
            {
                error_type = mapped.Code,
                message = ex.Message,
            });
            await context.Response.WriteAsync(body);
        }
    }
}
