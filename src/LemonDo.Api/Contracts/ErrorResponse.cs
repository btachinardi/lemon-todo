namespace LemonDo.Api.Contracts;

/// <summary>Standardized error response following RFC 7807 Problem Details format.</summary>
/// <remarks>
/// Type: URI reference identifying the problem type (e.g., "validation_error").
/// Title: Short, human-readable summary of the problem.
/// Status: HTTP status code for this occurrence.
/// Errors: Optional field-level validation errors (field name -> error messages).
/// </remarks>
public sealed record ErrorResponse(
    string Type,
    string Title,
    int Status,
    Dictionary<string, string[]>? Errors = null);
