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
    /// Returns the task with the given ID owned by the specified user, or <c>null</c> if not found or not owned.
    /// </summary>
    System.Threading.Tasks.Task<TaskEntity?> GetByIdAsync(TaskId id, UserId ownerId, CancellationToken ct = default);

    /// <summary>
    /// Returns the task with the given ID regardless of owner. Reserved for admin break-the-glass operations.
    /// </summary>
    System.Threading.Tasks.Task<TaskEntity?> GetByIdUnfilteredAsync(TaskId id, CancellationToken ct = default);

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
    /// Persists a new task aggregate. If <paramref name="encryptedNote"/> is provided,
    /// stores its encrypted value as a shadow property.
    /// </summary>
    System.Threading.Tasks.Task AddAsync(TaskEntity task, EncryptedField? encryptedNote = null, CancellationToken ct = default);

    /// <summary>
    /// Marks an existing task aggregate as modified. If <paramref name="encryptedNote"/> is provided,
    /// stores its encrypted value in the shadow property. Pass <c>null</c> with <paramref name="clearSensitiveNote"/>
    /// set to <c>true</c> to remove the note.
    /// </summary>
    /// <param name="task">The modified task aggregate to persist.</param>
    /// <param name="encryptedNote">Pre-encrypted note, or null to leave unchanged.</param>
    /// <param name="clearSensitiveNote">When true, removes the sensitive note regardless of the encryptedNote parameter value.</param>
    /// <param name="ct">Cancellation token.</param>
    System.Threading.Tasks.Task UpdateAsync(TaskEntity task, EncryptedField? encryptedNote = null, bool clearSensitiveNote = false, CancellationToken ct = default);

    /// <summary>
    /// Decrypts and returns the sensitive note for a task, or a not-found error if the task has no note.
    /// </summary>
    System.Threading.Tasks.Task<Result<string, DomainError>> GetDecryptedSensitiveNoteAsync(TaskId taskId, CancellationToken ct = default);

    /// <summary>
    /// Returns the encrypted sensitive note as a <see cref="RevealedField"/> for JSON-level decryption,
    /// or a not-found error if the task has no note.
    /// </summary>
    System.Threading.Tasks.Task<Result<RevealedField, DomainError>> GetEncryptedSensitiveNoteAsync(TaskId taskId, CancellationToken ct = default);
}
