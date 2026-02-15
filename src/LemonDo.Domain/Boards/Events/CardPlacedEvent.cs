namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record CardPlacedEvent(
    BoardId BoardId,
    TaskId TaskId,
    ColumnId ColumnId,
    decimal Rank) : DomainEvent;
