namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskStatusChangedEvent(
    BoardTaskId BoardTaskId,
    BoardTaskStatus OldStatus,
    BoardTaskStatus NewStatus) : DomainEvent;
