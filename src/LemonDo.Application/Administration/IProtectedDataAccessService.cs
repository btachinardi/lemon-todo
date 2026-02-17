namespace LemonDo.Application.Administration;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>
/// Port for audited protected data access. Every call to this service records an audit entry.
/// This is the ONLY authorized path for accessing encrypted protected data fields.
/// Returns <see cref="RevealedProtectedData"/> containing <see cref="RevealedField"/> values
/// that are decrypted only at JSON serialization time.
/// </summary>
public interface IProtectedDataAccessService
{
    /// <summary>
    /// Retrieves a user's protected data for an automated system operation (e.g., sending a transactional email).
    /// Records an audit entry with <see cref="AuditAction.ProtectedDataAccessed"/> and a null actor (system).
    /// System operations get <see cref="DecryptedProtectedData"/> with raw values since there's no HTTP response to serialize into.
    /// </summary>
    Task<Result<DecryptedProtectedData, DomainError>> AccessForSystemAsync(
        Guid userId,
        SystemProtectedDataAccessReason reason,
        string? details = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user's protected data for an admin break-the-glass reveal.
    /// The caller (<see cref="Commands.RevealProtectedDataCommandHandler"/>) is responsible for recording
    /// the admin-specific audit entry with justification, comments, and re-authentication proof.
    /// Returns <see cref="RevealedProtectedData"/> — decryption deferred to JSON serialization.
    /// </summary>
    Task<Result<RevealedProtectedData, DomainError>> RevealForAdminAsync(
        Guid userId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves a user's own protected data after password re-authentication.
    /// The caller (<see cref="LemonDo.Application.Identity.Commands.RevealOwnProfileCommandHandler"/>) is responsible
    /// for recording the audit entry.
    /// Returns <see cref="RevealedProtectedData"/> — decryption deferred to JSON serialization.
    /// </summary>
    Task<Result<RevealedProtectedData, DomainError>> RevealForOwnerAsync(
        Guid userId,
        CancellationToken ct = default);
}

/// <summary>Decrypted protected data values returned by <see cref="IProtectedDataAccessService.AccessForSystemAsync"/>.</summary>
public sealed record DecryptedProtectedData(string Email, string DisplayName);

/// <summary>Revealed protected data with deferred decryption, returned by reveal operations.</summary>
public sealed record RevealedProtectedData(RevealedField Email, RevealedField DisplayName);
