namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task transitions between lifecycle statuses via <see cref="Entities.Task.SetStatus"/>.</summary>
public sealed record TaskStatusChangedEvent(
    TaskId TaskId,
    TaskStatus OldStatus,
    TaskStatus NewStatus) : DomainEvent;
