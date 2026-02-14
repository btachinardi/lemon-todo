namespace LemonDo.Domain.Tasks.Repositories;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.ValueObjects;

public interface ITaskItemRepository
{
    Task<TaskItem?> GetByIdAsync(TaskItemId id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskItem>> GetByColumnAsync(ColumnId columnId, CancellationToken ct = default);
    Task AddAsync(TaskItem task, CancellationToken ct = default);
    Task UpdateAsync(TaskItem task, CancellationToken ct = default);
}
