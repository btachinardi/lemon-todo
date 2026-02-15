namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

public sealed class TaskRepository(LemonDoDbContext context) : ITaskRepository
{
    public async System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(TaskId id, CancellationToken ct = default)
    {
        return await context.Tasks
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async System.Threading.Tasks.Task<PagedResult<TaskEntity>> ListAsync(
        UserId ownerId,
        Priority? priority = null,
        TaskStatus? status = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = context.Tasks
            .Include(t => t.Tags)
            .Where(t => t.OwnerId == ownerId && !t.IsDeleted);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => EF.Property<string>(t, "Title").Contains(searchTerm));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TaskEntity>(items, totalCount, page, pageSize);
    }

    public async System.Threading.Tasks.Task<HashSet<TaskId>> GetActiveTaskIdsAsync(UserId ownerId, CancellationToken ct = default)
    {
        var ids = await context.Tasks
            .Where(t => t.OwnerId == ownerId && !t.IsDeleted && !t.IsArchived)
            .Select(t => t.Id)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    public async System.Threading.Tasks.Task AddAsync(TaskEntity task, CancellationToken ct = default)
    {
        await context.Tasks.AddAsync(task, ct);
    }

    public System.Threading.Tasks.Task UpdateAsync(TaskEntity task, CancellationToken ct = default)
    {
        context.Tasks.Update(task);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
