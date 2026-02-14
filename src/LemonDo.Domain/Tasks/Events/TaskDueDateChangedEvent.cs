namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskDueDateChangedEvent(
    TaskItemId TaskItemId,
    DateTimeOffset? OldDueDate,
    DateTimeOffset? NewDueDate) : DomainEvent;
