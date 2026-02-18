# ProjectTaskBridge Context

> **Source**: Designed for v2 — see docs/domain/contexts/projects.md and docs/domain/contexts/tasks.md
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## B3.1 Design Principles

1. **Weak references in the Projects context are not enough for rich queries** — The `Project` aggregate stores `LinkedTaskIds` as an opaque list of Guid references (see projects.md §8.3). These are sufficient for "link/unlink" operations but insufficient for queries like "give me the full task details for all tasks in this project" or "complete all tasks when this worktree merges". This bridge provides that richer cross-context query and automation surface.

2. **This bridge does not duplicate task or project data** — It stores only the link itself and its status. Task titles, priorities, and statuses are never cached here. Queries that need task details load them from the Tasks context via a read-only ACL port (`ITaskReadService`).

3. **This bridge is the authoritative record for task completion on worktree merge** — When a worktree merge event arrives from the Projects context, this bridge decides whether any linked tasks should be auto-completed, based on the `AutoCompleteOnMerge` flag on each link record. It dispatches `CompleteTaskCommand` to the Tasks context.

4. **The Projects context's own `LinkedTaskIds` list is the source of truth for membership** — Adding or removing a link in this bridge is always initiated from the Projects context (via `LinkTask` / `UnlinkTask`). This bridge subscribes to `TaskLinkedToProjectEvent` and `TaskUnlinkedFromProjectEvent` to keep its `ProjectTaskLink` records in sync. It does not create links independently.

5. **This bridge is kept deliberately thin** — Its only logic is: maintain link records, optionally auto-complete tasks on merge, and answer cross-context queries. It must not grow business rules about task or project ownership.

---

## B3.2 Entities

### ProjectTaskLink (Aggregate Root)

```
ProjectTaskLink
├── Id: ProjectTaskLinkId (value object)
├── OwnerId: UserId (from Identity context)
├── ProjectId: ProjectId (from Projects context)
├── TaskId: TaskId (from Tasks context)
├── AutoCompleteOnMerge: bool (if true, task is completed when the project's worktree is merged;
│                              default: false)
├── LinkedAt: DateTimeOffset (when the link was established)
├── UnlinkedAt: DateTimeOffset? (set when the link is removed; null while active)
├── IsActive: bool (true until UnlinkTask is called)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, projectId, taskId, autoCompleteOnMerge?)
│   │       -> ProjectTaskLinkCreatedEvent
│   │          (IsActive = true; sets LinkedAt)
│   ├── UpdateAutoComplete(autoCompleteOnMerge: bool)
│   │       -> ProjectTaskLinkUpdatedEvent
│   │          (updates the auto-complete setting; only valid while IsActive = true)
│   ├── Deactivate()
│   │       -> ProjectTaskLinkDeactivatedEvent
│   │          (IsActive = false; sets UnlinkedAt; once deactivated cannot be reactivated —
│   │           a new link record is created if the task is re-linked to the project)
│   └── RecordTaskCompletion(completedAt: DateTimeOffset)
│           -> ProjectTaskLinkTaskAutoCompletedEvent
│              (marks that this link triggered a task completion;
│               only valid when AutoCompleteOnMerge = true and IsActive = true)
│
└── Invariants:
    ├── ProjectId and TaskId are immutable after creation
    ├── Only one active link may exist per (ProjectId, TaskId) combination
    ├── UpdateAutoComplete() and RecordTaskCompletion() require IsActive = true
    ├── Deactivate() is idempotent — no error if already inactive
    └── UnlinkedAt is set exactly once, on the first call to Deactivate()
```

---

## B3.3 Value Objects

```
ProjectTaskLinkId       -> Guid wrapper

(No additional value objects — this bridge is intentionally thin. It references
 ProjectId from Projects context and TaskId from Tasks context, both of which are
 defined in their respective contexts.)
```

---

## B3.4 Domain Events

```
ProjectTaskLinkCreatedEvent             { LinkId, OwnerId, ProjectId, TaskId, AutoCompleteOnMerge }
ProjectTaskLinkUpdatedEvent             { LinkId, ProjectId, TaskId, AutoCompleteOnMerge }
                                        (auto-complete setting changed)
ProjectTaskLinkDeactivatedEvent         { LinkId, ProjectId, TaskId, UnlinkedAt }
ProjectTaskLinkTaskAutoCompletedEvent   { LinkId, ProjectId, TaskId, CompletedAt }
                                        (task was auto-completed because worktree was merged and
                                         AutoCompleteOnMerge = true for this link)
```

---

## B3.5 Use Cases

```
Commands:
├── HandleTaskLinkedCommand         { ProjectId, TaskId, OwnerId }
│       (Internal — dispatched by application layer on TaskLinkedToProjectEvent from Projects context)
│       → Creates a ProjectTaskLink (AutoCompleteOnMerge = false by default).
│         Raises ProjectTaskLinkCreatedEvent.
│         If an inactive link for this (ProjectId, TaskId) pair already exists: create a fresh one.
│
├── HandleTaskUnlinkedCommand       { ProjectId, TaskId }
│       (Internal — dispatched by application layer on TaskUnlinkedFromProjectEvent from Projects context)
│       → Loads the active ProjectTaskLink for (ProjectId, TaskId).
│         Calls Deactivate(); raises ProjectTaskLinkDeactivatedEvent.
│         No-op if no active link exists.
│
├── UpdateLinkAutoCompleteCommand   { ProjectId, TaskId, AutoCompleteOnMerge: bool }
│       → Loads the active link for (ProjectId, TaskId).
│         Calls UpdateAutoComplete(autoCompleteOnMerge); raises ProjectTaskLinkUpdatedEvent.
│
└── HandleWorktreeMergedCommand     { ProjectId, WorktreeId }
        (Internal — dispatched by application layer on WorktreeDeletedEvent from Projects context,
         which represents a merge + cleanup in the worktree lifecycle)
        → Loads all active ProjectTaskLinks for the project where AutoCompleteOnMerge = true.
          For each qualifying link:
            Dispatch CompleteTaskCommand to Tasks context.
            Call RecordTaskCompletion(now); raises ProjectTaskLinkTaskAutoCompletedEvent.

Queries:
├── ListTasksForProjectQuery        { ProjectId, Status? } -> IReadOnlyList<TaskSummaryDto>
│       (loads active link records for the project, then fetches task details from
│        Tasks context via ITaskReadService ACL port; applies optional status filter;
│        returns a combined view of task ID, title, priority, status, due date)
│
├── ListProjectsForTaskQuery        { TaskId } -> IReadOnlyList<ProjectSummaryDto>
│       (loads active link records for the task, then fetches project details from
│        Projects context via IProjectReadService ACL port;
│        returns a combined view of project ID, name, and status)
│
└── GetLinkQuery                    { ProjectId, TaskId } -> ProjectTaskLinkDto?
        (returns the active link record for the pair, including AutoCompleteOnMerge setting;
         returns null if no active link exists)
```

---

## B3.6 Repository Interface

```csharp
/// <summary>
/// Repository for ProjectTaskLink aggregate.
/// Each record represents one active or historical link between a project and a task.
/// </summary>
public interface IProjectTaskLinkRepository
{
    /// <summary>Loads a link by its own ID. Returns null if not found.</summary>
    Task<ProjectTaskLink?> GetByIdAsync(ProjectTaskLinkId id, CancellationToken ct);

    /// <summary>
    /// Loads the active link for the given (ProjectId, TaskId) pair.
    /// Returns null if no active link exists.
    /// </summary>
    Task<ProjectTaskLink?> GetActiveAsync(
        ProjectId projectId, TaskId taskId, CancellationToken ct);

    /// <summary>
    /// Returns all active links for the given project.
    /// Used by ListTasksForProjectQuery to enumerate task IDs for hydration.
    /// </summary>
    Task<IReadOnlyList<ProjectTaskLink>> ListActiveByProjectAsync(
        ProjectId projectId, CancellationToken ct);

    /// <summary>
    /// Returns all active links for the given task.
    /// Used by ListProjectsForTaskQuery to enumerate project IDs for hydration.
    /// </summary>
    Task<IReadOnlyList<ProjectTaskLink>> ListActiveByTaskAsync(
        TaskId taskId, CancellationToken ct);

    /// <summary>
    /// Returns all active links for a project where AutoCompleteOnMerge is true.
    /// Used by HandleWorktreeMergedCommand to find tasks to auto-complete.
    /// </summary>
    Task<IReadOnlyList<ProjectTaskLink>> ListAutoCompleteLinksForProjectAsync(
        ProjectId projectId, CancellationToken ct);

    /// <summary>Persists a newly created link.</summary>
    Task AddAsync(ProjectTaskLink link, CancellationToken ct);

    /// <summary>Persists mutations (auto-complete setting change, deactivation, completion recording).</summary>
    Task UpdateAsync(ProjectTaskLink link, CancellationToken ct);
}
```

---

## B3.7 ACL Ports

This bridge reads from both upstream contexts via read-only ACL ports. It never imports domain objects from Tasks or Projects directly.

```csharp
/// <summary>
/// Read-only ACL port to the Tasks context.
/// Used to hydrate task details for cross-context queries.
/// Never used for mutations — mutations go through the Tasks context's own command handlers.
/// </summary>
public interface ITaskReadService
{
    /// <summary>
    /// Returns summary DTOs for the given task IDs.
    /// Returns only the tasks that exist and are not soft-deleted.
    /// Order of results is not guaranteed to match the order of input IDs.
    /// </summary>
    Task<IReadOnlyList<TaskSummaryDto>> GetTaskSummariesAsync(
        IReadOnlyList<TaskId> taskIds, CancellationToken ct);
}

/// <summary>
/// Read-only ACL port to the Projects context.
/// Used to hydrate project details for cross-context queries.
/// </summary>
public interface IProjectReadService
{
    /// <summary>
    /// Returns summary DTOs for the given project IDs.
    /// Returns only the projects that exist and are not archived.
    /// Order of results is not guaranteed to match the order of input IDs.
    /// </summary>
    Task<IReadOnlyList<ProjectSummaryDto>> GetProjectSummariesAsync(
        IReadOnlyList<ProjectId> projectIds, CancellationToken ct);
}
```

---

## B3.8 API Endpoints

```
GET    /api/bridge/projects/{projectId}/tasks               List tasks linked to a project         [Auth]
GET    /api/bridge/tasks/{taskId}/projects                  List projects a task is linked to      [Auth]
GET    /api/bridge/projects/{projectId}/tasks/{taskId}/link Get link details (auto-complete flag)  [Auth]
PATCH  /api/bridge/projects/{projectId}/tasks/{taskId}/link Update auto-complete setting           [Auth]
```

> Note: Creating and removing links is always done through the Projects context endpoints
> (`POST /api/projects/{id}/tasks` and `DELETE /api/projects/{id}/tasks/{taskId}`).
> This bridge reacts to those events and exposes only the bridge-specific query and settings surface.

---

## B3.9 Cross-Context Event Subscriptions

| Upstream Context | Event Subscribed | Handler Behaviour |
|-----------------|-----------------|-------------------|
| Projects | `TaskLinkedToProjectEvent` | Dispatch `HandleTaskLinkedCommand`. Create active `ProjectTaskLink` record. |
| Projects | `TaskUnlinkedFromProjectEvent` | Dispatch `HandleTaskUnlinkedCommand`. Deactivate the link record. |
| Projects | `WorktreeDeletedEvent` | Dispatch `HandleWorktreeMergedCommand`. Auto-complete qualifying tasks. |

### Events Published by This Bridge

This bridge is primarily a consumer and query provider. Its own events (`ProjectTaskLinkCreatedEvent`, `ProjectTaskLinkTaskAutoCompletedEvent`) are raised for audit purposes. No other context subscribes to them.
