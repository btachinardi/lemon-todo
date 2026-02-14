namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

public sealed record ColumnRemovedEvent(BoardId BoardId, ColumnId ColumnId) : DomainEvent;
