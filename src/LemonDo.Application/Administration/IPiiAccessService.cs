namespace LemonDo.Application.Administration;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>
/// Port for audited PII decryption. Every call to this service records an audit entry.
/// This is the ONLY authorized path for decrypting encrypted PII fields.
/// </summary>
public interface IPiiAccessService
{
    /// <summary>
    /// Decrypts a user's PII for an automated system operation (e.g., sending a transactional email).
    /// Records an audit entry with <see cref="AuditAction.PiiAccessed"/> and a null actor (system).
    /// </summary>
    Task<Result<DecryptedPii, DomainError>> AccessForSystemAsync(
        Guid userId,
        SystemPiiAccessReason reason,
        string? details = null,
        CancellationToken ct = default);

    /// <summary>
    /// Decrypts a user's PII for an admin break-the-glass reveal.
    /// The caller (<see cref="Commands.RevealPiiCommandHandler"/>) is responsible for recording
    /// the admin-specific audit entry with justification, comments, and re-authentication proof.
    /// </summary>
    Task<Result<DecryptedPii, DomainError>> RevealForAdminAsync(
        Guid userId,
        CancellationToken ct = default);
}

/// <summary>Decrypted PII values returned by <see cref="IPiiAccessService"/>.</summary>
public sealed record DecryptedPii(string Email, string DisplayName);
