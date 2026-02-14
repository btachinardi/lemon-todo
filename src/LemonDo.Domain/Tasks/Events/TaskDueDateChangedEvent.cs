namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskDueDateChangedEvent(
    BoardTaskId BoardTaskId,
    DateTimeOffset? OldDueDate,
    DateTimeOffset? NewDueDate) : DomainEvent;
