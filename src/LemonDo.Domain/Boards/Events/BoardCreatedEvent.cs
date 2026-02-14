namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

public sealed record BoardCreatedEvent(BoardId BoardId, UserId OwnerId) : DomainEvent;
