namespace LemonDo.Domain.Notifications.Entities;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Notifications.Enums;
using LemonDo.Domain.Notifications.ValueObjects;

/// <summary>An in-app notification addressed to a specific user.</summary>
public sealed class Notification : Entity<NotificationId>
{
    /// <summary>The user this notification belongs to.</summary>
    public UserId UserId { get; }

    /// <summary>Categorisation of this notification.</summary>
    public NotificationType Type { get; }

    /// <summary>Short headline.</summary>
    public string Title { get; }

    /// <summary>Optional longer description.</summary>
    public string? Body { get; }

    /// <summary>Whether the user has read this notification.</summary>
    public bool IsRead { get; private set; }

    /// <summary>Timestamp of when the notification was read, or <c>null</c>.</summary>
    public DateTimeOffset? ReadAt { get; private set; }

    private Notification(NotificationId id, UserId userId, NotificationType type, string title, string? body)
        : base(id)
    {
        UserId = userId;
        Type = type;
        Title = title;
        Body = body;
    }

    /// <summary>EF Core constructor.</summary>
    private Notification() : base(default!)
    {
        UserId = default!;
        Title = default!;
    }

    /// <summary>Creates a new notification.</summary>
    public static Notification Create(UserId userId, NotificationType type, string title, string? body = null)
    {
        return new Notification(NotificationId.New(), userId, type, title, body);
    }

    /// <summary>Marks the notification as read. Idempotent.</summary>
    public void MarkAsRead()
    {
        if (IsRead) return;
        IsRead = true;
        ReadAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Reconstitutes a notification from persistence.</summary>
    public static Notification Reconstitute(
        NotificationId id,
        UserId userId,
        NotificationType type,
        string title,
        string? body,
        bool isRead,
        DateTimeOffset? readAt,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var notification = new Notification(id, userId, type, title, body)
        {
            IsRead = isRead,
            ReadAt = readAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
        return notification;
    }
}
