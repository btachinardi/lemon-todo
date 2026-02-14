namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.EntityFrameworkCore;

public sealed class TaskItemRepository(LemonDoDbContext context) : ITaskItemRepository
{
    public async Task<TaskItem?> GetByIdAsync(TaskItemId id, CancellationToken ct = default)
    {
        return await context.Tasks.FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<IReadOnlyList<TaskItem>> GetByColumnAsync(ColumnId columnId, CancellationToken ct = default)
    {
        return await context.Tasks
            .Where(t => t.ColumnId == columnId && !t.IsDeleted)
            .OrderBy(t => t.Position)
            .ToListAsync(ct);
    }

    public async Task<PagedResult<TaskItem>> ListAsync(
        UserId ownerId,
        ColumnId? columnId = null,
        Priority? priority = null,
        TaskItemStatus? status = null,
        string? searchTerm = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default)
    {
        var query = context.Tasks
            .Where(t => t.OwnerId == ownerId && !t.IsDeleted);

        if (columnId is not null)
            query = query.Where(t => t.ColumnId == columnId);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
            query = query.Where(t => t.Title.Value.Contains(searchTerm));

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TaskItem>(items, totalCount, page, pageSize);
    }

    public async Task AddAsync(TaskItem task, CancellationToken ct = default)
    {
        await context.Tasks.AddAsync(task, ct);
    }

    public Task UpdateAsync(TaskItem task, CancellationToken ct = default)
    {
        context.Tasks.Update(task);
        return Task.CompletedTask;
    }
}
