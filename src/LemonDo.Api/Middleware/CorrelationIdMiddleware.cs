using System.Text.RegularExpressions;
using Serilog.Context;

namespace LemonDo.Api.Middleware;

/// <summary>
/// Extracts or generates a correlation ID for each request. Sets it on
/// <see cref="HttpContext.TraceIdentifier"/>, adds it to response headers,
/// and pushes it to Serilog's <see cref="LogContext"/> so all log entries
/// include the correlation ID.
/// </summary>
/// <remarks>
/// Security: incoming X-Correlation-Id values are sanitised before use.
/// Values are truncated to <see cref="MaxCorrelationIdLength"/> characters and
/// stripped of any characters that are not alphanumeric, hyphens, or underscores.
/// If the sanitised value is empty, a new GUID is generated instead.
/// This prevents log injection, header injection, XSS reflection, and log bloat.
/// </remarks>
public sealed partial class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";
    private const int MaxCorrelationIdLength = 128;

    [GeneratedRegex("[^a-zA-Z0-9\\-_]")]
    private static partial Regex InvalidCorrelationIdChars();

    /// <summary>Processes the request, enriching it with a correlation ID.</summary>
    /// <remarks>
    /// Side effects: mutates HttpContext.TraceIdentifier with the correlation ID,
    /// adds X-Correlation-Id response header, and pushes CorrelationId to Serilog LogContext
    /// for downstream log entries. Reuses client-provided X-Correlation-Id header if present
    /// (after sanitisation), otherwise generates a new GUID.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        var raw = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        var correlationId = Sanitize(raw);

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

    private static string Sanitize(string? raw)
    {
        if (string.IsNullOrEmpty(raw))
            return Guid.NewGuid().ToString("N");

        // Truncate first to avoid running the regex over arbitrarily large input
        if (raw.Length > MaxCorrelationIdLength)
            raw = raw[..MaxCorrelationIdLength];

        // Strip any character that is not alphanumeric, hyphen, or underscore
        var sanitized = InvalidCorrelationIdChars().Replace(raw, string.Empty);

        // If nothing remains after stripping, generate a fresh ID
        return string.IsNullOrEmpty(sanitized)
            ? Guid.NewGuid().ToString("N")
            : sanitized;
    }
}
