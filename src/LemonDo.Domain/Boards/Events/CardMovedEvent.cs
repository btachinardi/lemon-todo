namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record CardMovedEvent(
    BoardId BoardId,
    TaskId TaskId,
    ColumnId FromColumnId,
    ColumnId ToColumnId,
    decimal NewRank) : DomainEvent;
