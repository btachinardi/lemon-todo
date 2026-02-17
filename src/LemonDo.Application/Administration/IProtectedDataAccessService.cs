namespace LemonDo.Application.Administration;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>
/// Port for audited protected data decryption. Every call to this service records an audit entry.
/// This is the ONLY authorized path for decrypting encrypted protected data fields.
/// </summary>
public interface IProtectedDataAccessService
{
    /// <summary>
    /// Decrypts a user's protected data for an automated system operation (e.g., sending a transactional email).
    /// Records an audit entry with <see cref="AuditAction.ProtectedDataAccessed"/> and a null actor (system).
    /// </summary>
    Task<Result<DecryptedProtectedData, DomainError>> AccessForSystemAsync(
        Guid userId,
        SystemProtectedDataAccessReason reason,
        string? details = null,
        CancellationToken ct = default);

    /// <summary>
    /// Decrypts a user's protected data for an admin break-the-glass reveal.
    /// The caller (<see cref="Commands.RevealProtectedDataCommandHandler"/>) is responsible for recording
    /// the admin-specific audit entry with justification, comments, and re-authentication proof.
    /// </summary>
    Task<Result<DecryptedProtectedData, DomainError>> RevealForAdminAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Decrypts a user's own protected data after password re-authentication.
    /// The caller (<see cref="LemonDo.Application.Identity.Commands.RevealOwnProfileCommandHandler"/>) is responsible
    /// for recording the audit entry.
    /// </summary>
    Task<Result<DecryptedProtectedData, DomainError>> RevealForOwnerAsync(
        Guid userId,
        CancellationToken ct = default);
}

/// <summary>Decrypted protected data values returned by <see cref="IProtectedDataAccessService"/>.</summary>
public sealed record DecryptedProtectedData(string Email, string DisplayName);
