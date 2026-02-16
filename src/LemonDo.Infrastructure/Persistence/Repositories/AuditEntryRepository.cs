namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Administration;
using LemonDo.Domain.Administration.Entities;
using LemonDo.Domain.Administration.Repositories;
using LemonDo.Domain.Common;
using Microsoft.EntityFrameworkCore;

/// <summary>EF Core implementation of <see cref="IAuditEntryRepository"/>.</summary>
public sealed class AuditEntryRepository(LemonDoDbContext context) : IAuditEntryRepository
{
    /// <inheritdoc />
    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken = default)
    {
        await context.AuditEntries.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PagedResult<AuditEntry>> SearchAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        AuditAction? action,
        Guid? actorId,
        string? resourceType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = context.AuditEntries.AsNoTracking().AsQueryable();

        if (dateFrom.HasValue)
            query = query.Where(e => e.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(e => e.CreatedAt <= dateTo.Value);

        if (action.HasValue)
            query = query.Where(e => e.Action == action.Value);

        if (actorId.HasValue)
            query = query.Where(e => e.ActorId == actorId.Value);

        if (!string.IsNullOrWhiteSpace(resourceType))
            query = query.Where(e => e.ResourceType == resourceType);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditEntry>(items, totalCount, page, pageSize);
    }
}
