namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

/// <summary>Raised when a new column is added to a board via <see cref="Entities.Board.AddColumn"/>.</summary>
public sealed record ColumnAddedEvent(BoardId BoardId, ColumnId ColumnId, string ColumnName) : DomainEvent;
