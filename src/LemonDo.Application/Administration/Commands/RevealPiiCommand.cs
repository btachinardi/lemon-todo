namespace LemonDo.Application.Administration.Commands;

using System.Text.Json;
using LemonDo.Application.Common;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>
/// Command to reveal a user's PII with break-the-glass controls.
/// Requires a justification reason, optional comments, and the admin's own password for re-authentication.
/// </summary>
public sealed record RevealPiiCommand(
    Guid UserId,
    PiiRevealReason Reason,
    string? ReasonDetails,
    string? Comments,
    string AdminPassword);

/// <summary>DTO returned when PII is revealed.</summary>
public sealed record RevealedPiiDto(string Email, string DisplayName);

/// <summary>Structured audit details for PII reveal actions, serialized as JSON in the audit trail.</summary>
public sealed record PiiRevealAuditDetails(
    string Reason,
    string? ReasonDetails,
    string? Comments);

/// <summary>
/// Handles <see cref="RevealPiiCommand"/> with break-the-glass controls:
/// validates justification, re-authenticates the admin, reveals PII, and records a structured audit entry.
/// </summary>
public sealed class RevealPiiCommandHandler(
    IAdminUserService adminService,
    IAuditService auditService,
    IRequestContext requestContext)
{
    /// <summary>Reveals the user's PII after validation and re-authentication.</summary>
    public async Task<Result<RevealedPiiDto, DomainError>> HandleAsync(
        RevealPiiCommand command, CancellationToken ct = default)
    {
        // 1. Validate: "Other" reason requires details
        if (command.Reason == PiiRevealReason.Other
            && string.IsNullOrWhiteSpace(command.ReasonDetails))
        {
            return Result<RevealedPiiDto, DomainError>.Failure(
                DomainError.Validation("reasonDetails",
                    "Reason details are required when reason is 'Other'."));
        }

        // 2. Re-authenticate the acting admin
        var adminUserId = requestContext.UserId
            ?? throw new InvalidOperationException("No authenticated user in request context.");
        var passwordResult = await adminService.VerifyAdminPasswordAsync(
            adminUserId, command.AdminPassword, ct);
        if (passwordResult.IsFailure)
            return Result<RevealedPiiDto, DomainError>.Failure(passwordResult.Error);

        // 3. Reveal PII
        var result = await adminService.RevealPiiAsync(command.UserId, ct);
        if (result.IsFailure)
            return Result<RevealedPiiDto, DomainError>.Failure(result.Error);

        // 4. Record structured audit entry
        var auditDetails = new PiiRevealAuditDetails(
            command.Reason.ToString(),
            command.ReasonDetails,
            command.Comments);

        await auditService.RecordAsync(
            AuditAction.PiiRevealed,
            "User",
            command.UserId.ToString(),
            JsonSerializer.Serialize(auditDetails),
            cancellationToken: ct);

        return result;
    }
}
