namespace LemonDo.Domain.Tasks.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.ValueObjects;

public interface IBoardTaskRepository
{
    Task<BoardTask?> GetByIdAsync(BoardTaskId id, CancellationToken ct = default);
    Task<IReadOnlyList<BoardTask>> GetByColumnAsync(ColumnId columnId, CancellationToken ct = default);
    Task<PagedResult<BoardTask>> ListAsync(
        UserId ownerId,
        ColumnId? columnId = null,
        Priority? priority = null,
        BoardTaskStatus? status = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);
    Task AddAsync(BoardTask task, CancellationToken ct = default);
    Task UpdateAsync(BoardTask task, CancellationToken ct = default);
}
