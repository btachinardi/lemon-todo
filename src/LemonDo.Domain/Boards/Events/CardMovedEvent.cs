namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a card is moved between columns or reordered via <see cref="Entities.Board.MoveCard"/>.</summary>
public sealed record CardMovedEvent(
    BoardId BoardId,
    TaskId TaskId,
    ColumnId FromColumnId,
    ColumnId ToColumnId,
    decimal NewRank) : DomainEvent;
