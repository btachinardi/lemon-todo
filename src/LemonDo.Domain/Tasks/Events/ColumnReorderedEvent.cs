namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record ColumnReorderedEvent(BoardId BoardId, ColumnId ColumnId, int OldPosition, int NewPosition) : DomainEvent;
