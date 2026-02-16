namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Administration;
using LemonDo.Application.Common;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>
/// Command for a user to reveal their own protected profile data (email and display name).
/// Requires password re-authentication for security.
/// </summary>
public sealed record RevealOwnProfileCommand(string Password);

/// <summary>
/// Handles <see cref="RevealOwnProfileCommand"/>: re-authenticates the user,
/// decrypts their protected data, and records an audit entry.
/// </summary>
public sealed class RevealOwnProfileCommandHandler(
    IAuthService authService,
    IProtectedDataAccessService protectedDataService,
    IAuditService auditService,
    ICurrentUserService currentUser)
{
    /// <summary>Decrypts and returns the user's own email and display name after password verification.</summary>
    public async Task<Result<DecryptedProtectedData, DomainError>> HandleAsync(
        RevealOwnProfileCommand command, CancellationToken ct = default)
    {
        // Re-authenticate the user
        var passwordResult = await authService.VerifyPasswordAsync(
            currentUser.UserId.Value, command.Password, ct);
        if (passwordResult.IsFailure)
            return Result<DecryptedProtectedData, DomainError>.Failure(passwordResult.Error);

        // Decrypt
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
