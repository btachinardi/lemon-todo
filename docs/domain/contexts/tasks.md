# Task Context

> **Source**: Extracted from docs/DOMAIN.md §3
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## 3.1 Design Principles

1. **Task owns its lifecycle and status** — A Task manages its own status through `SetStatus()`, `Complete()`, and `Uncomplete()`. Status transitions are self-contained within the Task aggregate.
2. **Task knows nothing about boards** — The Task entity has no concept of columns, positions, or spatial placement. It is a pure domain entity focused on task metadata and lifecycle.
3. **Archive is a visibility flag, NOT a lifecycle status** — `IsArchived` is a boolean orthogonal to status. Any task can be archived regardless of its current status (Todo, InProgress, Done).
4. **Status and CompletedAt are always consistent** — `SetStatus()` is the single method that manages CompletedAt transitions. `Complete()` and `Uncomplete()` are convenience wrappers around `SetStatus()`.

## 3.2 Entities

### Task (Aggregate Root)

```
Task
├── Id: TaskId (value object)
├── OwnerId: UserId (value object, from Identity context)
├── Title: TaskTitle (value object)
├── Description: TaskDescription? (value object)
├── Priority: Priority (value object / enum)
├── Status: TaskStatus (Todo, InProgress, Done)
├── DueDate: DateTimeOffset?
├── Tags: IReadOnlyList<Tag>
├── IsArchived: bool (visibility flag, orthogonal to status)
├── RedactedSensitiveNote: string? (redacted placeholder "[PROTECTED]" or null)
├── IsDeleted: bool (soft delete flag)
├── CompletedAt: DateTimeOffset? (set when status becomes Done)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, title, description?, priority?, dueDate?, tags?, sensitiveNote?)
│   │       -> TaskCreatedEvent (defaults to TaskStatus.Todo)
│   ├── SetStatus(status) -> TaskStatusChangedEvent
│   │       (manages CompletedAt: set when transitioning to Done, cleared when leaving Done)
│   ├── Complete() -> TaskStatusChangedEvent
│   │       (convenience: calls SetStatus(Done))
│   ├── Uncomplete() -> TaskStatusChangedEvent
│   │       (convenience: calls SetStatus(Todo))
│   ├── UpdateTitle(title) -> TaskUpdatedEvent
│   ├── UpdateDescription(description) -> TaskUpdatedEvent
│   ├── SetPriority(priority) -> TaskPriorityChangedEvent
│   ├── SetDueDate(dueDate) -> TaskDueDateChangedEvent
│   ├── AddTag(tag) -> TaskTagAddedEvent
│   ├── RemoveTag(tag) -> TaskTagRemovedEvent
│   ├── UpdateSensitiveNote(note?) -> (note encrypted at rest; only redacted value stored on entity)
│   ├── Archive() -> TaskArchivedEvent (any status)
│   ├── Unarchive() -> TaskUnarchivedEvent
│   └── Delete() -> TaskDeletedEvent (soft delete)
│
└── Invariants:
    ├── Title must be 1-500 characters
    ├── Description must be 0-10000 characters
    ├── Cannot archive a deleted task
    ├── Cannot edit a deleted task
    ├── SensitiveNote max 10,000 chars; encrypted at rest via AES-256-GCM
    ├── Tags are unique per task (no duplicates)
    ├── OwnerId cannot change after creation
    ├── CompletedAt is set when status transitions to Done
    └── CompletedAt is cleared when status transitions away from Done
```

## 3.3 Value Objects

```
TaskId          -> Guid wrapper
TaskTitle       -> Non-empty string, 1-500 chars, trimmed
TaskDescription -> String, 0-10000 chars
SensitiveNote   -> Non-empty string, 1-10000 chars, trimmed, implements IProtectedData
                   Encrypted at rest via AES-256-GCM shadow property; only redacted value on entity
Tag             -> Non-empty string, 1-50 chars, lowercase, trimmed
Priority        -> Enum: None, Low, Medium, High, Critical
TaskStatus      -> Enum: Todo, InProgress, Done (NO Archived — archive is a visibility flag)
```

## 3.4 Domain Events

```
TaskCreatedEvent            { TaskId, OwnerId, Title, Priority }
TaskUpdatedEvent            { TaskId, FieldName, OldValue, NewValue }
TaskStatusChangedEvent      { TaskId, OldStatus, NewStatus }
TaskPriorityChangedEvent    { TaskId, OldPriority, NewPriority }
TaskDueDateChangedEvent     { TaskId, OldDueDate?, NewDueDate? }
TaskTagAddedEvent           { TaskId, Tag }
TaskTagRemovedEvent         { TaskId, Tag }
TaskArchivedEvent           { TaskId }
TaskUnarchivedEvent         { TaskId }
TaskDeletedEvent            { TaskId, DeletedAt }
```

## 3.5 Use Cases

```
Commands:
├── CreateTaskCommand            { Title, Description?, Priority?, DueDate?, Tags?, SensitiveNote? }
│       → Creates Task (defaults to Todo), then coordinates with Board context
│         to place card on default board's initial column
├── UpdateTaskCommand            { TaskId, Title?, Description?, Priority?, DueDate?, SensitiveNote?, ClearSensitiveNote? }
├── AddTagToTaskCommand          { TaskId, Tag }
├── RemoveTagFromTaskCommand     { TaskId, Tag }
├── CompleteTaskCommand          { TaskId }
│       → Calls task.Complete(), then coordinates with Board context
│         to move card to Done column
├── UncompleteTaskCommand        { TaskId }
│       → Calls task.Uncomplete(), then coordinates with Board context
│         to move card to Todo column
├── ArchiveTaskCommand           { TaskId }
├── DeleteTaskCommand            { TaskId }
├── BulkCompleteTasksCommand     { TaskIds }
│       → Same as CompleteTaskCommand in a loop
├── ViewTaskNoteCommand          { TaskId, Password }
│       → Owner re-authenticates to decrypt and view their own sensitive note
│         Audited as SensitiveNoteRevealed
└── RevealTaskNoteCommand        { TaskId, Reason, ReasonDetails?, Comments?, AdminPassword }
        → Admin break-the-glass to decrypt any user's sensitive note
          Requires justification + password re-auth; audited with full details

Queries:
├── GetTaskByIdQuery             { TaskId } -> TaskDto (no columnId/position)
└── ListTasksQuery               { Status?, Priority?, Search?, Page, PageSize }
                                     -> PagedResult<TaskDto> (no columnId filter)
```

## 3.6 Repository Interface

```csharp
public interface ITaskRepository
{
    Task<Task?> GetByIdAsync(TaskId id, CancellationToken ct);
    Task<PagedResult<Task>> ListAsync(
        UserId ownerId, Priority? priority, TaskStatus? status,
        string? searchTerm, int page, int pageSize, CancellationToken ct);
    Task AddAsync(Task task, SensitiveNote? sensitiveNote = null, CancellationToken ct = default);
    Task UpdateAsync(Task task, SensitiveNote? sensitiveNote = null,
        bool clearSensitiveNote = false, CancellationToken ct = default);
    Task<Result<string, DomainError>> GetDecryptedSensitiveNoteAsync(TaskId taskId, CancellationToken ct);
}
```
