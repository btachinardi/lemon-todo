namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

public sealed record ColumnRenamedEvent(BoardId BoardId, ColumnId ColumnId, string Name) : DomainEvent;
