namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record ColumnAddedEvent(BoardId BoardId, ColumnId ColumnId, string ColumnName) : DomainEvent;
