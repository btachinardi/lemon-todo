namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

/// <summary>Raised when a column is renamed via <see cref="Entities.Board.RenameColumn"/>.</summary>
public sealed record ColumnRenamedEvent(BoardId BoardId, ColumnId ColumnId, string Name) : DomainEvent;
