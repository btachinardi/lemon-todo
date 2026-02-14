namespace LemonDo.Domain.Boards.Repositories;

using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Identity.ValueObjects;

/// <summary>
/// Repository for persisting and querying <see cref="Board"/> aggregates (including columns and cards).
/// </summary>
public interface IBoardRepository
{
    /// <summary>
    /// Returns the board with the given ID (with columns and cards loaded), or <c>null</c> if not found.
    /// </summary>
    Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct = default);

    /// <summary>
    /// Returns the first board owned by the given user, or <c>null</c> if they have no boards.
    /// In single-user mode (CP1), this returns the one default board.
    /// </summary>
    Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct = default);

    Task AddAsync(Board board, CancellationToken ct = default);
    Task UpdateAsync(Board board, CancellationToken ct = default);
}
