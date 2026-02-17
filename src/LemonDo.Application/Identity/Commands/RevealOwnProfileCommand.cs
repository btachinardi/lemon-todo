namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Administration;
using LemonDo.Application.Common;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>
/// Command for a user to reveal their own protected profile data (email and display name).
/// Requires password re-authentication for security.
/// </summary>
public sealed record RevealOwnProfileCommand(ProtectedValue Password);

/// <summary>
/// Handles <see cref="RevealOwnProfileCommand"/>: re-authenticates the user,
/// retrieves their protected data as RevealedFields, and records an audit entry.
/// </summary>
public sealed class RevealOwnProfileCommandHandler(
    IAuthService authService,
    IProtectedDataAccessService protectedDataService,
    IAuditService auditService,
    ICurrentUserService currentUser)
{
    /// <summary>Returns the user's own email and display name as RevealedFields after password verification.</summary>
    public async Task<Result<RevealedProtectedData, DomainError>> HandleAsync(
        RevealOwnProfileCommand command, CancellationToken ct = default)
    {
        // Re-authenticate the user
        var passwordResult = await authService.VerifyPasswordAsync(
            currentUser.UserId.Value, command.Password.Value, ct);
        if (passwordResult.IsFailure)
            return Result<RevealedProtectedData, DomainError>.Failure(passwordResult.Error);

        // Retrieve as RevealedFields (decryption deferred to JSON serialization)
        var result = await protectedDataService.RevealForOwnerAsync(
            currentUser.UserId.Value, ct);
        if (result.IsFailure)
            return result;

        // Audit
        await auditService.RecordAsync(
            AuditAction.OwnProfileRevealed,
            "User",
            currentUser.UserId.Value.ToString(),
            "User revealed their own protected profile data",
            cancellationToken: ct);

        return result;
    }
}
