namespace LemonDo.Api.Middleware;

using System.Diagnostics;
using System.Net;
using System.Text.Json;
using LemonDo.Api.Serialization;

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
    /// <remarks>
    /// Side effect: logs unhandled exceptions with request context (method, path, elapsed time)
    /// for observability and debugging.
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        var startTime = Stopwatch.GetTimestamp();
        try
        {
            await next(context);
        }
        catch (ProtectedDataValidationException pdex)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            logger.LogWarning(
                "Protected data validation failed on {Method} {Path} after {ElapsedMs:F1}ms: {ErrorCode}",
                context.Request.Method,
                context.Request.Path,
                elapsedMs,
                pdex.Error.Code);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";

            var field = pdex.Error.Code.Contains('.') ? pdex.Error.Code[..pdex.Error.Code.IndexOf('.')] : pdex.Error.Code;
            var response = new
            {
                type = "validation_error",
                title = pdex.Error.Message,
                status = 400,
                errors = new Dictionary<string, string[]>
                {
                    [field] = [pdex.Error.Message]
                }
            };

            await context.Response.WriteAsJsonAsync(response);
        }
        catch (Microsoft.AspNetCore.Http.BadHttpRequestException badReq)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            logger.LogWarning(
                "Bad request on {Method} {Path} after {ElapsedMs:F1}ms: {Message}",
                context.Request.Method,
                context.Request.Path,
                elapsedMs,
                badReq.Message);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "validation_error",
                title = "The request body is invalid.",
                status = 400
            });
        }
        catch (System.Text.Json.JsonException jsonEx)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            logger.LogWarning(
                "JSON parse error on {Method} {Path} after {ElapsedMs:F1}ms: {Message}",
                context.Request.Method,
                context.Request.Path,
                elapsedMs,
                jsonEx.Message);

            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "validation_error",
                title = "The request body contains invalid JSON.",
                status = 400
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException concurrencyEx)
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            logger.LogWarning(
                "Concurrency conflict on {Method} {Path} after {ElapsedMs:F1}ms: {Message}",
                context.Request.Method, context.Request.Path, elapsedMs, concurrencyEx.Message);

            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "concurrency_conflict",
                title = "The resource was modified by another request. Please retry.",
                status = 409
            });
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException dbEx)
            when (IsConstraintViolation(dbEx))
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            logger.LogWarning(
                "Duplicate key conflict on {Method} {Path} after {ElapsedMs:F1}ms: {Message}",
                context.Request.Method, context.Request.Path, elapsedMs, dbEx.InnerException?.Message);

            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "duplicate_conflict",
                title = "A conflicting resource already exists.",
                status = 409
            });
        }
        catch (Exception ex)
            when (Infrastructure.Resilience.SqliteTransientFaultDetector.IsTransient(ex))
        {
            var elapsedMs = Stopwatch.GetElapsedTime(startTime).TotalMilliseconds;
            logger.LogWarning(
                "SQLite transient fault on {Method} {Path} after {ElapsedMs:F1}ms: {Message}",
                context.Request.Method, context.Request.Path, elapsedMs, ex.Message);

            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "concurrency_conflict",
                title = "The resource was modified by another request. Please retry.",
                status = 409
            });
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

    /// <summary>
    /// Returns true when a <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> is
    /// caused by a database constraint violation (UNIQUE, PRIMARY KEY, etc.).
    /// Handles both SQL Server ("duplicate key", "unique index") and SQLite
    /// (error code 19 — SQLITE_CONSTRAINT — which covers all constraint types).
    /// </summary>
    private static bool IsConstraintViolation(Microsoft.EntityFrameworkCore.DbUpdateException ex)
    {
        var inner = ex.InnerException;
        if (inner is null) return false;

        // SQL Server / generic providers: message contains well-known keywords
        var msg = inner.Message;
        if (msg.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            || msg.Contains("unique index", StringComparison.OrdinalIgnoreCase))
            return true;

        // SQLite: SqliteException with ErrorCode 19 (SQLITE_CONSTRAINT) covers
        // UNIQUE, PRIMARY KEY, FOREIGN KEY, CHECK, and NOT NULL violations.
        // Concurrent inserts hitting the same PK/UNIQUE index raise error 19.
        if (inner is Microsoft.Data.Sqlite.SqliteException sqliteEx && sqliteEx.SqliteErrorCode == 19)
            return true;

        return false;
    }

}
