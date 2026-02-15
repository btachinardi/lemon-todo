namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task's priority level changes via <see cref="Entities.Task.SetPriority"/>.</summary>
public sealed record TaskPriorityChangedEvent(
    TaskId TaskId,
    Priority OldPriority,
    Priority NewPriority) : DomainEvent;
