namespace LemonDo.Application.Boards.EventHandlers;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Events;

/// <summary>
/// Reacts to <see cref="UserRegisteredEvent"/> by creating a default board for the new user.
/// This keeps the Board context (downstream) decoupled from the Identity context (upstream).
/// </summary>
public sealed class CreateDefaultBoardOnUserRegistered(
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork) : IDomainEventHandler<UserRegisteredEvent>
{
    /// <inheritdoc />
    public async Task HandleAsync(UserRegisteredEvent domainEvent, CancellationToken ct = default)
    {
        var boardResult = Board.CreateDefault(domainEvent.UserId);
        if (boardResult.IsSuccess)
        {
            await boardRepository.AddAsync(boardResult.Value, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
