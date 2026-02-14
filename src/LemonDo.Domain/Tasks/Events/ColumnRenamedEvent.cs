namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record ColumnRenamedEvent(BoardId BoardId, ColumnId ColumnId, string Name) : DomainEvent;
