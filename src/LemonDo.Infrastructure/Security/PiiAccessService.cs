namespace LemonDo.Infrastructure.Security;

using System.Text.Json;
using LemonDo.Application.Administration;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

/// <summary>
/// The ONLY authorized path for decrypting PII from encrypted shadow properties on the <c>Users</c> table.
/// Every decryption is recorded in the audit trail — whether initiated by an admin or by the system.
/// </summary>
public sealed class PiiAccessService(
    LemonDoDbContext dbContext,
    IFieldEncryptionService encryptionService,
    IAuditService auditService,
    ILogger<PiiAccessService> logger) : IPiiAccessService
{
    /// <inheritdoc />
    public async Task<Result<DecryptedPii, DomainError>> AccessForSystemAsync(
        Guid userId, SystemPiiAccessReason reason, string? details, CancellationToken ct)
    {
        var result = await DecryptPiiAsync(userId, ct);
        if (result.IsFailure)
            return result;

        logger.LogInformation("System PII access for user {UserId}: {Reason}", userId, reason);

        await auditService.RecordAsync(
            AuditAction.PiiAccessed,
            "User",
            userId.ToString(),
            JsonSerializer.Serialize(new { Reason = reason.ToString(), Details = details }),
            actorIdOverride: null, // null = system actor
            cancellationToken: ct);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<DecryptedPii, DomainError>> RevealForAdminAsync(
        Guid userId, CancellationToken ct)
    {
        // Audit is recorded by the calling RevealPiiCommandHandler with admin-specific context
        return await DecryptPiiAsync(userId, ct);
    }

    private async Task<Result<DecryptedPii, DomainError>> DecryptPiiAsync(
        Guid userId, CancellationToken ct)
    {
        var targetId = UserId.Reconstruct(userId);

        var encryptedData = await dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == targetId)
            .Select(u => new
            {
                EncryptedEmail = EF.Property<string>(u, "EncryptedEmail"),
                EncryptedDisplayName = EF.Property<string>(u, "EncryptedDisplayName"),
            })
            .FirstOrDefaultAsync(ct);

        if (encryptedData is null)
            return Result<DecryptedPii, DomainError>.Failure(
                DomainError.NotFound("user", userId.ToString()));

        var email = encryptionService.Decrypt(encryptedData.EncryptedEmail);
        var displayName = encryptionService.Decrypt(encryptedData.EncryptedDisplayName);

        return Result<DecryptedPii, DomainError>.Success(new DecryptedPii(email, displayName));
    }
}
