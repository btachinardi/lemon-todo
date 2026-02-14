namespace LemonDo.Domain.Tasks.Entities;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Events;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed class BoardTask : Entity<BoardTaskId>
{
    public UserId OwnerId { get; }
    public TaskTitle Title { get; private set; }
    public TaskDescription? Description { get; private set; }
    public Priority Priority { get; private set; }
    public BoardTaskStatus Status { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public ColumnId ColumnId { get; private set; }
    public int Position { get; private set; }
    public bool IsArchived { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    private readonly List<Tag> _tags = [];
    public IReadOnlyList<Tag> Tags => _tags.AsReadOnly();

    private BoardTask(
        BoardTaskId id,
        UserId ownerId,
        ColumnId columnId,
        int position,
        BoardTaskStatus initialStatus,
        TaskTitle title,
        TaskDescription? description,
        Priority priority,
        DateTimeOffset? dueDate,
        IEnumerable<Tag>? tags) : base(id)
    {
        OwnerId = ownerId;
        ColumnId = columnId;
        Position = position;
        Status = initialStatus;
        Title = title;
        Description = description;
        Priority = priority;
        DueDate = dueDate;
        IsArchived = false;
        IsDeleted = false;

        if (tags is not null)
            _tags.AddRange(tags);

        RaiseDomainEvent(new TaskCreatedEvent(id, ownerId, title.Value, priority, columnId, position, initialStatus));
    }

    // EF Core constructor
    private BoardTask() : base(default!) { OwnerId = default!; Title = default!; ColumnId = default!; }

    public static Result<BoardTask, DomainError> Create(
        UserId ownerId,
        ColumnId columnId,
        int position,
        BoardTaskStatus initialStatus,
        TaskTitle title,
        TaskDescription? description = null,
        Priority priority = Priority.None,
        DateTimeOffset? dueDate = null,
        IEnumerable<Tag>? tags = null)
    {
        if (position < 0)
            return Result<BoardTask, DomainError>.Failure(
                DomainError.Validation("position", "Position must be >= 0."));

        var id = BoardTaskId.New();
        return Result<BoardTask, DomainError>.Success(
            new BoardTask(id, ownerId, columnId, position, initialStatus, title, description, priority, dueDate, tags));
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

    public Result<DomainError> MoveTo(ColumnId columnId, int position, BoardTaskStatus targetStatus)
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        if (position < 0)
            return Result<DomainError>.Failure(
                DomainError.Validation("position", "Position must be >= 0."));

        var fromColumnId = ColumnId;
        var oldStatus = Status;

        ColumnId = columnId;
        Position = position;
        Status = targetStatus;

        // Manage CompletedAt based on status transition
        if (targetStatus == BoardTaskStatus.Done && oldStatus != BoardTaskStatus.Done)
            CompletedAt = DateTimeOffset.UtcNow;
        else if (targetStatus != BoardTaskStatus.Done && oldStatus == BoardTaskStatus.Done)
        {
            CompletedAt = null;
            IsArchived = false;
        }

        UpdatedAt = DateTimeOffset.UtcNow;

        RaiseDomainEvent(new TaskMovedEvent(Id, fromColumnId, columnId, position));

        if (oldStatus != targetStatus)
            RaiseDomainEvent(new TaskStatusChangedEvent(Id, oldStatus, targetStatus));

        return Result<DomainError>.Success();
    }

    public Result<DomainError> Archive()
    {
        if (IsDeleted)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.deleted", "Cannot edit a deleted task."));

        if (Status != BoardTaskStatus.Done)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("task.not_completed", "Cannot archive a non-completed task."));

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
