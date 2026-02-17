namespace LemonDo.Application.Administration.Commands;

using System.Text.Json;
using LemonDo.Application.Common;
using LemonDo.Application.Identity;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>
/// Command to reveal a user's protected data with break-the-glass controls.
/// Requires a justification reason, optional comments, and the admin's own password for re-authentication.
/// </summary>
public sealed record RevealProtectedDataCommand(
    Guid UserId,
    ProtectedDataRevealReason Reason,
    string? ReasonDetails,
    string? Comments,
    ProtectedValue AdminPassword);

/// <summary>DTO returned when protected data is revealed. RevealedField values are decrypted at JSON serialization.</summary>
public sealed record RevealedProtectedDataDto(RevealedField Email, RevealedField DisplayName);

/// <summary>Structured audit details for protected data reveal actions, serialized as JSON in the audit trail.</summary>
public sealed record ProtectedDataRevealAuditDetails(
    string Reason,
    string? ReasonDetails,
    string? Comments);

/// <summary>
/// Handles <see cref="RevealProtectedDataCommand"/> with break-the-glass controls:
/// validates justification, re-authenticates the admin, reveals protected data, and records a structured audit entry.
/// </summary>
public sealed class RevealProtectedDataCommandHandler(
    IAuthService authService,
    IProtectedDataAccessService protectedDataAccessService,
    IAuditService auditService,
    IRequestContext requestContext)
{
    /// <summary>Reveals the user's protected data after validation and re-authentication.</summary>
    public async Task<Result<RevealedProtectedDataDto, DomainError>> HandleAsync(
        RevealProtectedDataCommand command, CancellationToken ct = default)
    {
        // 1. Validate: "Other" reason requires details
        if (command.Reason == ProtectedDataRevealReason.Other
            && string.IsNullOrWhiteSpace(command.ReasonDetails))
        {
            return Result<RevealedProtectedDataDto, DomainError>.Failure(
                DomainError.Validation("reasonDetails",
                    "Reason details are required when reason is 'Other'."));
        }

        // 2. Re-authenticate the acting admin
        var adminUserId = requestContext.UserId
            ?? throw new InvalidOperationException("No authenticated user in request context.");
        var passwordResult = await authService.VerifyPasswordAsync(
            adminUserId, command.AdminPassword.Value, ct);
        if (passwordResult.IsFailure)
            return Result<RevealedProtectedDataDto, DomainError>.Failure(passwordResult.Error);

        // 3. Reveal protected data via the audited access service (returns RevealedFields)
        var result = await protectedDataAccessService.RevealForAdminAsync(command.UserId, ct);
        if (result.IsFailure)
            return Result<RevealedProtectedDataDto, DomainError>.Failure(result.Error);

        // 4. Record structured audit entry (admin-specific context: reason, comments)
        var auditDetails = new ProtectedDataRevealAuditDetails(
            command.Reason.ToString(),
            command.ReasonDetails,
            command.Comments);

        await auditService.RecordAsync(
            AuditAction.ProtectedDataRevealed,
            "User",
            command.UserId.ToString(),
            JsonSerializer.Serialize(auditDetails),
            cancellationToken: ct);

        return Result<RevealedProtectedDataDto, DomainError>.Success(
            new RevealedProtectedDataDto(result.Value.Email, result.Value.DisplayName));
    }
}
