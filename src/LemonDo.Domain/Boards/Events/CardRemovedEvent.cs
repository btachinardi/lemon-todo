namespace LemonDo.Domain.Boards.Events;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Raised when a task card is removed from a board via <see cref="Entities.Board.RemoveCard"/>.</summary>
public sealed record CardRemovedEvent(
    BoardId BoardId,
    TaskId TaskId) : DomainEvent;
