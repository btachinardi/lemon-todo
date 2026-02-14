namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskDeletedEvent(
    BoardTaskId BoardTaskId,
    DateTimeOffset DeletedAt) : DomainEvent;
