namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

/// <summary>Raised when a column's position changes via <see cref="Entities.Board.ReorderColumn"/>.</summary>
public sealed record ColumnReorderedEvent(BoardId BoardId, ColumnId ColumnId, int OldPosition, int NewPosition) : DomainEvent;
