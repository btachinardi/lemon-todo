namespace LemonDo.Application.Analytics.EventHandlers;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Events;

/// <summary>Tracks a <c>task_created</c> analytics event when a task is created.</summary>
public sealed class AnalyticsOnTaskCreated(IAnalyticsService analytics) : IDomainEventHandler<TaskCreatedEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(TaskCreatedEvent domainEvent, CancellationToken ct = default)
    {
        await analytics.TrackAsync(
            "task_created",
            domainEvent.OwnerId.Value,
            new Dictionary<string, string> { ["priority"] = domainEvent.Priority.ToString() },
            ct);
    }
}
