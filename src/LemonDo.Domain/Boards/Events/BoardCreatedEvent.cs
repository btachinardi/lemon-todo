namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>Raised when a new board is created via <see cref="Entities.Board.CreateDefault"/> or <see cref="Entities.Board.Create"/>.</summary>
public sealed record BoardCreatedEvent(BoardId BoardId, UserId OwnerId) : DomainEvent;
