namespace LemonDo.Domain.Administration.Repositories;

using LemonDo.Domain.Administration.Entities;
using LemonDo.Domain.Common;

/// <summary>Repository interface for audit trail persistence and querying.</summary>
public interface IAuditEntryRepository
{
    /// <summary>Persists a new audit entry.</summary>
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches audit entries with optional filters and pagination.
    /// </summary>
    Task<PagedResult<AuditEntry>> SearchAsync(
        DateTimeOffset? dateFrom = null,
        DateTimeOffset? dateTo = null,
        AuditAction? action = null,
        Guid? actorId = null,
        string? resourceType = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
}
