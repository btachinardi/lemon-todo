namespace LemonDo.Domain.Common;

/// <summary>
/// Represents a domain-level error with a machine-readable code and human-readable message.
/// </summary>
/// <remarks>
/// Error codes follow a dotted convention that determines HTTP status mapping in the API layer:
/// codes ending in <c>.not_found</c> map to 404, <c>.validation</c> to 400, and all others to 422.
/// Use the factory methods below or the constructor directly for custom codes.
/// </remarks>
public sealed record DomainError(string Code, string Message)
{
    /// <summary>
    /// Creates a not-found error. The resulting code is <c>{entity}.not_found</c>.
    /// </summary>
    public static DomainError NotFound(string entity, string id) =>
        new($"{entity}.not_found", $"{entity} with ID '{id}' was not found.");

    /// <summary>
    /// Creates a validation error. The resulting code is <c>{field}.validation</c>.
    /// </summary>
    public static DomainError Validation(string field, string message) =>
        new($"{field}.validation", message);

    /// <summary>
    /// Creates a business rule violation. The code is used as-is (no suffix appended).
    /// </summary>
    public static DomainError BusinessRule(string code, string message) =>
        new(code, message);
}
