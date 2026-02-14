namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

public sealed record ColumnReorderedEvent(BoardId BoardId, ColumnId ColumnId, int OldPosition, int NewPosition) : DomainEvent;
