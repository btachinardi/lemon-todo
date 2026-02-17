namespace LemonDo.Domain.Notifications.Enums;

/// <summary>Categorises the reason a notification was created.</summary>
public enum NotificationType
{
    /// <summary>A task is due within the next 24 hours.</summary>
    DueDateReminder = 0,

    /// <summary>A task has passed its due date.</summary>
    TaskOverdue = 1,

    /// <summary>Welcome notification sent after registration.</summary>
    Welcome = 2,
}
