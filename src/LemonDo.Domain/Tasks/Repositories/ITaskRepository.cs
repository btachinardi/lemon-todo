namespace LemonDo.Domain.Tasks.Repositories;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>
/// Repository for persisting and querying <see cref="TaskEntity"/> aggregates.
/// Handles transparent encryption of sensitive notes via shadow properties.
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
    /// matches against the task title and description.
    /// </summary>
    System.Threading.Tasks.Task<PagedResult<TaskEntity>> ListAsync(
        UserId ownerId,
        Priority? priority = null,
        TaskStatus? status = null,
        string? searchTerm = null,
        string? tag = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Returns the IDs of all tasks that are neither deleted nor archived for the given user.
    /// Used by board queries to filter out cards for inactive tasks.
    /// </summary>
    System.Threading.Tasks.Task<HashSet<TaskId>> GetActiveTaskIdsAsync(UserId ownerId, CancellationToken ct = default);

    /// <summary>
    /// Persists a new task aggregate. If <paramref name="sensitiveNote"/> is provided,
    /// encrypts it and stores it as a shadow property.
    /// </summary>
    System.Threading.Tasks.Task AddAsync(TaskEntity task, SensitiveNote? sensitiveNote = null, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing task aggregate as modified. If <paramref name="sensitiveNote"/> is provided,
    /// encrypts and updates the shadow property. Pass <c>null</c> with <paramref name="clearSensitiveNote"/>
    /// set to <c>true</c> to remove the note.
    /// </summary>
    System.Threading.Tasks.Task UpdateAsync(TaskEntity task, SensitiveNote? sensitiveNote = null, bool clearSensitiveNote = false, CancellationToken ct = default);

    /// <summary>
    /// Decrypts and returns the sensitive note for a task, or a not-found error if the task has no note.
    /// </summary>
    System.Threading.Tasks.Task<Result<string, DomainError>> GetDecryptedSensitiveNoteAsync(TaskId taskId, CancellationToken ct = default);
}
