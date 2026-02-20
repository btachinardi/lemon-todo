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
    /// Returns the board with the given ID owned by the specified user, or <c>null</c> if not found or not owned.
    /// </summary>
    Task<Board?> GetByIdAsync(BoardId id, UserId ownerId, CancellationToken ct = default);

    /// <summary>
    /// Returns the first board owned by the given user, or <c>null</c> if they have no boards.
    /// In single-user mode (CP1), this returns the one default board.
    /// </summary>
    Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct = default);

    /// <summary>Persists a new board aggregate.</summary>
    Task AddAsync(Board board, CancellationToken ct = default);

    /// <summary>Marks an existing board aggregate as modified for the next unit-of-work commit.</summary>
    Task UpdateAsync(Board board, CancellationToken ct = default);
}
