namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task card is initially placed on a board via <see cref="Entities.Board.PlaceTask"/>.</summary>
public sealed record CardPlacedEvent(
    BoardId BoardId,
    TaskId TaskId,
    ColumnId ColumnId,
    decimal Rank) : DomainEvent;
