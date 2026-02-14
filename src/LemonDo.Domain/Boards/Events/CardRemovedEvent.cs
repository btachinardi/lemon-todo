namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record CardRemovedEvent(
    BoardId BoardId,
    TaskId TaskId) : DomainEvent;
