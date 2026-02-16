namespace LemonDo.Domain.Notifications.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Notifications.Entities;
using LemonDo.Domain.Notifications.ValueObjects;

/// <summary>Persistence contract for <see cref="Notification"/> aggregates.</summary>
public interface INotificationRepository
{
    /// <summary>Returns a single notification by id, or <c>null</c>.</summary>
    System.Threading.Tasks.Task<Notification?> GetByIdAsync(NotificationId id, CancellationToken ct = default);

    /// <summary>Returns the most recent notifications for a user, newest first.</summary>
    System.Threading.Tasks.Task<PagedResult<Notification>> ListAsync(
        UserId userId,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default);

    /// <summary>Returns the count of unread notifications for a user.</summary>
    System.Threading.Tasks.Task<int> GetUnreadCountAsync(UserId userId, CancellationToken ct = default);

    /// <summary>Persists a new notification.</summary>
    System.Threading.Tasks.Task AddAsync(Notification notification, CancellationToken ct = default);

    /// <summary>Marks all unread notifications as read for a user.</summary>
    System.Threading.Tasks.Task MarkAllAsReadAsync(UserId userId, CancellationToken ct = default);
}
