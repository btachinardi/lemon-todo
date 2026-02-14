namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskDueDateChangedEvent(
    TaskId TaskId,
    DateTimeOffset? OldDueDate,
    DateTimeOffset? NewDueDate) : DomainEvent;
