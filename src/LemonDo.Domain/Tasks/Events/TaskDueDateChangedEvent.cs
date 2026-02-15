namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task's due date is set or cleared via <see cref="Entities.Task.SetDueDate"/>.</summary>
public sealed record TaskDueDateChangedEvent(
    TaskId TaskId,
    DateTimeOffset? OldDueDate,
    DateTimeOffset? NewDueDate) : DomainEvent;
