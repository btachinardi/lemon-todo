using Serilog.Context;

namespace LemonDo.Api.Middleware;

/// <summary>
/// Extracts or generates a correlation ID for each request. Sets it on
/// <see cref="HttpContext.TraceIdentifier"/>, adds it to response headers,
/// and pushes it to Serilog's <see cref="LogContext"/> so all log entries
/// include the correlation ID.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>Processes the request, enriching it with a correlation ID.</summary>
    /// <remarks>
    /// Side effects: mutates HttpContext.TraceIdentifier with the correlation ID,
    /// adds X-Correlation-Id response header, and pushes CorrelationId to Serilog LogContext
    /// for downstream log entries. Reuses client-provided X-Correlation-Id header if present,
    /// otherwise generates a new GUID.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
