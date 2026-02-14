namespace LemonDo.Domain.Boards.Repositories;

using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Identity.ValueObjects;

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct = default);
    Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct = default);
    Task AddAsync(Board board, CancellationToken ct = default);
    Task UpdateAsync(Board board, CancellationToken ct = default);
}
