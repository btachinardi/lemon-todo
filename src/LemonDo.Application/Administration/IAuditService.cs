namespace LemonDo.Application.Administration;

using LemonDo.Domain.Administration;

/// <summary>
/// Service for recording audit trail entries. Abstracts the audit entry repository
/// and request context so domain event handlers can record auditable actions.
/// </summary>
public interface IAuditService
{
    /// <summary>Records an auditable action with context from the current request.</summary>
    Task RecordAsync(
        AuditAction action,
        string resourceType,
        string? resourceId = null,
        string? details = null,
        Guid? actorIdOverride = null,
        CancellationToken cancellationToken = default);
}
