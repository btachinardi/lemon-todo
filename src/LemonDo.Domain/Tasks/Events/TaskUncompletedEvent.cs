namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskUncompletedEvent(BoardTaskId BoardTaskId) : DomainEvent;
