namespace LemonDo.Application.Administration;

using LemonDo.Application.Common;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Administration.Entities;
using LemonDo.Domain.Administration.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>
/// Records audit trail entries by combining domain action details
/// with request context (actor ID, IP, user agent).
/// </summary>
public sealed class AuditService(
    IAuditEntryRepository repository,
    IRequestContext requestContext,
    ILogger<AuditService> logger) : IAuditService
{
    /// <inheritdoc />
    public async Task RecordAsync(
        AuditAction action,
        string resourceType,
        string? resourceId = null,
        string? details = null,
        Guid? actorIdOverride = null,
        CancellationToken cancellationToken = default)
    {
        var actorId = actorIdOverride ?? requestContext.UserId;

        var entry = AuditEntry.Create(
            actorId,
            action,
            resourceType,
            resourceId,
            details,
            requestContext.IpAddress,
            requestContext.UserAgent);

        await repository.AddAsync(entry, cancellationToken);

        logger.LogInformation(
            "Audit: {Action} on {ResourceType}/{ResourceId} by {ActorId}",
            action, resourceType, resourceId ?? "N/A", actorId?.ToString() ?? "system");
    }
}
