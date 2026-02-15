namespace LemonDo.Api.Extensions;

using LemonDo.Api.Contracts;
using LemonDo.Domain.Common;

/// <summary>
/// Maps <see cref="Result{TValue, TError}"/> to minimal API <see cref="IResult"/> responses.
/// Error classification: codes ending in <c>.not_found</c> → 404,
/// <c>.validation</c> → 400, all others → 422.
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
    /// <returns>An <see cref="IResult"/> representing the HTTP response.</returns>
    public static IResult ToHttpResult<TValue>(this Result<TValue, DomainError> result, Func<TValue, IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess(result.Value) : Results.Ok(result.Value);

        return ToErrorResponse(result.Error);
    }

    /// <summary>
    /// Converts a domain <see cref="Result{TError}"/> (no value) to an HTTP response.
    /// Success returns 200 OK (or custom <paramref name="onSuccess"/> response).
    /// Failure returns a problem details response with status code based on error classification:
    /// 404 for not_found errors, 400 for validation errors, 422 for business rule violations.
    /// </summary>
    /// <param name="result">The domain result to convert.</param>
    /// <param name="onSuccess">Optional custom success response factory. Defaults to Ok().</param>
    /// <returns>An <see cref="IResult"/> representing the HTTP response.</returns>
    public static IResult ToHttpResult(this Result<DomainError> result, Func<IResult>? onSuccess = null)
    {
        if (result.IsSuccess)
            return onSuccess is not null ? onSuccess() : Results.Ok();

        return ToErrorResponse(result.Error);
    }

    private static IResult ToErrorResponse(DomainError error)
    {
        var (statusCode, type) = ClassifyError(error);

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

        // Business rule violations → 422
        return (422, "business_rule_violation");
    }

    private static string ExtractField(string code)
    {
        var dotIndex = code.IndexOf('.');
        return dotIndex > 0 ? code[..dotIndex] : code;
    }
}
