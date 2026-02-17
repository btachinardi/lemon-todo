namespace LemonDo.Api.Serialization;

using LemonDo.Domain.Common;

/// <summary>
/// Thrown by <see cref="EncryptedField"/> JSON converters when domain validation fails
/// during deserialization (e.g., invalid email format, display name too short).
/// Caught by <see cref="LemonDo.Api.Middleware.ErrorHandlingMiddleware"/> and returned as a 400 response.
/// </summary>
public sealed class ProtectedDataValidationException(DomainError error)
    : Exception(error.Message)
{
    /// <summary>The domain validation error that caused this exception.</summary>
    public DomainError Error { get; } = error;
}
