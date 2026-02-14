namespace LemonDo.Domain.Common;

/// <summary>
/// Represents a domain-level error with a code and message.
/// </summary>
public sealed record DomainError(string Code, string Message)
{
    public static DomainError NotFound(string entity, string id) =>
        new($"{entity}.not_found", $"{entity} with ID '{id}' was not found.");

    public static DomainError Validation(string field, string message) =>
        new($"{field}.validation", message);

    public static DomainError BusinessRule(string code, string message) =>
        new(code, message);
}
