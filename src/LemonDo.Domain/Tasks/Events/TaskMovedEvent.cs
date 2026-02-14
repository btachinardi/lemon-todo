namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskMovedEvent(
    BoardTaskId BoardTaskId,
    ColumnId FromColumnId,
    ColumnId ToColumnId,
    int NewPosition) : DomainEvent;
