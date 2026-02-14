namespace LemonDo.Domain.Tasks.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

public interface ITaskRepository
{
    System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(TaskId id, CancellationToken ct = default);
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
