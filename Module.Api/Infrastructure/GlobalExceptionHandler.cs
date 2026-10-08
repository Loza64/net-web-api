using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Module.Application.Common;

namespace Module.Api.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        int status;
        string title;
        string? detail;

        if (exception is AppException app)
        {
            status = app.StatusCode; title = app.Title; detail = app.Message;
        }
        else
        {
            logger.LogError(exception, "Unhandled exception");
            status = StatusCodes.Status500InternalServerError;
            title = "Error interno";
            detail = null;
        }

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = status, Title = title, Detail = detail }, ct);
        return true;
    }
}
