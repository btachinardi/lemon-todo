namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskCreatedEvent(
    TaskId TaskId,
    UserId OwnerId,
    string Title,
    Priority Priority) : DomainEvent;
