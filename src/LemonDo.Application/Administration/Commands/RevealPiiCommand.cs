namespace LemonDo.Application.Administration.Commands;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;

/// <summary>Command to reveal a user's PII (decrypted email and display name). SystemAdmin only.</summary>
public sealed record RevealPiiCommand(Guid UserId);

/// <summary>DTO returned when PII is revealed.</summary>
public sealed record RevealedPiiDto(string Email, string DisplayName);

/// <summary>Handles <see cref="RevealPiiCommand"/> by decrypting PII and recording an audit entry.</summary>
public sealed class RevealPiiCommandHandler(
    IAdminUserService adminService,
    IAuditService auditService)
{
    /// <summary>Reveals the user's PII and logs the action.</summary>
    public async Task<Result<RevealedPiiDto, DomainError>> HandleAsync(
        RevealPiiCommand command, CancellationToken ct = default)
    {
        var result = await adminService.RevealPiiAsync(command.UserId, ct);
        if (result.IsFailure)
            return Result<RevealedPiiDto, DomainError>.Failure(result.Error);

        await auditService.RecordAsync(
            AuditAction.PiiRevealed,
            "User",
            command.UserId.ToString(),
            $"Revealed PII for user {command.UserId}",
            cancellationToken: ct);

        return result;
    }
}
