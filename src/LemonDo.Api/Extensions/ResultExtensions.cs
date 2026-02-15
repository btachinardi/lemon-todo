namespace LemonDo.Api.Extensions;

using LemonDo.Api.Contracts;
using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>
/// Maps <see cref="Result{TValue, TError}"/> to minimal API <see cref="IResult"/> responses.
/// Error classification: codes ending in <c>.not_found</c> → 404,
/// <c>.validation</c> → 400, <c>.unauthorized</c> → 401, <c>.conflict</c> → 409,
/// <c>.rate_limited</c> → 429, all others → 422.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Converts a domain <see cref="Result{TValue, TError}"/> to an HTTP response.
    /// Success returns 200 with the value (or custom <paramref name="onSuccess"/> response).
    /// Failure returns a problem details response with status code based on error classification:
    /// 404 for not_found errors, 400 for validation errors, 422 for business rule violations.
    /// </summary>
    /// <typeparam name="TValue">The success value type.</typeparam>
    /// <param name="result">The domain result to convert.</param>
    /// <param name="onSuccess">Optional custom success response factory. Defaults to Ok(value).</param>
    /// <param name="httpContext">Optional HTTP context used to log domain errors for observability.</param>
    /// <returns>An <see cref="IResult"/> representing the HTTP response.</returns>
    public static IResult ToHttpResult<TValue>(this Result<TValue, DomainError> result, Func<TValue, IResult>? onSuccess = null, HttpContext? httpContext = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess(result.Value) : Results.Ok(result.Value);

        return ToErrorResponse(result.Error, httpContext);
    }

    /// <summary>
    /// Converts a domain <see cref="Result{TError}"/> (no value) to an HTTP response.
    /// Success returns 200 OK (or custom <paramref name="onSuccess"/> response).
    /// Failure returns a problem details response with status code based on error classification:
    /// 404 for not_found errors, 400 for validation errors, 422 for business rule violations.
    /// </summary>
    /// <param name="result">The domain result to convert.</param>
    /// <param name="onSuccess">Optional custom success response factory. Defaults to Ok().</param>
    /// <param name="httpContext">Optional HTTP context used to log domain errors for observability.</param>
    /// <returns>An <see cref="IResult"/> representing the HTTP response.</returns>
    public static IResult ToHttpResult(this Result<DomainError> result, Func<IResult>? onSuccess = null, HttpContext? httpContext = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess() : Results.Ok();

        return ToErrorResponse(result.Error, httpContext);
    }

    private static IResult ToErrorResponse(DomainError error, HttpContext? httpContext)
    {
        var (statusCode, type) = ClassifyError(error);

        // Log domain errors so trends are visible in structured logs
        if (httpContext is not null)
        {
            var logger = httpContext.RequestServices
                .GetService<ILoggerFactory>()?
                .CreateLogger("LemonDo.Api.DomainErrors");

            logger?.LogWarning(
                "Domain error {ErrorCode}: {ErrorMessage} (HTTP {StatusCode}) on {Method} {Path}",
                error.Code, error.Message, statusCode,
                httpContext.Request.Method, httpContext.Request.Path);
        }

        var response = new ErrorResponse(
            Type: type,
            Title: error.Message,
            Status: statusCode,
            Errors: new Dictionary<string, string[]>
            {
                [ExtractField(error.Code)] = [error.Message]
            });

        return Results.Json(response, statusCode: statusCode);
    }

    private static (int StatusCode, string Type) ClassifyError(DomainError error)
    {
        if (error.Code.EndsWith(".not_found"))
            return (404, "not_found");

        if (error.Code.EndsWith(".validation"))
            return (400, "validation_error");

        if (error.Code.EndsWith(".unauthorized"))
            return (401, "unauthorized");

        if (error.Code.EndsWith(".conflict"))
            return (409, "conflict");

        if (error.Code.EndsWith(".rate_limited"))
            return (429, "rate_limited");

        // Business rule violations → 422
        return (422, "business_rule_violation");
    }

    private static string ExtractField(string code)
    {
        var dotIndex = code.IndexOf('.');
        return dotIndex > 0 ? code[..dotIndex] : code;
    }
}
