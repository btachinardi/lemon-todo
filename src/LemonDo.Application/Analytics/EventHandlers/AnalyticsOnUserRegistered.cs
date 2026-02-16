namespace LemonDo.Application.Analytics.EventHandlers;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Events;

/// <summary>Tracks a <c>user_registered</c> analytics event when a user signs up.</summary>
public sealed class AnalyticsOnUserRegistered(IAnalyticsService analytics) : IDomainEventHandler<UserRegisteredEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken ct = default)
    {
        await analytics.TrackAsync(
            "user_registered",
            domainEvent.UserId.Value,
            ct: ct);
    }
}
