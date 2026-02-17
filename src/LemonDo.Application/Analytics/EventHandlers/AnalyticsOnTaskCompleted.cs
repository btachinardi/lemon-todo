namespace LemonDo.Application.Analytics.EventHandlers;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Events;
using TaskStatus = LemonDo.Domain.Tasks.ValueObjects.TaskStatus;

/// <summary>Tracks a <c>task_completed</c> analytics event when a task transitions to Done.</summary>
public sealed class AnalyticsOnTaskCompleted(IAnalyticsService analytics) : IDomainEventHandler<TaskStatusChangedEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(TaskStatusChangedEvent domainEvent, CancellationToken ct = default)
    {
        if (domainEvent.NewStatus != TaskStatus.Done) return;

        await analytics.TrackAsync(
            "task_completed",
            properties: new Dictionary<string, string>
            {
                ["from_status"] = domainEvent.OldStatus.ToString(),
            },
            ct: ct);
    }
}
