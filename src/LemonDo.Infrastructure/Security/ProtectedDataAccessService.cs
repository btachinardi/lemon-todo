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
/// The ONLY authorized path for decrypting protected data from encrypted shadow properties on the <c>Users</c> table.
/// Every decryption is recorded in the audit trail — whether initiated by an admin or by the system.
/// </summary>
public sealed class ProtectedDataAccessService(
    LemonDoDbContext dbContext,
    IFieldEncryptionService encryptionService,
    IAuditService auditService,
    ILogger<ProtectedDataAccessService> logger) : IProtectedDataAccessService
{
    /// <inheritdoc />
    public async Task<Result<DecryptedProtectedData, DomainError>> AccessForSystemAsync(
        Guid userId, SystemProtectedDataAccessReason reason, string? details, CancellationToken ct)
    {
        var result = await DecryptProtectedDataAsync(userId, ct);
        if (result.IsFailure)
            return result;

        logger.LogInformation("System protected data access for user {UserId}: {Reason}", userId, reason);

        await auditService.RecordAsync(
            AuditAction.ProtectedDataAccessed,
            "User",
            userId.ToString(),
            JsonSerializer.Serialize(new { Reason = reason.ToString(), Details = details }),
            actorIdOverride: null, // null = system actor
            cancellationToken: ct);

        return result;
    }

    /// <inheritdoc />
    public async Task<Result<DecryptedProtectedData, DomainError>> RevealForAdminAsync(
        Guid userId, CancellationToken ct)
    {
        // Audit is recorded by the calling RevealProtectedDataCommandHandler with admin-specific context
        return await DecryptProtectedDataAsync(userId, ct);
    }

    /// <inheritdoc />
    public async Task<Result<DecryptedProtectedData, DomainError>> RevealForOwnerAsync(
        Guid userId, CancellationToken ct)
    {
        // Audit is recorded by the calling RevealOwnProfileCommandHandler
        return await DecryptProtectedDataAsync(userId, ct);
    }

    private async Task<Result<DecryptedProtectedData, DomainError>> DecryptProtectedDataAsync(
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
            return Result<DecryptedProtectedData, DomainError>.Failure(
                DomainError.NotFound("user", userId.ToString()));

        var email = encryptionService.Decrypt(encryptedData.EncryptedEmail);
        var displayName = encryptionService.Decrypt(encryptedData.EncryptedDisplayName);

        return Result<DecryptedProtectedData, DomainError>.Success(new DecryptedProtectedData(email, displayName));
    }
}
