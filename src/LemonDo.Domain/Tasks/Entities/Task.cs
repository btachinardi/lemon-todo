namespace LemonDo.Domain.Tasks.Entities;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Events;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Aggregate root for the Task bounded context. Owns the task lifecycle (status, priority,
/// tags, archival) but not spatial placement — that belongs to the Board context.
/// </summary>
/// <remarks>
/// <para>All mutation methods enforce a soft-delete guard: deleted tasks reject edits.</para>
/// <para>Status transitions are managed via <see cref="SetStatus"/>, which also maintains
/// <see cref="CompletedAt"/>. <see cref="IsArchived"/> is independent of status.</para>
/// </remarks>
public sealed class Task : Entity<TaskId>
{
    public UserId OwnerId { get; }
    public TaskTitle Title { get; private set; }
    public TaskDescription? Description { get; private set; }
    public Priority Priority { get; private set; }
    public TaskStatus Status { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    private Task(
        TaskId id,
        UserId ownerId,
        TaskTitle title,
        TaskDescription? description,
        Priority priority,
        DateTimeOffset? dueDate,
        IEnumerable<Tag>? tags) : base(id)
    {
        OwnerId = ownerId;
        Title = title;
        Description = description;
        Priority = priority;
        Status = TaskStatus.Todo;
        DueDate = dueDate;
        IsArchived = false;
        IsDeleted = false;

        if (tags is not null)
            _tags.AddRange(tags);

        RaiseDomainEvent(new TaskCreatedEvent(id, ownerId, title.Value, priority));
    }

    // EF Core constructor
    private Task() : base(default!) { OwnerId = default!; Title = default!; }

    /// <summary>
    /// Factory method that creates a new task with <see cref="TaskStatus.Todo"/> status.
    /// Raises <see cref="Events.TaskCreatedEvent"/>.
    /// </summary>
    public static Result<Task, DomainError> Create(
        UserId ownerId,
        TaskTitle title,
        TaskDescription? description = null,
        Priority priority = Priority.None,
        DateTimeOffset? dueDate = null,
        IEnumerable<Tag>? tags = null)
    {
        var id = TaskId.New();
        return Result<Task, DomainError>.Success(
            new Task(id, ownerId, title, description, priority, dueDate, tags));
    }

    /// <summary>
    /// Transitions the task to a new status. No-ops if already in that status.
    /// </summary>
    /// <remarks>
    /// Side effects: sets <see cref="CompletedAt"/> when transitioning to Done;
    /// clears <see cref="CompletedAt"/> when leaving Done.
    /// </remarks>
    public Result<DomainError> SetStatus(TaskStatus newStatus)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        var oldStatus = Status;
        if (oldStatus == newStatus)
            return Result<DomainError>.Success();

        Status = newStatus;

        if (newStatus == TaskStatus.Done && oldStatus != TaskStatus.Done)
            CompletedAt = DateTimeOffset.UtcNow;
        else if (newStatus != TaskStatus.Done && oldStatus == TaskStatus.Done)
        {
            CompletedAt = null;
        }

        UpdatedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new TaskStatusChangedEvent(Id, oldStatus, newStatus));
        return Result<DomainError>.Success();
    }

    public Result<DomainError> Complete()
    {
        return SetStatus(TaskStatus.Done);
    }

    public Result<DomainError> Uncomplete()
    {
        return SetStatus(TaskStatus.Todo);
    }

    public Result<DomainError> UpdateTitle(TaskTitle newTitle)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        var oldTitle = Title.Value;
        Title = newTitle;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskUpdatedEvent(Id, "Title", oldTitle, newTitle.Value));
        return Result<DomainError>.Success();
    }

    public Result<DomainError> UpdateDescription(TaskDescription newDescription)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        var oldDescription = Description?.Value;
        Description = newDescription;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskUpdatedEvent(Id, "Description", oldDescription, newDescription.Value));
        return Result<DomainError>.Success();
    }

    public Result<DomainError> SetPriority(Priority newPriority)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        var oldPriority = Priority;
        Priority = newPriority;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskPriorityChangedEvent(Id, oldPriority, newPriority));
        return Result<DomainError>.Success();
    }

    public Result<DomainError> SetDueDate(DateTimeOffset? newDueDate)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        var oldDueDate = DueDate;
        DueDate = newDueDate;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskDueDateChangedEvent(Id, oldDueDate, newDueDate));
        return Result<DomainError>.Success();
    }

    /// <summary>
    /// Adds a tag to the task. Fails if a tag with the same value already exists (case-insensitive, since tags are normalized to lowercase).
    /// </summary>
    public Result<DomainError> AddTag(Tag tag)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        if (_tags.Any(t => t.Value == tag.Value))
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.duplicate_tag", $"Tag '{tag.Value}' already exists on this task."));

        _tags.Add(tag);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskTagAddedEvent(Id, tag));
        return Result<DomainError>.Success();
    }

    public Result<DomainError> RemoveTag(Tag tag)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        var existing = _tags.FirstOrDefault(t => t.Value == tag.Value);
        if (existing is null)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.tag_not_found", $"Tag '{tag.Value}' does not exist on this task."));

        _tags.Remove(existing);
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskTagRemovedEvent(Id, tag));
        return Result<DomainError>.Success();
    }

    /// <summary>
    /// Archives a task, hiding it from active views. Can be called regardless of status.
    /// </summary>
    public Result<DomainError> Archive()
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        IsArchived = true;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskArchivedEvent(Id));
        return Result<DomainError>.Success();
    }

    public Result<DomainError> Unarchive()
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        if (!IsArchived)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.not_archived", "Task is not archived."));

        IsArchived = false;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskUnarchivedEvent(Id));
        return Result<DomainError>.Success();
    }

    /// <summary>
    /// Soft-deletes the task. Once deleted, all other mutation methods will reject further changes.
    /// </summary>
    public Result<DomainError> Delete()
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.already_deleted", "Task is already deleted."));

        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskDeletedEvent(Id, UpdatedAt));
        return Result<DomainError>.Success();
    }
}
