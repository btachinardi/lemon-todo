namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Notifications.Entities;
using LemonDo.Domain.Notifications.Repositories;
using LemonDo.Domain.Notifications.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>EF Core implementation of <see cref="INotificationRepository"/>.</summary>
public sealed class NotificationRepository(LemonDoDbContext context) : INotificationRepository
{
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken ct = default)
    {
        return await context.Notifications.FirstOrDefaultAsync(n => n.Id == id, ct);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<PagedResult<Notification>> ListAsync(
        UserId userId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = context.Notifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<Notification>(items, totalCount, page, pageSize);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default)
    {
        return await context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task AddAsync(Notification notification, CancellationToken ct = default)
    {
        await context.Notifications.AddAsync(notification, ct);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task MarkAllAsReadAsync(UserId userId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now)
                .SetProperty(n => n.UpdatedAt, now), ct);
    }
}
