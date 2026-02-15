namespace LemonDo.Api.Middleware;

using System.Diagnostics;
using System.Net;
using System.Text.Json;

/// <summary>
/// Catches unhandled exceptions and returns a structured JSON error response (500).
/// In development, includes the full exception detail in the response body.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    /// <summary>
    /// Processes the HTTP request and catches any unhandled exceptions.
    /// Returns a standardized JSON error response (500) with structured problem details.
    /// In development mode, includes the full exception stack trace in the response detail field.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            logger.LogError(ex,
                "Unhandled exception on {Method} {Path} after {ElapsedMs:F1}ms",
                context.Request.Method,
                context.Request.Path,
                elapsedMs);

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var isDev = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true;
            var response = new
            {
                type = "internal_server_error",
                title = "An unexpected error occurred.",
                status = 500,
                correlationId = context.TraceIdentifier,
                detail = isDev ? ex.ToString() : null
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
