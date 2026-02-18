# ProjectAgentBridge Context

> **Source**: Designed for v2 — see docs/domain/contexts/projects.md and docs/domain/contexts/agents.md
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## B1.1 Design Principles

1. **This bridge owns the worktree-to-session correlation** — Neither the Projects context nor the Agents context is an appropriate owner of the mapping between a session and the worktree it runs in. `AgentProjectCorrelation` is the canonical record of this linkage. Any downstream context that needs to know "which worktree did session X run in?" queries this bridge, not Agents or Projects directly.

2. **The Agents context knows only `WorkingDirectory` (a string path)** — The Agents context never holds a `WorktreeId`. It is given a resolved absolute path when a session starts, and that path is the full extent of its project knowledge. This bridge resolves `ProjectId + WorktreeId` into a `WorkingDirectory` path before handing off to Agents, and translates back when session events arrive.

3. **The WorkQueue aggregate lives here, not in Agents** — `WorkQueue` and `WorkItem` require knowledge of both `ProjectId` and `TaskId` to orchestrate their lifecycle. Because the Agents context is conformist to Projects (weak reference only), it cannot safely own an aggregate that actively creates worktrees and correlates with project state. The bridge owns `WorkQueue` and uses both upstream contexts to fulfil each work item.

4. **This bridge does not own session lifecycle** — It does not manage `AgentSession` status. It subscribes to events from the Agents context and reacts to lifecycle transitions. When a session starts, this bridge creates or resolves the worktree. When a session is approved, this bridge requests worktree merge via the Projects context.

5. **Worktree creation is always mediated by Projects** — This bridge never calls `IGitService` directly. It dispatches `CreateWorktreeCommand` to the Projects context (via application layer coordination) and receives a `WorktreeCreatedEvent` that it uses to resolve the `WorkingDirectory` for the session.

6. **This bridge publishes its own events** — `WorkQueueItemStartedEvent`, `WorkQueueItemCompletedEvent`, and `WorkQueueCompletedEvent` are owned here, not in Agents. Notification context subscribes to these for queue progress alerts.

---

## B1.2 Entities

### AgentProjectCorrelation (Aggregate Root)

```
AgentProjectCorrelation
├── Id: CorrelationId (value object)
├── OwnerId: UserId (from Identity context)
├── AgentSessionId: AgentSessionId (from Agents context — the session being correlated)
├── ProjectId: ProjectId (from Projects context — the project the session works in)
├── WorktreeId: WorktreeId? (from Projects context — null if no worktree was created)
├── TaskId: TaskId? (from Tasks context — the task objective; null for ad-hoc sessions)
├── WorkQueueId: WorkQueueId? (set when session is part of a queue managed here)
├── WorkItemId: WorkItemId? (the specific work item this session is fulfilling)
├── WorkingDirectory: string (resolved absolute path — the path given to the Agents context)
├── Status: CorrelationStatus (Pending, Resolving, Active, Merging, Merged, Failed, Cancelled)
├── FailureReason: string? (set when Status = Failed; human-readable summary)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, agentSessionId, projectId, taskId?, workQueueId?, workItemId?)
│   │       -> CorrelationCreatedEvent
│   │          (Status = Pending; WorktreeId = null; WorkingDirectory = empty string)
│   ├── BeginWorktreeResolution()
│   │       -> CorrelationResolvingEvent
│   │          (Status: Pending -> Resolving; called when CreateWorktreeCommand is dispatched)
│   ├── Activate(worktreeId, workingDirectory)
│   │       -> CorrelationActivatedEvent
│   │          (Status: Resolving -> Active; sets WorktreeId and WorkingDirectory;
│   │           called when WorktreeCreatedEvent arrives from Projects context;
│   │           the WorkingDirectory is now ready to pass to the Agents context)
│   ├── ActivateWithExistingWorktree(worktreeId, workingDirectory)
│   │       -> CorrelationActivatedEvent
│   │          (Status: Pending -> Active; used when the session targets an existing worktree,
│   │           not a new one; WorktreeId and WorkingDirectory set directly)
│   ├── BeginMerge()
│   │       -> CorrelationMergingEvent
│   │          (Status: Active -> Merging; called when AgentSessionApprovedEvent arrives
│   │           from Agents context and a worktree merge is required)
│   ├── CompleteMerge()
│   │       -> CorrelationMergedEvent
│   │          (Status: Merging -> Merged; called when WorktreeDeletedEvent arrives from
│   │           Projects context confirming the branch was merged and cleaned up)
│   ├── Fail(reason: string)
│   │       -> CorrelationFailedEvent
│   │          (transitions any non-terminal status -> Failed; records reason;
│   │           called when worktree creation fails or session fails before activation)
│   └── Cancel()
│           -> CorrelationCancelledEvent
│              (transitions Pending or Resolving -> Cancelled; called when session is
│               cancelled before the worktree is fully created)
│
└── Invariants:
    ├── AgentSessionId is immutable after creation
    ├── ProjectId is immutable after creation
    ├── WorktreeId is set exactly once, on Activate() or ActivateWithExistingWorktree()
    ├── WorkingDirectory must be a non-empty absolute path when Status = Active or beyond
    ├── WorkingDirectory is empty string only in Pending and Resolving states
    ├── Only one correlation may be Active per AgentSessionId at a time
    ├── Status transitions: Pending -> Resolving -> Active -> Merging -> Merged (happy path)
    │   Pending -> Active (existing worktree path, no resolution needed)
    │   Any non-terminal -> Failed
    │   Pending or Resolving -> Cancelled
    ├── Cannot transition out of Merged, Failed, or Cancelled (terminal states)
    └── If WorkQueueId is set then WorkItemId must also be set, and vice versa
```

### WorkQueue (Aggregate Root)

The `WorkQueue` orchestrates a batch of work items, each representing a task to be executed by an agent session in the context of a project. This aggregate moved here from the Agents context because it requires knowledge of both `ProjectId` (to create worktrees) and `TaskId` (to correlate with the Tasks context).

```
WorkQueue
├── Id: WorkQueueId (value object)
├── OwnerId: UserId
├── ProjectId: ProjectId (from Projects context — all items run in this project)
├── Name: QueueName (value object)
├── ExecutionMode: ExecutionMode (Parallel, Sequential)
├── Status: WorkQueueStatus (Pending, Running, Paused, Completed, Failed, Cancelled)
├── Items: IReadOnlyList<WorkItem> (ordered by Position, then Priority)
├── Budget: QueueBudget? (value object — total token/cost cap across all items; optional)
├── RequiresVerificationGate: bool (if true, each session must be approved before next item starts)
├── MaxParallelSessions: int (for Parallel mode; default: 3; min: 1; max: 10)
├── TemplateId: AgentTemplateId? (from Agents context — template for sessions in this queue)
├── AutoCreateWorktrees: bool (if true, bridge creates a worktree per item on dispatch)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, projectId, name, mode, items, budget?, requiresVerificationGate?,
│   │          maxParallel?, templateId?, autoCreateWorktrees?)
│   │       -> WorkQueueCreatedEvent
│   ├── Start() -> WorkQueueStartedEvent
│   │       (Status: Pending -> Running; emits to signal first item(s) may be dispatched)
│   ├── MarkItemDispatched(itemId: WorkItemId, sessionId: AgentSessionId)
│   │       -> WorkQueueItemDispatchedEvent
│   │          (Status: item transitions Queued/Ready -> Running; records AssignedSessionId;
│   │           called when a session is successfully started for this item)
│   ├── AdvanceQueue(completedItemId: WorkItemId) -> WorkQueueAdvancedEvent?
│   │       (called when a WorkItem's session is approved; marks item Completed;
│   │        for Sequential mode: transitions next Queued item to Ready;
│   │        for Parallel mode: marks completed slot freed; returns event if queue advances;
│   │        if all items are terminal: transitions queue to Completed via WorkQueueCompletedEvent)
│   ├── RecordItemFailure(itemId: WorkItemId, reason: string?) -> WorkQueueItemFailedEvent
│   │       (marks item as Failed; for Sequential mode, pauses the entire queue)
│   ├── Pause() -> WorkQueuePausedEvent
│   ├── Resume() -> WorkQueueResumedEvent
│   │       (Status: Paused -> Running; re-signals ready items for dispatch)
│   ├── Cancel() -> WorkQueueCancelledEvent
│   │       (marks all non-terminal items as Skipped; bridge cancels their sessions)
│   ├── ReorderItems(orderedItemIds: IReadOnlyList<WorkItemId>) -> WorkQueueReorderedEvent
│   │       (only valid when Status == Pending; resets Position values)
│   └── UpdateBudget(newBudget: QueueBudget) -> WorkQueueBudgetUpdatedEvent
│
└── Invariants:
    ├── Must have at least one WorkItem
    ├── In Sequential mode, only one item may be in Running status at a time
    ├── In Parallel mode, the number of Running items must not exceed MaxParallelSessions
    ├── MaxParallelSessions must be 1-10
    ├── ReorderItems() is only valid when Status == Pending
    ├── A Cancelled or Completed queue cannot be restarted or modified
    ├── When QueueBudget is set, the aggregate tracks total session costs; queue auto-pauses
    │   when the sum of all completed session costs reaches QueueBudget.HardCapUsd
    ├── Item positions must be unique and contiguous (1, 2, 3, ..., N)
    └── AutoCreateWorktrees = true requires ProjectId to be set (always true here by design)
```

### WorkItem (Entity, owned by WorkQueue)

```
WorkItem
├── Id: WorkItemId (value object)
├── QueueId: WorkQueueId (parent queue)
├── TaskId: TaskId (from Tasks context — the objective for this work item)
├── Position: int (order in the queue; 1-based)
├── Priority: WorkItemPriority (Low, Normal, High, Critical)
├── Status: WorkItemStatus (Queued, Ready, Running, VerificationPassed, Completed, Failed, Skipped)
├── AssignedSessionId: AgentSessionId? (set when a session is started for this item)
├── AssignedWorktreeId: WorktreeId? (set when a worktree is created for this item)
├── VerificationPassed: bool (set to true when the session's verification gate passes)
├── AccumulatedCostUsd: decimal (updated from session metrics; used for queue budget tracking)
├── CompletedAt: DateTimeOffset?
│
└── Invariants:
    ├── TaskId is immutable after creation
    ├── Position must be >= 1
    ├── AssignedSessionId is set once on transition to Running and is then immutable
    ├── AssignedWorktreeId is set once when the bridge creates the worktree for this item
    ├── VerificationPassed can only be set to true when Status == Running
    ├── AccumulatedCostUsd must be >= 0
    └── Completed and Skipped are terminal states — no further transitions allowed
```

---

## B1.3 Value Objects

```
CorrelationId           -> Guid wrapper
WorkQueueId             -> Guid wrapper
WorkItemId              -> Guid wrapper

CorrelationStatus       -> Enum: Pending, Resolving, Active, Merging, Merged, Failed, Cancelled
                           Pending: correlation record created; worktree not yet requested
                           Resolving: CreateWorktreeCommand dispatched; awaiting WorktreeCreatedEvent
                           Active: WorkingDirectory resolved; session may start
                           Merging: session approved; merge requested from Projects context
                           Merged: worktree merged and cleaned up
                           Failed: error during worktree creation or session execution
                           Cancelled: session cancelled before worktree was fully created

WorkQueueStatus         -> Enum: Pending, Running, Paused, Completed, Failed, Cancelled
WorkItemStatus          -> Enum: Queued, Ready, Running, VerificationPassed, Completed, Failed, Skipped
WorkItemPriority        -> Enum: Low, Normal, High, Critical
ExecutionMode           -> Enum: Parallel, Sequential

QueueName               -> Non-empty string, 1-200 chars, trimmed

QueueBudget             -> { HardCapUsd: decimal, WarnAtPercent: int (default 80),
                              TotalSpentUsd: decimal }
                           HardCapUsd must be > 0; WarnAtPercent must be 1-99
```

---

## B1.4 Domain Events

```
CorrelationCreatedEvent         { CorrelationId, OwnerId, AgentSessionId, ProjectId, TaskId?,
                                   WorkQueueId?, WorkItemId? }

CorrelationResolvingEvent       { CorrelationId, AgentSessionId, ProjectId }
                                (worktree creation has been dispatched; awaiting Projects context)

CorrelationActivatedEvent       { CorrelationId, AgentSessionId, ProjectId, WorktreeId,
                                   WorkingDirectory }
                                (WorkingDirectory is now resolved; Agents context may proceed)

CorrelationMergingEvent         { CorrelationId, AgentSessionId, ProjectId, WorktreeId }
                                (session approved; merge request dispatched to Projects context)

CorrelationMergedEvent          { CorrelationId, AgentSessionId, ProjectId, WorktreeId }
                                (Projects context confirmed merge and worktree cleanup)

CorrelationFailedEvent          { CorrelationId, AgentSessionId, ProjectId, WorktreeId?, Reason }
CorrelationCancelledEvent       { CorrelationId, AgentSessionId, ProjectId }

WorkQueueCreatedEvent           { QueueId, OwnerId, ProjectId, ExecutionMode, ItemCount }
WorkQueueStartedEvent           { QueueId, OwnerId, ProjectId }
WorkQueueItemDispatchedEvent    { QueueId, ItemId, SessionId, WorktreeId? }
                                (item has a session started for it; worktree created if AutoCreateWorktrees)
WorkQueueAdvancedEvent          { QueueId, CompletedItemId, NextItemId? }
WorkQueueItemFailedEvent        { QueueId, ItemId, SessionId, Reason? }
WorkQueuePausedEvent            { QueueId, OwnerId }
WorkQueueResumedEvent           { QueueId, OwnerId }
WorkQueueCancelledEvent         { QueueId, OwnerId }
WorkQueueReorderedEvent         { QueueId, OwnerId }
WorkQueueBudgetUpdatedEvent     { QueueId, OwnerId, NewHardCapUsd }
WorkQueueCompletedEvent         { QueueId, OwnerId, ProjectId, TotalItemCount,
                                   CompletedCount, FailedCount, SkippedCount, TotalCostUsd }
```

---

## B1.5 Use Cases

```
Commands:
├── StartQueuedAgentSessionCommand  { ProjectId, TaskId?, TemplateId?, Name?,
│                                     BudgetCapUsd: decimal, AutoCreateWorktree: bool,
│                                     UseExistingWorktreeId?: WorktreeId }
│       → The bridge entry point for starting a standalone (non-queue) agent session
│         with full project correlation.
│
│         Step 1 — Create AgentProjectCorrelation (Status = Pending).
│         Step 2a — If AutoCreateWorktree = true:
│                   BeginWorktreeResolution(); dispatch CreateWorktreeCommand to Projects context.
│                   Await WorktreeCreatedEvent; call Activate(worktreeId, worktreePath).
│         Step 2b — If UseExistingWorktreeId is provided:
│                   Load worktree from Projects context; call ActivateWithExistingWorktree(...).
│         Step 2c — If no worktree needed:
│                   Activate with projectPath as WorkingDirectory; WorktreeId = null.
│         Step 3 — Dispatch StartAgentSessionCommand to Agents context with WorkingDirectory
│                  and Objective resolved from TaskId (loaded from Tasks context if set).
│         Step 4 — Raises CorrelationActivatedEvent.
│
├── StartWorkQueueCommand           { ProjectId, TaskIds: IReadOnlyList<TaskId>,
│                                     ExecutionMode, BudgetPerSessionUsd: decimal,
│                                     TotalBudgetCapUsd?: decimal,
│                                     RequiresVerificationGate: bool,
│                                     MaxParallelSessions?: int,
│                                     TemplateId?: AgentTemplateId,
│                                     AutoCreateWorktrees: bool }
│       → Creates a WorkQueue with WorkItems for each TaskId.
│         Calls queue.Start().
│         For Parallel mode: dispatches StartQueuedAgentSessionCommand for the first
│           MaxParallelSessions items concurrently.
│         For Sequential mode: dispatches StartQueuedAgentSessionCommand for item[0] only.
│         All subsequent item dispatches are triggered by WorkQueueAdvancedEvent.
│
├── PauseWorkQueueCommand           { QueueId }
│       → Calls queue.Pause(); pauses all Running sessions in the queue via
│         PauseAgentSessionCommand (Agents context).
│
├── ResumeWorkQueueCommand          { QueueId }
│       → Calls queue.Resume(); re-dispatches ready items to new sessions.
│
├── CancelWorkQueueCommand          { QueueId }
│       → Calls queue.Cancel(); dispatches CancelAgentSessionCommand for all
│         non-terminal sessions in the queue (Agents context).
│         Cancels all non-merged correlations via Cancel().
│
├── ReorderWorkQueueCommand         { QueueId, OrderedItemIds: IReadOnlyList<WorkItemId> }
│       → Calls queue.ReorderItems(); only valid when queue is still Pending.
│
├── HandleSessionApprovedCommand    { AgentSessionId }
│       (Internal — dispatched by application layer on AgentSessionApprovedEvent from Agents context)
│       → Loads correlation by AgentSessionId.
│         If WorktreeId is set: calls BeginMerge(); dispatches DeleteWorktreeCommand to
│           Projects context (which merges + removes the worktree).
│         If WorkQueueId is set: calls queue.AdvanceQueue(workItemId).
│           If queue advances: dispatches StartQueuedAgentSessionCommand for next ready item.
│           If queue is now Completed: raises WorkQueueCompletedEvent.
│
├── HandleWorktreeCreatedCommand    { WorktreeId, ProjectId, LocalPath }
│       (Internal — dispatched by application layer on WorktreeCreatedEvent from Projects context)
│       → Loads correlation by (ProjectId, Status = Resolving).
│         Calls Activate(worktreeId, localPath); raises CorrelationActivatedEvent.
│         If correlation has a pending StartAgentSessionCommand awaiting activation:
│           dispatches it now with WorkingDirectory resolved.
│
├── HandleWorktreeDeletedCommand    { WorktreeId, ProjectId }
│       (Internal — dispatched by application layer on WorktreeDeletedEvent from Projects context)
│       → Loads correlation by WorktreeId where Status = Merging.
│         Calls CompleteMerge(); raises CorrelationMergedEvent.
│
├── HandleSessionFailedCommand      { AgentSessionId, FailureReason }
│       (Internal — dispatched by application layer on AgentSessionFailedEvent from Agents context)
│       → Loads correlation by AgentSessionId.
│         Calls Fail(reason); raises CorrelationFailedEvent.
│         If WorkQueueId is set: calls queue.RecordItemFailure(workItemId, reason).
│
└── HandleSessionCancelledCommand   { AgentSessionId }
        (Internal — dispatched by application layer on SessionCancelledEvent from Agents context)
        → Loads correlation by AgentSessionId.
          If Status is Pending or Resolving: calls Cancel().
          If Status is Active or beyond: no action (session was running; correlation stays Active).
          Raises CorrelationCancelledEvent if cancelled.

Queries:
├── GetCorrelationBySessionQuery    { AgentSessionId } -> CorrelationDto?
│       (returns the correlation for a session, including resolved WorkingDirectory and
│        WorktreeId; used by the Agents context ACL to display project context on session detail)
│
├── GetCorrelationByWorktreeQuery   { WorktreeId } -> CorrelationDto?
│       (returns the correlation record for a given worktree; used by the Projects context
│        UI to show which session is running in this worktree)
│
├── ListCorrelationsByProjectQuery  { ProjectId, Status? } -> IReadOnlyList<CorrelationSummaryDto>
│       (returns all correlations for a project; used for the project activity panel)
│
├── GetWorkQueueQuery               { QueueId } -> WorkQueueDto
│       (includes Items with their statuses, assigned session IDs, and accumulated costs)
│
└── ListWorkQueuesQuery             { ProjectId?, Status?, Page, PageSize }
                                         -> PagedResult<WorkQueueSummaryDto>
```

---

## B1.6 Repository Interfaces

```csharp
/// <summary>
/// Repository for AgentProjectCorrelation aggregate.
/// Correlations are the canonical record of the session-to-worktree mapping.
/// </summary>
public interface IAgentProjectCorrelationRepository
{
    /// <summary>Loads a correlation by its own ID. Returns null if not found.</summary>
    Task<AgentProjectCorrelation?> GetByIdAsync(CorrelationId id, CancellationToken ct);

    /// <summary>
    /// Loads the active correlation for the given agent session.
    /// Returns null if no correlation exists for this session.
    /// </summary>
    Task<AgentProjectCorrelation?> GetBySessionIdAsync(
        AgentSessionId sessionId, CancellationToken ct);

    /// <summary>
    /// Loads the correlation that owns the given worktree.
    /// Returns null if no correlation references this worktree.
    /// </summary>
    Task<AgentProjectCorrelation?> GetByWorktreeIdAsync(
        WorktreeId worktreeId, CancellationToken ct);

    /// <summary>
    /// Loads the first correlation in Resolving status for the given project.
    /// Used by HandleWorktreeCreatedCommand to match an inbound WorktreeCreatedEvent
    /// to the correlation that requested it.
    /// Returns null if no pending resolution exists for this project.
    /// </summary>
    Task<AgentProjectCorrelation?> GetPendingResolutionByProjectAsync(
        ProjectId projectId, CancellationToken ct);

    /// <summary>
    /// Returns all correlations for the given project, with optional status filter.
    /// Results are ordered by CreatedAt descending.
    /// </summary>
    Task<IReadOnlyList<AgentProjectCorrelation>> ListByProjectAsync(
        ProjectId projectId,
        CorrelationStatus? status,
        CancellationToken ct);

    /// <summary>Persists a newly created correlation.</summary>
    Task AddAsync(AgentProjectCorrelation correlation, CancellationToken ct);

    /// <summary>Persists status transitions and field updates (WorktreeId, WorkingDirectory).</summary>
    Task UpdateAsync(AgentProjectCorrelation correlation, CancellationToken ct);
}

/// <summary>
/// Repository for WorkQueue aggregate. Includes WorkItems as part of the aggregate.
/// </summary>
public interface IWorkQueueRepository
{
    /// <summary>Loads a queue with all its work items. Returns null if not found.</summary>
    Task<WorkQueue?> GetByIdAsync(WorkQueueId id, CancellationToken ct);

    /// <summary>
    /// Lists queues for the owning user with optional project and status filters.
    /// Results are ordered by CreatedAt descending.
    /// </summary>
    Task<PagedResult<WorkQueue>> ListAsync(
        UserId ownerId,
        ProjectId? projectId,
        WorkQueueStatus? status,
        int page, int pageSize,
        CancellationToken ct);

    /// <summary>Persists a new work queue and all its initial work items.</summary>
    Task AddAsync(WorkQueue queue, CancellationToken ct);

    /// <summary>
    /// Persists mutations to an existing work queue and its items
    /// (status changes, item reordering, item completion, accumulated cost updates).
    /// </summary>
    Task UpdateAsync(WorkQueue queue, CancellationToken ct);
}
```

---

## B1.7 API Endpoints

```
Work Queues (Human-facing, user JWT required):
GET    /api/bridge/queues                              List work queues (by project or status)    [Auth]
POST   /api/bridge/queues                              Create and start a work queue              [Auth]
GET    /api/bridge/queues/{id}                         Get queue detail with items                [Auth]
POST   /api/bridge/queues/{id}/pause                   Pause a running queue                      [Auth]
POST   /api/bridge/queues/{id}/resume                  Resume a paused queue                      [Auth]
POST   /api/bridge/queues/{id}/cancel                  Cancel a queue                             [Auth]
PUT    /api/bridge/queues/{id}/order                   Reorder queue items (Pending queues only)  [Auth]

Correlations (Human-facing, primarily for dashboard display):
GET    /api/bridge/correlations                        List correlations by project or session    [Auth]
GET    /api/bridge/correlations/{id}                   Get a specific correlation record          [Auth]
GET    /api/bridge/projects/{projectId}/correlations   All correlations for a project             [Auth]
GET    /api/bridge/sessions/{sessionId}/correlation    The correlation for a specific session     [Auth]
```

> Note: Internal event-handling commands (`HandleSessionApprovedCommand`, `HandleWorktreeCreatedCommand`, etc.) are not exposed as API endpoints. They are dispatched by application-layer event handlers in response to domain events published by the Agents and Projects contexts.

---

## B1.8 Cross-Context Event Subscriptions

This bridge subscribes to events from both the Projects context and the Agents context.

| Upstream Context | Event Subscribed | Handler Behaviour |
|-----------------|-----------------|-------------------|
| Agents | `AgentSessionApprovedEvent` | Dispatch `HandleSessionApprovedCommand`. Triggers worktree merge (if worktree exists) and queue advancement (if queue item). |
| Agents | `AgentSessionFailedEvent` | Dispatch `HandleSessionFailedCommand`. Marks correlation Failed; records item failure in queue. |
| Agents | `SessionCancelledEvent` | Dispatch `HandleSessionCancelledCommand`. Cancels correlation if still Pending or Resolving. |
| Projects | `WorktreeCreatedEvent` | Dispatch `HandleWorktreeCreatedCommand`. Activates correlation with resolved WorkingDirectory. |
| Projects | `WorktreeDeletedEvent` | Dispatch `HandleWorktreeDeletedCommand`. Completes merge on correlation. |

### Events Published by This Bridge

| Event | Subscribers |
|-------|-------------|
| `WorkQueueCompletedEvent` | Notification context — sends queue completion summary |
| `WorkQueueItemFailedEvent` | Notification context — sends item failure alert |
| `CorrelationFailedEvent` | Notification context — sends standalone session failure alert |

---

## B1.9 Application Layer Coordination

The "start agent in project worktree" workflow is a multi-step, multi-context operation. The steps are orchestrated here, in the bridge's application layer.

### Workflow: Start Agent in New Worktree

| Step | Bridge | Projects Context | Agents Context |
|------|--------|-----------------|----------------|
| 1 | Create `AgentProjectCorrelation` (Pending) | — | — |
| 2 | Call `BeginWorktreeResolution()` (Resolving) | — | — |
| 3 | — | Dispatch `CreateWorktreeCommand` | — |
| 4 | — | `WorktreeCreatedEvent` published | — |
| 5 | Handle `WorktreeCreatedCommand`; call `Activate(worktreeId, path)` | — | — |
| 6 | — | — | Dispatch `StartAgentSessionCommand` with `WorkingDirectory` |
| 7 | `CorrelationActivatedEvent` raised | — | `AgentSessionStartedEvent` raised |

### Workflow: Agent Completed → Merge Worktree

| Step | Bridge | Projects Context | Agents Context |
|------|--------|-----------------|----------------|
| 1 | — | — | `AgentSessionApprovedEvent` published |
| 2 | Handle `AgentSessionApprovedEvent`; load correlation | — | — |
| 3 | Call `BeginMerge()` (Merging) | — | — |
| 4 | — | Dispatch `DeleteWorktreeCommand` (merge + cleanup) | — |
| 5 | — | `WorktreeDeletedEvent` published | — |
| 6 | Handle `WorktreeDeletedEvent`; call `CompleteMerge()` (Merged) | — | — |
| 7 | `CorrelationMergedEvent` raised | — | — |

### Workflow: Sequential Queue Advancement

| Step | Bridge |
|------|--------|
| 1 | Receive `AgentSessionApprovedEvent` for item N's session |
| 2 | Dispatch `HandleSessionApprovedCommand` |
| 3 | Call `queue.AdvanceQueue(itemN.Id)` → `WorkQueueAdvancedEvent` with `NextItemId = itemN+1.Id` |
| 4 | Dispatch `StartQueuedAgentSessionCommand` for item N+1 (includes worktree creation if AutoCreateWorktrees) |
| 5 | If all items terminal: `WorkQueueCompletedEvent` raised |

---

## Design Notes

| Item | Type | Detail |
|------|------|--------|
| Correlation matching on WorktreeCreatedEvent | Design constraint | `GetPendingResolutionByProjectAsync` assumes only one concurrent Resolving correlation per project. If two sessions start simultaneously in the same project, both wanting new worktrees, there is a race condition. A more robust approach is to include a `CorrelationId` or `RequestToken` in the `CreateWorktreeCommand` and have the Projects context echo it back in `WorktreeCreatedEvent`. This requires a minor evolution of the Projects context's event schema. Deferred until concurrent queue execution is tested in practice. |
| WorkQueue moved from Agents context | Migration note | The `WorkQueue` and `WorkItem` aggregates previously appeared in the Agents context design (agents.md). They have been moved here because they require `ProjectId` awareness for worktree orchestration. The Agents context `IWorkQueueRepository` interface and related use cases in agents.md should be considered superseded by this bridge design. |
| `DeleteWorktreeCommand` as merge proxy | Design gap | The bridge dispatches `DeleteWorktreeCommand` when merging, treating worktree deletion as the merge proxy. This assumes the Projects context's delete flow includes a git merge step. If merge and delete are separate operations in the Projects context, a dedicated `MergeWorktreeCommand` should be added there and used here instead. |
