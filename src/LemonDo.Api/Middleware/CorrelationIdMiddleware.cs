namespace LemonDo.Api.Middleware;

/// <summary>
/// Extracts or generates a correlation ID for each request. Sets it on
/// <see cref="HttpContext.TraceIdentifier"/>, adds it to response headers,
/// and pushes a logging scope so all log entries include the correlation ID.
/// </summary>
public sealed class CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>Processes the request, enriching it with a correlation ID.</summary>
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

        using (logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId
        }))
        {
            await next(context);
        }
    }
}
