namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

/// <summary>Raised when a column is removed from a board via <see cref="Entities.Board.RemoveColumn"/>.</summary>
public sealed record ColumnRemovedEvent(BoardId BoardId, ColumnId ColumnId) : DomainEvent;
