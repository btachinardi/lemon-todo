# AgentTaskBridge Context

> **Source**: Designed for v2 — see docs/domain/contexts/agents.md and docs/domain/contexts/tasks.md
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## B2.1 Design Principles

1. **This bridge owns the session-to-task correlation** — The Agents context stores `TaskId` as an opaque Guid reference. The Tasks context knows nothing about sessions. This bridge owns the correlation record that links them and the lifecycle rules governing when and how a task is affected by agent work.

2. **Task completion is always mediated by this bridge** — When a session is approved, this bridge determines whether the linked task should be completed, and dispatches `CompleteTaskCommand` to the Tasks context if appropriate. Neither the Agents context nor the Tasks context calls the other directly.

3. **Agent-created tasks are tracked as follow-up links** — When an agent calls the Agent API to create a task, this bridge records the relationship: the new task is a follow-up to the session's primary task (if any). This correlation is queryable ("all tasks created by agent session X") without requiring the Tasks context to know about sessions.

4. **This bridge is deliberately thin** — It does not own session lifecycle, task lifecycle, or any rich business logic. It subscribes to events from Agents and reacts minimally. If logic accumulates here, that is a signal to reconsider the upstream context ownership.

5. **This bridge does not coordinate with Projects** — Worktree merging and project-level orchestration live in `ProjectAgentBridge`. This bridge is concerned only with task state transitions driven by agent session outcomes.

---

## B2.2 Entities

### AgentTaskCorrelation (Aggregate Root)

Tracks the relationship between an agent session and the task(s) it works on or produces.

```
AgentTaskCorrelation
├── Id: AgentTaskCorrelationId (value object)
├── OwnerId: UserId (from Identity context)
├── AgentSessionId: AgentSessionId (from Agents context)
├── PrimaryTaskId: TaskId? (from Tasks context — the task this session was assigned to;
│                           null for sessions with no task objective)
├── FollowUpTaskIds: IReadOnlyList<TaskId> (tasks created by the agent during this session
│                    via the Agent API; may be empty)
├── Status: AgentTaskCorrelationStatus (Active, TaskCompleted, TaskSkipped, Failed)
├── TaskCompletedAt: DateTimeOffset? (set when Status transitions to TaskCompleted)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, agentSessionId, primaryTaskId?)
│   │       -> AgentTaskCorrelationCreatedEvent
│   │          (Status = Active)
│   ├── RecordFollowUpTask(taskId: TaskId)
│   │       -> AgentFollowUpTaskRecordedEvent
│   │          (appends to FollowUpTaskIds; idempotent — no-op if taskId already present)
│   ├── MarkTaskCompleted()
│   │       -> AgentTaskCorrelationTaskCompletedEvent
│   │          (Status: Active -> TaskCompleted; sets TaskCompletedAt;
│   │           only valid when PrimaryTaskId is set and Status == Active)
│   ├── MarkTaskSkipped(reason: string)
│   │       -> AgentTaskCorrelationTaskSkippedEvent
│   │          (Status: Active -> TaskSkipped; used when session is approved but
│   │           PrimaryTaskId is null — no task to complete)
│   └── MarkFailed(reason: string)
│           -> AgentTaskCorrelationFailedEvent
│              (Status: Active -> Failed; used when session is rejected or fails;
│               task is NOT completed)
│
└── Invariants:
    ├── AgentSessionId is immutable after creation
    ├── PrimaryTaskId is immutable after creation (set once at session start; never changed)
    ├── FollowUpTaskIds contains no duplicates
    ├── MarkTaskCompleted() requires PrimaryTaskId to be set; raises domain error if null
    ├── MarkTaskCompleted() is only valid from Status == Active
    ├── MarkTaskSkipped() is only valid from Status == Active
    ├── MarkFailed() is only valid from Status == Active
    ├── TaskCompleted, TaskSkipped, and Failed are terminal states — no further transitions
    ├── Only one correlation may exist per AgentSessionId
    └── TaskCompletedAt is set exactly once, on the first transition to TaskCompleted
```

---

## B2.3 Value Objects

```
AgentTaskCorrelationId      -> Guid wrapper

AgentTaskCorrelationStatus  -> Enum: Active, TaskCompleted, TaskSkipped, Failed
                               Active: session is running; task outcome not yet determined
                               TaskCompleted: session approved; primary task was completed
                               TaskSkipped: session approved; no primary task to complete
                               Failed: session rejected or failed; task was NOT completed
```

---

## B2.4 Domain Events

```
AgentTaskCorrelationCreatedEvent        { CorrelationId, OwnerId, AgentSessionId, PrimaryTaskId? }
                                        (created when a session with a task objective starts)

AgentFollowUpTaskRecordedEvent          { CorrelationId, AgentSessionId, FollowUpTaskId,
                                           LinkedFromTaskId? }
                                        (new task created by agent via Agent API, now linked here)

AgentTaskCorrelationTaskCompletedEvent  { CorrelationId, AgentSessionId, TaskId, CompletedAt }
                                        (primary task was marked complete; dispatched downstream
                                         to Task context via CompleteTaskCommand)

AgentTaskCorrelationTaskSkippedEvent    { CorrelationId, AgentSessionId, Reason }
                                        (session approved but no primary task existed)

AgentTaskCorrelationFailedEvent         { CorrelationId, AgentSessionId, PrimaryTaskId?, Reason }
                                        (session rejected or failed; task was NOT completed)
```

---

## B2.5 Use Cases

```
Commands:
├── CreateAgentTaskCorrelationCommand   { AgentSessionId, OwnerId, PrimaryTaskId? }
│       (Internal — dispatched by application layer on AgentSessionStartedEvent from Agents context)
│       → Creates an AgentTaskCorrelation (Status = Active).
│         Raises AgentTaskCorrelationCreatedEvent.
│         No-op if correlation already exists for this session (idempotent).
│
├── HandleSessionApprovedForTaskCommand { AgentSessionId }
│       (Internal — dispatched by application layer on AgentSessionApprovedEvent from Agents context)
│       → Loads correlation by AgentSessionId.
│         If PrimaryTaskId is set:
│           Dispatch CompleteTaskCommand to Tasks context.
│           Call MarkTaskCompleted(); raises AgentTaskCorrelationTaskCompletedEvent.
│         If PrimaryTaskId is null:
│           Call MarkTaskSkipped("no primary task"); raises AgentTaskCorrelationTaskSkippedEvent.
│
├── HandleSessionRejectedForTaskCommand { AgentSessionId }
│       (Internal — dispatched by application layer on AgentSessionRejectedEvent from Agents context)
│       → Loads correlation by AgentSessionId.
│         Calls MarkFailed("session output rejected"); raises AgentTaskCorrelationFailedEvent.
│         No task completion is dispatched.
│
├── HandleSessionFailedForTaskCommand   { AgentSessionId, FailureReason }
│       (Internal — dispatched by application layer on AgentSessionFailedEvent from Agents context)
│       → Loads correlation by AgentSessionId.
│         Calls MarkFailed(failureReason); raises AgentTaskCorrelationFailedEvent.
│         No task completion is dispatched.
│
└── RecordAgentCreatedTaskCommand       { AgentSessionId, CreatedTaskId }
        (Internal — dispatched by application layer on AgentApiTaskCreatedEvent from Agents context)
        → Loads correlation by AgentSessionId.
          If no correlation exists: creates one (Status = Active, PrimaryTaskId = null).
          Calls RecordFollowUpTask(createdTaskId); raises AgentFollowUpTaskRecordedEvent.

Queries:
├── GetAgentTaskCorrelationQuery        { AgentSessionId } -> AgentTaskCorrelationDto?
│       (returns the correlation for a session, including PrimaryTaskId and FollowUpTaskIds;
│        used by the session detail page to show linked task context)
│
├── GetTaskAgentHistoryQuery            { TaskId } -> IReadOnlyList<AgentTaskCorrelationSummaryDto>
│       (returns all correlations where PrimaryTaskId == TaskId;
│        shows the history of agent work attempted on a specific task)
│
└── ListFollowUpTasksBySessionQuery     { AgentSessionId }
                                             -> IReadOnlyList<TaskId>
        (returns all TaskIds recorded as follow-ups from the given session;
         used by the session detail page to link to agent-created tasks)
```

---

## B2.6 Repository Interface

```csharp
/// <summary>
/// Repository for AgentTaskCorrelation aggregate.
/// Maintains the linkage between agent sessions and the tasks they work on or create.
/// </summary>
public interface IAgentTaskCorrelationRepository
{
    /// <summary>Loads a correlation by its own ID. Returns null if not found.</summary>
    Task<AgentTaskCorrelation?> GetByIdAsync(
        AgentTaskCorrelationId id, CancellationToken ct);

    /// <summary>
    /// Loads the correlation for the given agent session.
    /// Returns null if no correlation exists for this session.
    /// Only one correlation per session is enforced by the aggregate invariant.
    /// </summary>
    Task<AgentTaskCorrelation?> GetBySessionIdAsync(
        AgentSessionId sessionId, CancellationToken ct);

    /// <summary>
    /// Returns all correlations whose PrimaryTaskId matches the given task.
    /// Ordered by CreatedAt descending.
    /// Used for the task's agent work history panel.
    /// </summary>
    Task<IReadOnlyList<AgentTaskCorrelation>> ListByPrimaryTaskIdAsync(
        TaskId taskId, CancellationToken ct);

    /// <summary>Persists a newly created correlation.</summary>
    Task AddAsync(AgentTaskCorrelation correlation, CancellationToken ct);

    /// <summary>
    /// Persists status transitions and follow-up task list updates.
    /// </summary>
    Task UpdateAsync(AgentTaskCorrelation correlation, CancellationToken ct);
}
```

---

## B2.7 API Endpoints

This context has no user-facing mutation endpoints. All state changes are driven by internal event handlers. Read-only query endpoints are provided for display purposes.

```
GET    /api/bridge/sessions/{sessionId}/task-correlation    Get task correlation for a session   [Auth]
GET    /api/bridge/tasks/{taskId}/agent-history             Agent work history for a task        [Auth]
GET    /api/bridge/sessions/{sessionId}/follow-up-tasks     Tasks created by this session        [Auth]
```

---

## B2.8 Cross-Context Event Subscriptions

| Upstream Context | Event Subscribed | Handler Behaviour |
|-----------------|-----------------|-------------------|
| Agents | `AgentSessionStartedEvent` | Dispatch `CreateAgentTaskCorrelationCommand` with PrimaryTaskId from event payload. |
| Agents | `AgentSessionApprovedEvent` | Dispatch `HandleSessionApprovedForTaskCommand`. Completes primary task (if set). |
| Agents | `AgentSessionRejectedEvent` | Dispatch `HandleSessionRejectedForTaskCommand`. Marks correlation Failed. |
| Agents | `AgentSessionFailedEvent` | Dispatch `HandleSessionFailedForTaskCommand`. Marks correlation Failed. |
| Agents | `AgentApiTaskCreatedEvent` | Dispatch `RecordAgentCreatedTaskCommand`. Links follow-up task to session. |

### Events Published by This Bridge

This bridge is a pure consumer — it does not publish events consumed by other contexts. Its own domain events (`AgentTaskCorrelationTaskCompletedEvent`, etc.) are raised for audit and UI display, but no other context subscribes to them. The task completion itself is handled by dispatching `CompleteTaskCommand` directly to the Tasks context application layer.
