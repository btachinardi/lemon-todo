namespace LemonDo.Infrastructure.Persistence.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using LemonDo.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>
/// EF Core implementation of <see cref="ITaskRepository"/>. Eagerly loads tags.
/// Handles transparent encryption of sensitive notes via shadow properties.
/// </summary>
public sealed class TaskRepository(LemonDoDbContext context, IFieldEncryptionService encryptionService) : ITaskRepository
{
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(TaskId id, CancellationToken ct = default)
    {
        return await context.Tasks
            .Include(t => t.Tags)
            .FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    /// <inheritdoc/>
    /// <param name="ownerId">The task owner to filter by.</param>
    /// <param name="priority">Optional priority filter.</param>
    /// <param name="status">Optional status filter.</param>
    /// <param name="searchTerm">Optional text to search in title and description. Null or empty returns all.</param>
    /// <param name="tag">Optional tag filter (exact match, case-insensitive).</param>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="ct">Cancellation token.</param>
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
    public async System.Threading.Tasks.Task AddAsync(TaskEntity task, SensitiveNote? sensitiveNote = null, CancellationToken ct = default)
    {
        await context.Tasks.AddAsync(task, ct);

        if (sensitiveNote is not null)
        {
            var entry = context.Entry(task);
            entry.Property("EncryptedSensitiveNote").CurrentValue = encryptionService.Encrypt(sensitiveNote.Value);
        }
    }

    /// <inheritdoc/>
    public System.Threading.Tasks.Task UpdateAsync(TaskEntity task, SensitiveNote? sensitiveNote = null, bool clearSensitiveNote = false, CancellationToken ct = default)
    {
        context.Tasks.Update(task);

        if (clearSensitiveNote)
        {
            var entry = context.Entry(task);
            entry.Property("EncryptedSensitiveNote").CurrentValue = null;
        }
        else if (sensitiveNote is not null)
        {
            var entry = context.Entry(task);
            entry.Property("EncryptedSensitiveNote").CurrentValue = encryptionService.Encrypt(sensitiveNote.Value);
        }

        return System.Threading.Tasks.Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<Result<string, DomainError>> GetDecryptedSensitiveNoteAsync(TaskId taskId, CancellationToken ct = default)
    {
        var task = await context.Tasks.FirstOrDefaultAsync(t => t.Id == taskId, ct);
        if (task is null)
            return Result<string, DomainError>.Failure(DomainError.NotFound("Task", taskId.Value.ToString()));

        var encrypted = context.Entry(task).Property<string?>("EncryptedSensitiveNote").CurrentValue;
        if (encrypted is null)
            return Result<string, DomainError>.Failure(
                DomainError.NotFound("SensitiveNote", taskId.Value.ToString()));

        return Result<string, DomainError>.Success(encryptionService.Decrypt(encrypted));
    }
}
