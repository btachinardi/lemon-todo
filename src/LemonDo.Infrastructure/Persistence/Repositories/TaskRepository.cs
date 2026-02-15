namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>EF Core implementation of <see cref="ITaskRepository"/>. Eagerly loads tags.</summary>
public sealed class TaskRepository(LemonDoDbContext context) : ITaskRepository
{
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(TaskId id, CancellationToken ct = default)
    {
        return await context.Tasks
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<PagedResult<TaskEntity>> ListAsync(
        UserId ownerId,
        Priority? priority = null,
        TaskStatus? status = null,
        string? searchTerm = null,
        string? tag = null,
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
            query = query.Where(t =>
                EF.Property<string>(t, "Title").Contains(searchTerm) ||
                (EF.Property<string?>(t, "Description") != null && EF.Property<string>(t, "Description").Contains(searchTerm)));

        if (!string.IsNullOrWhiteSpace(tag))
            query = query.Where(t => t.Tags.Any(tg => EF.Property<string>(tg, "Value") == tag));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TaskEntity>(items, totalCount, page, pageSize);
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<HashSet<TaskId>> GetActiveTaskIdsAsync(UserId ownerId, CancellationToken ct = default)
    {
        var ids = await context.Tasks
            .Where(t => t.OwnerId == ownerId && !t.IsDeleted && !t.IsArchived)
            .Select(t => t.Id)
            .ToListAsync(ct);

        return ids.ToHashSet();
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task AddAsync(TaskEntity task, CancellationToken ct = default)
    {
        await context.Tasks.AddAsync(task, ct);
    }

    /// <inheritdoc/>
    public System.Threading.Tasks.Task UpdateAsync(TaskEntity task, CancellationToken ct = default)
    {
        context.Tasks.Update(task);
        return System.Threading.Tasks.Task.CompletedTask;
    }
}
