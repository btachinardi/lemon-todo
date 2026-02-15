namespace LemonDo.Api.Contracts;

/// <summary>Standardized error envelope returned by all API error responses.</summary>
public sealed record ErrorResponse(
    string Type,
    string Title,
    int Status,
    Dictionary<string, string[]>? Errors = null);
