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
/// The ONLY authorized path for accessing protected data from encrypted shadow properties on the <c>Users</c> table.
/// Every access is recorded in the audit trail — whether initiated by an admin or by the system.
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
        var encrypted = await GetEncryptedDataAsync(userId, ct);
        if (encrypted is null)
            return Result<DecryptedProtectedData, DomainError>.Failure(
                DomainError.NotFound("user", userId.ToString()));

        logger.LogInformation("System protected data access for user {UserId}: {Reason}", userId, reason);

        await auditService.RecordAsync(
            AuditAction.ProtectedDataAccessed,
            "User",
            userId.ToString(),
            JsonSerializer.Serialize(new { Reason = reason.ToString(), Details = details }),
            actorIdOverride: null, // null = system actor
            cancellationToken: ct);

        // System access decrypts immediately (no HTTP response to serialize into)
        var email = encryptionService.Decrypt(encrypted.Value.EncryptedEmail);
        var displayName = encryptionService.Decrypt(encrypted.Value.EncryptedDisplayName);

        return Result<DecryptedProtectedData, DomainError>.Success(new DecryptedProtectedData(email, displayName));
    }

    /// <inheritdoc />
    public async Task<Result<RevealedProtectedData, DomainError>> RevealForAdminAsync(
        Guid userId, CancellationToken ct)
    {
        // Audit is recorded by the calling RevealProtectedDataCommandHandler with admin-specific context
        return await GetRevealedDataAsync(userId, ct);
    }

    /// <inheritdoc />
    public async Task<Result<RevealedProtectedData, DomainError>> RevealForOwnerAsync(
        Guid userId, CancellationToken ct)
    {
        // Audit is recorded by the calling RevealOwnProfileCommandHandler
        return await GetRevealedDataAsync(userId, ct);
    }

    private async Task<Result<RevealedProtectedData, DomainError>> GetRevealedDataAsync(
        Guid userId, CancellationToken ct)
    {
        var encrypted = await GetEncryptedDataAsync(userId, ct);
        if (encrypted is null)
            return Result<RevealedProtectedData, DomainError>.Failure(
                DomainError.NotFound("user", userId.ToString()));

        // Return RevealedFields — decryption deferred to JSON serialization
        return Result<RevealedProtectedData, DomainError>.Success(
            new RevealedProtectedData(
                new RevealedField(encrypted.Value.EncryptedEmail),
                new RevealedField(encrypted.Value.EncryptedDisplayName)));
    }

    private async Task<(string EncryptedEmail, string EncryptedDisplayName)?> GetEncryptedDataAsync(
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
            return null;

        return (encryptedData.EncryptedEmail, encryptedData.EncryptedDisplayName);
    }
}
