namespace LemonDo.Domain.Tasks.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>
/// Repository for persisting and querying <see cref="TaskEntity"/> aggregates.
/// </summary>
public interface ITaskRepository
{
    /// <summary>
    /// Returns the task with the given ID, or <c>null</c> if not found.
    /// </summary>
    System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(TaskId id, CancellationToken ct = default);

    /// <summary>
    /// Returns a paginated, filtered list of tasks owned by the given user.
    /// Filters are optional and combined with AND logic. The <paramref name="searchTerm"/>
    /// matches against the task title.
    /// </summary>
    System.Threading.Tasks.Task<PagedResult<TaskEntity>> ListAsync(
        UserId ownerId,
        Priority? priority = null,
        TaskStatus? status = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    System.Threading.Tasks.Task AddAsync(TaskEntity task, CancellationToken ct = default);
    System.Threading.Tasks.Task UpdateAsync(TaskEntity task, CancellationToken ct = default);
}
