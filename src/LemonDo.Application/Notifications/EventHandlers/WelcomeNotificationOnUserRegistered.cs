namespace LemonDo.Application.Notifications.EventHandlers;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Events;
using LemonDo.Domain.Notifications.Entities;
using LemonDo.Domain.Notifications.Enums;
using LemonDo.Domain.Notifications.Repositories;

/// <summary>Creates a welcome notification when a new user registers.</summary>
public sealed class WelcomeNotificationOnUserRegistered(INotificationRepository notificationRepository)
    : IDomainEventHandler<UserRegisteredEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken ct = default)
    {
        var notification = Notification.Create(
            domainEvent.UserId,
            NotificationType.Welcome,
            "Welcome to Lemon.DO!",
            "Start by creating your first task. We're glad you're here!");

        await notificationRepository.AddAsync(notification, ct);
    }
}
