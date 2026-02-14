namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;

public sealed record ColumnAddedEvent(BoardId BoardId, ColumnId ColumnId, string ColumnName) : DomainEvent;
