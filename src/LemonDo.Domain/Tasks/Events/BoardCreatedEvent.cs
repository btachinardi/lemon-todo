namespace LemonDo.Domain.Tasks.Events;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record BoardCreatedEvent(BoardId BoardId, UserId OwnerId) : DomainEvent;
