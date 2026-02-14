namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskStatusChangedEvent(
    TaskId TaskId,
    TaskStatus OldStatus,
    TaskStatus NewStatus) : DomainEvent;
