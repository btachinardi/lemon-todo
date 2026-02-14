namespace LemonDo.Api.Contracts;

public sealed record ErrorResponse(
    string Type,
    string Title,
    int Status,
    Dictionary<string, string[]>? Errors = null);
