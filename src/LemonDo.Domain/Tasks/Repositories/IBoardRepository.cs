namespace LemonDo.Domain.Tasks.Repositories;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.ValueObjects;

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct = default);
    Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct = default);
    Task AddAsync(Board board, CancellationToken ct = default);
    Task UpdateAsync(Board board, CancellationToken ct = default);
}
