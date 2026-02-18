# Agent Sessions Context

> **Source**: Designed for v2 — see docs/product/modules/agents.md and docs/scenarios/agent-workflows.md
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## 8.1 Design Principles

1. **AgentSession owns its own lifecycle** — An `AgentSession` manages its own status transitions (Queued → Running → Idle / ContextExhausted / Completed / Failed). The normal resting state after a turn completes is `Idle` — not `Completed`. An Idle session accepts follow-up messages and transitions back to Running when the next turn begins. `Completed` is an explicit terminal closure, reached either when Bruno explicitly closes the session or when a `SessionChain` is fully done. `ContextExhausted` is a separate terminal-for-input state: the session's context window is full and it cannot accept new messages — it must hand off to a new session via `SessionChain`. Status enforcement, budget tracking, metrics capture, and output recording are all self-contained within the session aggregate.

2. **WorkQueue owns execution strategy, not session mechanics** — A `WorkQueue` is responsible for the ordering and sequencing of work items and for deciding when the next item may start. It does not know how sessions run; it only observes session completion events and triggers the next `WorkItem` accordingly. Parallel vs. sequential execution is a queue property, not a session property.

3. **This context is conformist to Identity, Task, and Projects** — `AgentSession` and `WorkItem` reference `UserId`, `TaskId`, and `ProjectId` as opaque identifiers. This context never loads or interprets user profiles, task metadata, or project details — it only stores the IDs for cross-context linkage and defers reads to the originating contexts.

4. **The Agent API is a published language boundary** — The `/api/agent/` sub-namespace exposes a stable published language that running agent processes use to interact with LemonDo. It is authenticated with per-session API keys (`AgentApiKey`), not user JWTs. This keeps agent-initiated operations clearly separated from human-initiated operations.

5. **Budget enforcement is always conservative** — When a session's actual token spend reaches or would exceed its `SessionBudget`, the session is auto-paused and a `SessionBudgetExhaustedEvent` is raised. A session is never allowed to run past its hard cap. Estimated cost warnings are fired at 80% of the cap to give Bruno time to adjust before the hard stop.

6. **Human-in-the-loop approval is a first-class state, but not a resting state** — A session that produces output transitions to `Idle` with `HasPendingReview: true`, making review intent visible in the UI without requiring a separate enum value. Only when Bruno explicitly approves does the session move to `Completed`. Rejection transitions to `Failed`. Idle sessions with or without pending review both accept follow-up messages — the `HasPendingReview` flag is informational, not a gate. This boundary cannot be bypassed via API.

7. **Agent communication is bidirectional and queue-managed** — Sessions are not fire-and-forget. Bruno can send steering messages into a running session (injected immediately into the agentic loop via the SDK's interrupt/send mechanism), or queue follow-up messages that the session processes sequentially after the current turn completes. Idle and Interrupted sessions both accept queued follow-ups. The `SessionMessageQueue` is a first-class domain aggregate — not infrastructure — and its contents are visible in the UI. The session pool (`SessionPool` domain service) enforces a global concurrency cap and tracks utilization as a domain-level concern.

8. **SessionChain tracks long-running work across context window boundaries** — When a session's context window exhausts, the agent creates a handoff document and a new session continues the work. A `SessionChain` aggregate owns this continuity: it links sessions in order, stores handoff documents, and tracks the original objective across multiple sessions. The chain is the durable unit of "a piece of work" when that work spans multiple context windows.

9. **SessionMetrics distinguishes aggregatable from snapshot data** — Token costs and per-model token counts are aggregatable (they sum across turns and subagent contributions roll up into the parent). Context window snapshots are NOT aggregatable — each `usage_update` event from the SDK replaces the previous snapshot rather than accumulating. Subagents have their own independent context windows which are never merged into the parent session's context window, though their costs and tokens do aggregate upward.

---

## 8.2 Runtime Architecture (Event-Sourced Sidecar Pattern)

Each agent session runs as an independent Node.js process using the Claude Agent SDK (`@anthropic-ai/claude-agent-sdk`). The Node.js process publishes raw SDK events to Redis Streams. The .NET backend subscribes to those streams and builds domain state (projections) from the events via the Anti-Corruption Layer (ACL). The domain model never sees raw SDK types.

Communication is **bidirectional**: the .NET backend also writes steering commands to Redis (a separate command stream), which the Node.js sidecar subscribes to. This allows in-flight sessions to receive interrupts, steering messages, and cancel signals from the backend without polling.

**The React UI communicates exclusively with the .NET API backend.** The UI never writes to Redis or communicates with Node.js sidecars directly. All steering, interrupt, and lifecycle commands flow through the .NET API, which validates them as domain commands before writing to Redis. All session output reaches the UI via Server-Sent Events or WebSocket connections managed by the .NET backend.

```
┌──────────┐  HTTP/SSE   ┌──────────┐  cmd stream  ┌───────────┐  cmd stream  ┌─────────────────┐
│  React   │ ──────────→ │  .NET    │ ──────────→  │  Redis    │ ──────────→  │ Node.js process  │
│  UI      │ ←────────── │  API     │ ←──────────  │  Streams  │ ←──────────  │ (Agent SDK)      │
│          │  SSE/WS     │  backend │  raw events  │           │  raw events  │                  │
└──────────┘              └──────────┘              └───────────┘              └─────────────────┘
```

**Benefits of this pattern:**

- **Isolation** — one process crash does not affect other running sessions
- **Scalability** — Node.js processes spin up and down independently of the .NET host
- **Single gateway** — the .NET API is the only entry point for all UI interactions; Redis and Node.js are internal infrastructure invisible to the client
- **Real-time UI** — the frontend subscribes to the .NET SignalR hub for live session output; the hub bridges the Redis Stream to the browser via SSE/WebSocket
- **Replay** — the Redis Stream IS the audit trail; any subscriber can replay from any offset (AG-017)
- **Budget enforcement** — the .NET backend reads the token spend from each stream event and can terminate the Node.js process when the hard cap is reached
- **Bidirectional steering** — the .NET backend writes to a per-session command stream (`agent:session:{sessionId}:commands`); the Node.js sidecar subscribes and injects steering messages or interrupts into the running SDK session

### Session Process Lifecycle

```
StartAgentSessionCommand
        │
        ▼
IAgentRuntime.StartSessionAsync()
        │
        ├─ spawns Node.js process (one per session)
        ├─ passes: sessionId, apiKey, modelId, systemPrompt, allowedTools, worktreeRef,
        │          objective, customTools (McpToolDefinition list)
        └─ Node.js process begins streaming SDK events → Redis Stream key: agent:session:{sessionId}

Redis Stream events (raw SDK types, NOT domain types):
  assistant_message       — text or tool_use content block
  tool_result             — result of a tool call (arrives as a user message in SDK)
  subagent_message        — message from a spawned subagent
  usage_update            — token usage and context window snapshot for the current turn
  context_nearing_limit   — emitted by sidecar when context usage crosses the warn threshold (e.g., 85%)
  context_exhausted       — emitted by sidecar when context window is full; sidecar has already
                            created a handoff document as its last action (via system prompt instruction)
  handoff_document        — carries the structured handoff content (summary, remaining work, decisions)
                            emitted immediately after context_exhausted; the sidecar writes it before
                            the session_end event so the chain can attach it before the new session starts
  session_end             — final status (success, error)

ACL (IAgentRuntimeEventConsumer — .NET background service):
  reads Redis Stream → translates each raw event → dispatches domain command or raises domain event

Command stream (agent:session:{sessionId}:commands):
  Node.js sidecar subscribes to this stream for inbound .NET commands.
  Command types:
    steering_message  — inject a message into the running SDK session immediately (V2: session.send())
    interrupt         — call query.interrupt() to pause the current turn
    pause             — call query.interrupt() and suspend execution
    resume            — resume suspended execution
    stop              — terminate the session cleanly
```

### Session Pool Lifecycle

```
StartAgentSessionCommand
        │
        ▼
SessionPool.CanAllocate()
        │
        ├─ true  → SessionPool.Allocate(sessionId) → SessionAllocatedEvent → proceed with IAgentRuntime.StartSessionAsync()
        └─ false → SessionPoolExhaustedEvent → return PoolExhaustedError to caller
                   (session remains in Queued status; user is notified; they may cancel another session to free a slot)

Session completes / fails / is cancelled:
        │
        ▼
SessionPool.Release(sessionId) → SessionReleasedEvent
        │
        └─ if any Queued sessions are waiting → application layer may dispatch the next StartAgentSessionCommand
```

---

## 8.3 ACL Design — SDK Event to Domain Event Mapping

The Claude Agent SDK has structural conventions that must not leak into the domain. The ACL layer (implemented by `IAgentRuntimeEventConsumer` in the infrastructure layer) applies the following translations.

### SDK Problems the ACL Solves

| SDK Behaviour | Domain Treatment |
|---------------|-----------------|
| Tool call results arrive as `user` role messages | `ToolCall` entity owns its `ToolResult` — result is attached to the call, never modelled as a standalone message |
| Subagent messages are interleaved with parent messages in the same stream | `SubAgentSession` is a separate child entity with its own message list; messages are routed by the ACL based on `subagent_id` |
| No distinction between user / system / agent / webhook message sources | `AgentMessage.Source` is a discriminated enum: `User`, `System`, `Agent`, `SubAgent`, `Webhook` — the ACL classifies each raw event before constructing the domain object |
| Session end event carries a raw exit code | ACL maps exit codes to `FailureReason` enum values or marks the session for `RecordOutput` on clean exit |
| Steering messages are injected via SDK `session.send()` (V2) or prompt injection (V1) | The command stream carries a `steering_message` command; the Node.js sidecar reads it and calls the appropriate SDK method before the model's next turn |
| Interrupt/pause/resume/cancel acknowledgements arrive as sidecar events | ACL dispatches the appropriate confirmation command (`ConfirmSessionInterruptedCommand`, etc.) to transition the session out of its intermediate state |
| `usage_update` carries both per-turn cost/tokens AND the current context window snapshot | ACL maps this to `UpdateSessionMetricsCommand` which aggregates cost/tokens and REPLACES (not adds) the context window snapshot |
| Context window exhaustion is a sidecar-detected condition, not a native SDK event | The sidecar monitors context usage; when the window is full it emits `context_exhausted` + `handoff_document` events before the final `session_end`; ACL translates these to the handoff workflow commands |

### Raw Event → Domain Command Mapping

| Raw SDK Event | ACL Output |
|---------------|-----------|
| `assistant_message` (text) | `RecordAgentMessageCommand { Source: Agent, Content }` |
| `assistant_message` (tool_use) | `RecordToolCallCommand { ToolName, Input }` |
| `tool_result` | `RecordToolCallResultCommand { ToolCallId, Result, IsError }` |
| `subagent_message` | `RecordAgentMessageCommand { Source: SubAgent, SubAgentId, Content }` |
| `usage_update` | `UpdateSessionMetricsCommand { SessionId, SubAgentId?, ModelId, CostUsd, TokenUsage, ContextWindow }` |
| `context_nearing_limit` | `RecordContextNearingLimitCommand { SessionId, UsagePercent }` |
| `context_exhausted` | `RecordContextExhaustedCommand { SessionId }` |
| `handoff_document` | `RecordHandoffDocumentCommand { SessionId, ChainId, Summary, RemainingWork, KeyDecisions? }` |
| `session_end` (clean) | `RecordSessionOutputCommand { ... }` |
| `session_end` (error) | `RecordSessionFailureCommand { FailureReason, Message }` |
| `interrupt_ack` | `ConfirmSessionInterruptedCommand { SessionId }` |
| `pause_ack` | `ConfirmSessionPausedCommand { SessionId }` |
| `resume_ack` | `ConfirmSessionResumedCommand { SessionId }` |
| `stop_ack` | `ConfirmSessionCancelledCommand { SessionId }` |

### Command Stream → Node.js Sidecar Mapping

| Command Written to Redis by .NET | Node.js Sidecar Action |
|----------------------------------|------------------------|
| `steering_message` (priority: Immediate) | Calls `query.interrupt()` then re-invokes `session.send(content)` (V2) or injects into next turn via `UserPromptSubmit` hook (V1 fallback) |
| `steering_message` (priority: Queued) | Enqueues content in local Node.js buffer; injects after current turn's `SDKResultMessage` |
| `interrupt` | Calls `query.interrupt()` — suspends the current turn; writes `interrupt_ack` to event stream |
| `pause` | Calls `query.interrupt()` and suspends execution; writes `pause_ack` to event stream |
| `resume` | Resumes suspended execution; writes `resume_ack` to event stream |
| `stop` | Calls `abortController.abort()` — terminates the session cleanly; writes `stop_ack` to event stream |

---

## 8.4 Entities

### AgentSession (Aggregate Root)

```
AgentSession
├── Id: AgentSessionId (value object)
├── OwnerId: UserId (from Identity context)
├── ProjectId: ProjectId (from Projects context)
├── TaskId: TaskId? (from Task context — the assigned objective; null for templateless runs)
├── WorkQueueId: WorkQueueId? (set when session is part of a queue; null for standalone sessions)
├── WorkItemId: WorkItemId? (the specific work item this session is fulfilling; null for standalone)
├── TemplateId: AgentTemplateId? (the template used to configure this session; null if ad-hoc)
├── ChainId: SessionChainId? (set when this session is part of a SessionChain; null for standalone sessions)
├── Name: SessionName (value object — auto-generated from task title or manually set)
├── Status: AgentSessionStatus (Queued, Running,
│                                Interrupting, Interrupted,
│                                Pausing, Paused, Resuming,
│                                Cancelling, Cancelled,
│                                Idle, ContextExhausted, Completed, Failed)
├── HasPendingReview: bool (true when session has produced output awaiting Bruno's approval;
│                           set to true by RecordOutput(); cleared by ApproveOutput() or RejectOutput();
│                           an Idle session with HasPendingReview = true is shown as "Awaiting Review" in the UI)
├── ExecutionMode: ExecutionMode (Standalone, ParallelBatch, SequentialQueue)
├── Budget: SessionBudget (value object — per-session token/cost cap and current spend)
├── Metrics: SessionMetrics (value object — rich metrics: per-model tokens, context window, subagent breakdowns)
├── WorktreeRef: WorktreeRef? (branch name and path of the auto-created worktree; null if no worktree)
├── ApiKeyHash: string (SHA-256 of the per-session agent API key — never stored in plaintext)
├── Output: SessionOutput? (set when the session completes; null while running)
├── FailureReason: FailureReason? (enum + optional message; set on transition to Failed)
├── RetryCount: int (number of times this session has been retried after failure)
├── StartedAt: DateTimeOffset?
├── CompletedAt: DateTimeOffset?
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Start(worktreeRef?, apiKey) -> AgentSessionStartedEvent
│   │       (transitions Queued -> Running; sets StartedAt; hashes and stores apiKey;
│   │        raises event so Projects context can create worktree if worktreeRef is provided)
│   ├── RecordOutput(outputLines: int, filesChanged: int, testsAdded: int,
│   │               allTestsPassing: bool, commitSha: string?)
│   │       -> AgentSessionOutputReadyEvent
│   │       (transitions Running -> Idle; sets Output value object; sets HasPendingReview = true)
│   ├── RecordTurnCompleted()
│   │       -> AgentSessionTurnCompletedEvent
│   │       (transitions Running -> Idle when a turn ends without producing final output;
│   │        HasPendingReview remains false; session accepts follow-up messages in Idle state)
│   ├── RecordLogChunk(chunk: SessionLogChunk)
│   │       (appends streaming output chunk; does not change status or raise events — streamed via SignalR)
│   │
│   ├── RequestInterrupt() -> SessionInterruptRequestedEvent
│   │       (transitions Running -> Interrupting; validates session is Running;
│   │        the application layer writes an `interrupt` command to the Redis command stream)
│   ├── ConfirmInterrupted() -> SessionInterruptedEvent
│   │       (transitions Interrupting -> Interrupted; called by the ACL when sidecar sends interrupt_ack;
│   │        only valid from Interrupting — prevents duplicate confirmations;
│   │        Interrupted sessions can receive follow-up messages — transitions to Running on next message)
│   │
│   ├── RequestPause() -> SessionPauseRequestedEvent
│   │       (transitions Running -> Pausing; validates session is Running and not already Pausing/Paused;
│   │        the application layer writes a `pause` command to the Redis command stream)
│   ├── ConfirmPaused() -> SessionPausedEvent
│   │       (transitions Pausing -> Paused; called by the ACL when sidecar sends pause_ack;
│   │        only valid from Pausing — prevents duplicate confirmations)
│   │
│   ├── RequestResume() -> SessionResumeRequestedEvent
│   │       (transitions Paused -> Resuming; validates session is Paused;
│   │        the application layer writes a `resume` command to the Redis command stream)
│   ├── ConfirmResumed() -> SessionResumedEvent
│   │       (transitions Resuming -> Running; called by the ACL when sidecar sends resume_ack;
│   │        only valid from Resuming — prevents duplicate confirmations)
│   │
│   ├── RequestCancel() -> SessionCancelRequestedEvent
│   │       (transitions any non-terminal, non-intermediate status -> Cancelling;
│   │        the application layer writes a `stop` command to the Redis command stream)
│   ├── ConfirmCancelled() -> SessionCancelledEvent
│   │       (transitions Cancelling -> Cancelled; called by the ACL when sidecar sends stop_ack;
│   │        also valid as direct transition for sessions that have not yet started a sidecar process)
│   │
│   ├── RecordContextExhausted() -> ContextExhaustedEvent
│   │       (transitions Running -> ContextExhausted; called by ACL when sidecar sends context_exhausted;
│   │        ContextExhausted is a terminal-for-input state — no more messages accepted;
│   │        the session's sidecar has already written a handoff_document event before this)
│   │
│   ├── ApproveOutput() -> AgentSessionApprovedEvent
│   │       (transitions Idle (with HasPendingReview = true) -> Completed; sets CompletedAt;
│   │        clears HasPendingReview; triggers Projects context to merge worktree via event)
│   ├── RejectOutput(reason: string?) -> AgentSessionRejectedEvent
│   │       (transitions Idle (with HasPendingReview = true) -> Failed; sets FailureReason)
│   ├── Close() -> AgentSessionClosedEvent
│   │       (transitions Idle (HasPendingReview = false) -> Completed; sets CompletedAt;
│   │        explicit closure — Bruno is done with this session; no output to approve)
│   ├── RecordFailure(reason: FailureReason, message: string?) -> AgentSessionFailedEvent
│   │       (transitions Running, Paused, Interrupting, Interrupted, Pausing, Resuming -> Failed;
│   │        sets FailureReason and CompletedAt)
│   ├── Retry(instruction: string?) -> AgentSessionRetriedEvent
│   │       (transitions Failed -> Queued; increments RetryCount; clears Output; clears HasPendingReview;
│   │        only valid when RetryCount < MaxRetries)
│   ├── UpdateMetrics(modelId: string, costUsd: decimal, tokenUsage: TokenUsage,
│   │                 contextWindow: ContextWindowSnapshot, subAgentId: SubAgentId?)
│   │       -> SessionMetricsUpdatedEvent
│   │       -> SessionBudgetWarningEvent? (if total cost >= 80% of cap)
│   │       -> SessionBudgetExhaustedEvent (if total cost >= 100% of cap — also triggers RequestPause())
│   │       -> ContextNearingLimitEvent? (if contextWindow.UsagePercent >= warn threshold — raised by
│   │          RecordContextNearingLimit() separately when sidecar emits context_nearing_limit)
│   │       (aggregates cost/tokens into Metrics.TotalCostUsd and Metrics.TokenUsageByModel;
│   │        REPLACES Metrics.ContextWindow snapshot — never adds to it;
│   │        when subAgentId is set, updates Metrics.SubAgentMetrics[subAgentId] and also
│   │        aggregates cost/tokens up into the parent session totals)
│   ├── RecordContextNearingLimit(usagePercent: decimal) -> ContextNearingLimitEvent
│   │       (raises warning event; does not change status; only valid from Running)
│   └── GenerateApiKey() -> string (plaintext, returned once; SHA-256 hash stored on aggregate)
│
└── Invariants:
    ├── Status transitions are strictly controlled:
    │   Queued -> Running (via Start)
    │   Running -> Interrupting (via RequestInterrupt); Interrupting -> Interrupted (via ConfirmInterrupted)
    │   Running -> Pausing (via RequestPause); Pausing -> Paused (via ConfirmPaused)
    │   Paused -> Resuming (via RequestResume); Resuming -> Running (via ConfirmResumed)
    │   Any non-terminal, non-intermediate status -> Cancelling (via RequestCancel);
    │     Cancelling -> Cancelled (via ConfirmCancelled)
    │   Running -> Idle (via RecordOutput or RecordTurnCompleted)
    │   Idle -> Running (when next message is processed / next turn begins)
    │   Idle -> Completed (via ApproveOutput when HasPendingReview = true, or via Close when false)
    │   Idle -> Failed (via RejectOutput when HasPendingReview = true)
    │   Running -> ContextExhausted (via RecordContextExhausted — terminal for input)
    │   Interrupted -> Running (when a follow-up message is processed)
    │   Failed -> Queued (via Retry, only when RetryCount < MaxRetries)
    ├── Intermediate states (Interrupting, Pausing, Resuming, Cancelling) prevent duplicate lifecycle
    │   requests — e.g., RequestPause() raises a domain error if Status is already Pausing or Paused
    ├── A session may not transition to Running if its Budget.HardCapUsd is already exhausted
    ├── ApproveOutput() and RejectOutput() are only valid when Status == Idle AND HasPendingReview == true
    ├── Close() is only valid when Status == Idle AND HasPendingReview == false
    ├── ContextExhausted sessions cannot receive messages — any attempt raises a domain error
    ├── Completed, Cancelled, and ContextExhausted are terminal — no messages accepted after these states
    ├── RetryCount must not exceed MaxRetries (default: 3); Retry() raises a domain error if exceeded
    ├── ApiKeyHash is set once on Start() and is immutable thereafter
    ├── WorktreeRef is immutable once set on Start()
    ├── CompletedAt is set exactly once, on the first transition to a terminal status
    │   (Completed, Failed, or Cancelled)
    ├── Budget.CurrentSpend must never exceed Budget.HardCapUsd
    ├── Metrics.TotalCostUsd and Budget.EstimatedCostUsd must be kept in sync — both are updated
    │   by UpdateMetrics(); they represent the same value from different domain concerns
    └── Metrics.ContextWindow is a snapshot — it is replaced on each UpdateMetrics() call, not summed
```

### SessionChain (Aggregate Root)

A `SessionChain` tracks continuity when a session's context window exhausts and work is handed off to a new session. The chain is the durable representation of "a unit of work" that may span multiple sessions.

```
SessionChain
├── Id: SessionChainId (value object)
├── OwnerId: UserId
├── OriginalObjective: string (the initial task/objective that started the chain; 1-2000 chars)
├── Sessions: IReadOnlyList<ChainedSession> (ordered by Position, 1-based)
│   ├── SessionId: AgentSessionId
│   ├── Position: int (order in chain; 1 = first session)
│   └── JoinedAt: DateTimeOffset
├── CurrentSessionId: AgentSessionId (the active/latest session in the chain)
├── HandoffDocuments: IReadOnlyList<HandoffDocument>
│   ├── Id: HandoffDocumentId (value object, Guid wrapper)
│   ├── FromSessionId: AgentSessionId (the session that exhausted its context)
│   ├── ToSessionId: AgentSessionId (the session that picks up the work)
│   ├── Summary: string (what was accomplished; 1-4000 chars)
│   ├── RemainingWork: string (what still needs to be done; 1-4000 chars)
│   ├── KeyDecisions: string? (important context to carry forward; null if none)
│   └── CreatedAt: DateTimeOffset
├── Status: SessionChainStatus (Active, Completed, Cancelled)
├── CreatedAt: DateTimeOffset
├── CompletedAt: DateTimeOffset?
│
├── Methods:
│   ├── Create(ownerId, objective, firstSessionId) -> SessionChainCreatedEvent
│   │       (creates chain with Status = Active; adds first session at Position 1;
│   │        sets CurrentSessionId = firstSessionId)
│   ├── InitiateHandoff(fromSessionId, summary, remainingWork, keyDecisions?)
│   │       -> SessionHandoffInitiatedEvent
│   │       (creates a HandoffDocument with a placeholder ToSessionId;
│   │        ToSessionId is filled in when AttachSession() is called next;
│   │        validates fromSessionId == CurrentSessionId;
│   │        only valid when Status == Active)
│   ├── AttachSession(newSessionId) -> SessionAddedToChainEvent
│   │       (appends the new session to Sessions at the next Position;
│   │        updates CurrentSessionId = newSessionId;
│   │        fills in the pending HandoffDocument.ToSessionId if one exists;
│   │        only valid when Status == Active)
│   ├── Complete() -> SessionChainCompletedEvent
│   │       (transitions Active -> Completed; sets CompletedAt;
│   │        only valid when the current session's status is Idle or Completed)
│   └── Cancel() -> SessionChainCancelledEvent
│           (transitions Active -> Cancelled; application layer cancels all non-terminal sessions
│            in the chain via CancelAgentSessionCommand)
│
└── Invariants:
    ├── Sessions list must be ordered and contiguous by Position (1, 2, 3, ..., N)
    ├── CurrentSessionId must always reference the last session in the Sessions list
    ├── Each HandoffDocument's FromSessionId and ToSessionId must reference adjacent sessions
    │   (FromSession.Position + 1 == ToSession.Position)
    ├── Only Active chains can accept new sessions (AttachSession raises error if Completed or Cancelled)
    ├── A chain with a single session that never exhausted its context has zero HandoffDocuments
    ├── OriginalObjective must be 1-2000 characters
    ├── HandoffDocument.Summary and RemainingWork must each be 1-4000 characters
    └── CompletedAt is set exactly once on the first transition to Completed or Cancelled
```

### WorkQueue (Aggregate Root)

```
WorkQueue
├── Id: WorkQueueId (value object)
├── OwnerId: UserId
├── ProjectId: ProjectId (from Projects context)
├── Name: QueueName (value object)
├── ExecutionMode: ExecutionMode (Parallel, Sequential)
├── Status: WorkQueueStatus (Pending, Running, Paused, Completed, Failed, Cancelled)
├── Items: IReadOnlyList<WorkItem> (ordered by Position, then Priority)
├── Budget: QueueBudget (value object — total token/cost cap across all items; optional)
├── RequiresVerificationGate: bool (if true, each session must pass its gate before next item starts)
├── MaxParallelSessions: int (for Parallel mode; default: 3)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, projectId, name, mode, items, budget?, requiresVerificationGate?, maxParallel?)
│   │       -> WorkQueueCreatedEvent
│   ├── Start() -> WorkQueueStartedEvent
│   │       (transitions Pending -> Running; emits for first item(s) to be dispatched to sessions)
│   ├── AdvanceQueue(completedItemId: WorkItemId) -> WorkQueueAdvancedEvent?
│   │       (called when a WorkItem's session is approved; marks item Complete;
│   │        for Sequential mode: transitions next Queued item to Ready;
│   │        for Parallel mode: marks completed slot as freed; returns event only if queue advances)
│   ├── RecordItemFailure(itemId: WorkItemId) -> WorkQueueItemFailedEvent
│   │       (marks item as Failed; for Sequential mode, pauses the entire queue)
│   ├── Pause() -> WorkQueuePausedEvent
│   ├── Resume() -> WorkQueueResumedEvent
│   ├── Cancel() -> WorkQueueCancelledEvent
│   ├── ReorderItems(orderedItemIds: IReadOnlyList<WorkItemId>) -> WorkQueueReorderedEvent
│   │       (only valid when Status == Pending; resets Position values)
│   └── UpdateQueueBudget(newBudget: QueueBudget) -> WorkQueueBudgetUpdatedEvent
│
└── Invariants:
    ├── Must have at least one WorkItem
    ├── In Sequential mode, only one item may be in Running status at a time
    ├── In Parallel mode, the number of Running items must not exceed MaxParallelSessions
    ├── ReorderItems() is only valid when Status == Pending
    ├── A Cancelled or Completed queue cannot be restarted
    ├── When QueueBudget is set, the sum of all session costs must not exceed QueueBudget.HardCapUsd;
    │   the queue auto-pauses when this threshold is reached
    └── Item positions must be unique and contiguous (1, 2, 3, ..., N)
```

### WorkItem (Entity, owned by WorkQueue)

```
WorkItem
├── Id: WorkItemId (value object)
├── QueueId: WorkQueueId (parent queue)
├── TaskId: TaskId (from Task context — the objective)
├── Position: int (order in the queue; 1-based)
├── Priority: WorkItemPriority (Low, Normal, High, Critical)
├── Status: WorkItemStatus (Queued, Ready, Running, VerificationPassed, Completed, Failed, Skipped)
├── AssignedSessionId: AgentSessionId? (set when a session is started for this item)
├── VerificationPassed: bool (set to true when session's verification gate passes)
├── CompletedAt: DateTimeOffset?
│
└── Invariants:
    ├── TaskId is immutable after creation
    ├── Position must be >= 1
    ├── AssignedSessionId is set once on transition to Running and is then immutable
    ├── VerificationPassed can only be set to true when Status == Running or VerificationPassed
    └── Completed and Skipped are terminal states — no further transitions allowed
```

### AgentTemplate (Aggregate Root)

```
AgentTemplate
├── Id: AgentTemplateId (value object)
├── OwnerId: UserId
├── Name: TemplateName (value object)
├── Description: string?
├── ModelId: string (e.g., "claude-opus-4-5", "claude-sonnet-4-5")
├── SystemPrompt: string? (base instructions injected into every session)
├── AllowedTools: IReadOnlyList<string> (tool names the agent is permitted to use)
├── CustomToolDefinitions: IReadOnlyList<McpToolDefinition>
│       (MCP server tool configurations that extend agent capabilities; empty for standard templates)
├── ClassificationRules: IReadOnlyList<ClassificationRule> (for automation templates — e.g., email triage)
├── Schedule: AgentSchedule? (null for on-demand templates; set for scheduled automations)
├── DefaultBudget: SessionBudget (default per-session budget when using this template)
├── IsActive: bool
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, name, modelId, systemPrompt?, allowedTools, defaultBudget,
│   │          customToolDefinitions?)
│   │       -> AgentTemplateCreatedEvent
│   ├── AddClassificationRule(rule: ClassificationRule) -> AgentTemplateUpdatedEvent
│   ├── RemoveClassificationRule(ruleId: ClassificationRuleId) -> AgentTemplateUpdatedEvent
│   ├── AddCustomToolDefinition(tool: McpToolDefinition) -> AgentTemplateUpdatedEvent
│   │       (registers an MCP tool that sessions using this template will have access to)
│   ├── RemoveCustomToolDefinition(toolName: string) -> AgentTemplateUpdatedEvent
│   ├── UpdateSchedule(schedule: AgentSchedule?) -> AgentTemplateUpdatedEvent
│   ├── Deactivate() -> AgentTemplateDeactivatedEvent
│   └── Activate() -> AgentTemplateActivatedEvent
│
└── Invariants:
    ├── Name must be 1-100 characters
    ├── ModelId must be a non-empty string (format validated at application layer against allowed models list)
    ├── AllowedTools must contain at least one entry
    ├── DefaultBudget.HardCapUsd must be > 0
    ├── A deactivated template cannot be used to create new sessions
    ├── ClassificationRuleId values must be unique within the template
    ├── McpToolDefinition.Name values must be unique within the template
    └── McpToolDefinition.ServerUri must be a valid absolute URI
```

### AgentMessage (Entity, owned by AgentSession or SubAgentSession)

```
AgentMessage
├── Id: MessageId (value object)
├── SessionId: AgentSessionId (parent session)
├── SubAgentId: SubAgentId? (set only when Source == SubAgent; ties to SubAgentSession)
├── Source: AgentMessageSource (discriminated enum: User, System, Agent, SubAgent, Webhook)
├── Content: string (text content of the message)
├── Timestamp: DateTimeOffset
│
└── Invariants:
    ├── Content must be non-empty
    ├── When Source == SubAgent, SubAgentId must be set
    ├── When Source != SubAgent, SubAgentId must be null
    ├── Timestamp is immutable after creation
    └── SubAgent messages must never appear in the parent session's top-level message list;
        they belong exclusively to the SubAgentSession entity
```

### ToolCall (Entity, owned by AgentSession or SubAgentSession)

```
ToolCall
├── Id: ToolCallId (value object)
├── SessionId: AgentSessionId (parent session)
├── SubAgentId: SubAgentId? (set when this tool call was made by a subagent)
├── ToolName: string (name of the tool invoked, e.g., "Read", "Bash", "Write")
├── Input: string (JSON-serialised tool input)
├── Result: ToolResult? (null while Pending; set when the tool returns)
├── Status: ToolCallStatus (Pending, Success, Error)
├── StartedAt: DateTimeOffset
├── CompletedAt: DateTimeOffset?
│
└── Invariants:
    ├── ToolName must be non-empty
    ├── Input must be valid JSON (validated on construction)
    ├── Result is set exactly once, on transition out of Pending
    ├── Status transitions: Pending -> Success or Pending -> Error only
    ├── CompletedAt is set when Status transitions from Pending
    ├── The ToolResult belongs to this entity — it is NEVER modelled as a standalone AgentMessage
    └── A ToolCall for a subagent is owned by that SubAgentSession, not the parent session
```

### SubAgentSession (Entity, owned by AgentSession)

```
SubAgentSession
├── Id: SubAgentId (value object)
├── ParentSessionId: AgentSessionId (the session that spawned this subagent)
├── ModelId: string (model used by this subagent)
├── Objective: string (the task objective passed to the subagent)
├── Messages: IReadOnlyList<AgentMessage> (messages owned by this subagent)
├── ToolCalls: IReadOnlyList<ToolCall> (tool calls made by this subagent)
├── Status: AgentSessionStatus (Running, Completed, Failed)
├── StartedAt: DateTimeOffset
├── CompletedAt: DateTimeOffset?
│
└── Invariants:
    ├── ParentSessionId is immutable after creation
    ├── Messages in this entity must have Source == SubAgent
    ├── SubAgent messages must never appear in the parent AgentSession's message list
    ├── ToolCalls owned here must reference this SubAgentId, not null
    └── Status is terminal once Completed or Failed — no further transitions
```

### SessionMessageQueue (Aggregate Root)

The message queue is a first-class domain aggregate — not an infrastructure queue. It is the domain-level representation of all pending communications from the user (or system) to a running session. Bruno can see and manage this queue in the UI.

```
SessionMessageQueue
├── Id: AgentSessionId (same ID as the owning AgentSession — 1:1 relationship)
├── Messages: IReadOnlyList<SessionMessage>
│   ├── MessageId: Guid
│   ├── Content: string (the message text to inject into the running session)
│   ├── Priority: MessagePriority (Immediate | Queued)
│   │       Immediate — injected into the active agentic loop as soon as possible (interrupt + send)
│   │       Queued    — processed sequentially after the current SDK turn completes
│   ├── Status: MessageStatus (Pending | Delivered | Cancelled)
│   ├── Source: MessageSource (User | System | Webhook)
│   ├── CreatedAt: DateTimeOffset
│   └── DeliveredAt: DateTimeOffset?
│
├── Methods:
│   ├── Enqueue(content: string, priority: MessagePriority, source: MessageSource)
│   │       -> MessageEnqueuedEvent
│   │       (adds a new SessionMessage with Status = Pending; Immediate messages skip ahead
│   │        of all Queued messages in the processing order)
│   ├── Cancel(messageId: Guid)
│   │       -> MessageCancelledEvent
│   │       (sets Status = Cancelled; only valid when Status == Pending;
│   │        raises domain error if the message is already Delivered)
│   ├── Promote(messageId: Guid)
│   │       -> MessagePromotedToImmediateEvent
│   │       (changes Priority from Queued to Immediate; only valid when
│   │        Status == Pending AND Priority == Queued)
│   ├── MarkDelivered(messageId: Guid)
│   │       -> MessageDeliveredEvent
│   │       (sets Status = Delivered and DeliveredAt; called by the infrastructure
│   │        layer once the Node.js sidecar acknowledges receipt via the command stream)
│   └── GetNextPending() -> SessionMessage?
│           (returns the next message to deliver: first by Priority (Immediate before Queued),
│            then by CreatedAt ascending; returns null if no Pending messages exist)
│
└── Invariants:
    ├── Cannot cancel a Delivered message — Cancel() on a non-Pending message raises a domain error
    ├── Cannot promote a message that is already Immediate — Promote() on an Immediate message raises
    │   a domain error
    ├── Cannot promote a message that is not Pending — Promote() on Delivered or Cancelled raises
    │   a domain error
    ├── Queue order for Queued messages is preserved by CreatedAt ascending (FIFO within priority)
    ├── Immediate messages always precede Queued messages in GetNextPending() ordering
    ├── Content must be 1-4000 characters
    └── MessageId values must be unique within the queue
```

---

## 8.5 Value Objects

```
AgentSessionId          -> Guid wrapper
WorkQueueId             -> Guid wrapper
WorkItemId              -> Guid wrapper
AgentTemplateId         -> Guid wrapper
ClassificationRuleId    -> Guid wrapper
MessageId               -> Guid wrapper
ToolCallId              -> Guid wrapper
SubAgentId              -> Guid wrapper
SessionChainId          -> Guid wrapper
HandoffDocumentId       -> Guid wrapper

SessionName             -> Non-empty string, 1-200 chars, trimmed
QueueName               -> Non-empty string, 1-200 chars, trimmed
TemplateName            -> Non-empty string, 1-100 chars, trimmed

AgentSessionStatus      -> Enum: Queued, Running,
                                 Interrupting, Interrupted,
                                 Pausing, Paused,
                                 Resuming,
                                 Cancelling, Cancelled,
                                 Idle, ContextExhausted,
                                 Completed, Failed
                           Idle: normal resting state after a turn completes; accepts follow-ups.
                           ContextExhausted: context window full; no new messages accepted; triggers
                             handoff to a new session in the chain.
                           Completed: explicit terminal closure (Bruno approved output or closed session).
                           Intermediate states (Interrupting, Pausing, Resuming, Cancelling) represent
                           pending sidecar acknowledgements and prevent duplicate lifecycle requests.
                           Terminal states: Cancelled, Completed, ContextExhausted, Failed.
                           Note: ContextExhausted is terminal-for-input but the sidecar process has
                           already ended; the session record is preserved for audit trail.

SessionChainStatus      -> Enum: Active, Completed, Cancelled
                           Active: chain is live and may accept new sessions (via handoff).
                           Completed: all work is done; no new sessions will be added.
                           Cancelled: chain was cancelled; all non-terminal sessions were cancelled.

WorkQueueStatus         -> Enum: Pending, Running, Paused, Completed, Failed, Cancelled
WorkItemStatus          -> Enum: Queued, Ready, Running, VerificationPassed, Completed, Failed, Skipped
WorkItemPriority        -> Enum: Low, Normal, High, Critical
ExecutionMode           -> Enum: Standalone, Parallel, Sequential
FailureReason           -> Enum: BudgetExhausted, VerificationFailed, AgentError, ManualCancellation,
                                 Timeout, InfrastructureError
AgentMessageSource      -> Enum: User, System, Agent, SubAgent, Webhook
                           (discriminated; determines routing and ACL classification of raw SDK events)
ToolCallStatus          -> Enum: Pending, Success, Error
                           (Pending while the tool is executing; Success or Error on completion;
                            result is attached to the ToolCall, never as a separate message)

MessagePriority         -> Enum: Immediate | Queued
                           Immediate messages are injected into the active SDK agentic loop ASAP
                           (interrupt + send); Queued messages are processed after the current
                           turn's SDKResultMessage arrives
MessageStatus           -> Enum: Pending | Delivered | Cancelled
                           Pending = not yet sent to the sidecar; Delivered = sidecar acknowledged;
                           Cancelled = cancelled before delivery
MessageSource           -> Enum: User | System | Webhook
                           Identifies who enqueued the message; User = Bruno via the UI;
                           System = LemonDo automation (e.g., budget warning injection);
                           Webhook = external system trigger

SessionBudget           -> { HardCapUsd: decimal, WarnAtPercent: int (default 80),
                              TokensUsed: int, EstimatedCostUsd: decimal }
                           HardCapUsd must be > 0; WarnAtPercent must be 1-99;
                           EstimatedCostUsd must be kept in sync with SessionMetrics.TotalCostUsd

QueueBudget             -> { HardCapUsd: decimal, WarnAtPercent: int (default 80),
                              TotalSpentUsd: decimal }
                           HardCapUsd must be > 0

SessionOutput           -> { OutputLineCount: int, FilesChanged: int, TestsAdded: int,
                              AllTestsPassing: bool, CommitSha: string? }
                           CommitSha must be a valid 40-char git SHA if set; all counts >= 0

SessionLogChunk         -> { SequenceNumber: int, Content: string, RecordedAt: DateTimeOffset }
                           Content must be non-empty; SequenceNumber must be >= 0

ToolResult              -> { Content: string, IsError: bool }
                           Content must be non-empty; belongs to ToolCall, never a standalone message

WorktreeRef             -> { BranchName: string, LocalPath: string }
                           BranchName must match git branch naming rules (non-empty, no spaces)
                           LocalPath must be non-empty
                           NOTE: Projects context now exposes WorktreeId (Guid wrapper);
                           this value object should be migrated to store WorktreeId alongside or
                           instead of BranchName+LocalPath in a future revision — see Design Notes

AgentSchedule           -> { CronExpression: string, TimeZone: string, IsEnabled: bool }
                           CronExpression must be a valid 5-field cron expression
                           TimeZone must be a valid IANA time zone identifier

ClassificationRule      -> { Id: ClassificationRuleId, MatchField: string, MatchPattern: string,
                              ClassifyAs: string, MinPriority: WorkItemPriority? }
                           MatchField and MatchPattern must be non-empty

SessionPoolConfig       -> { MaxConcurrentSessions: int }
                           MaxConcurrentSessions must be >= 1; default: 20; configurable per user
                           Stored as a user-level configuration; read by the SessionPool domain service

McpToolDefinition       -> { Name: string, Description: string, InputSchema: string (JSON),
                              ServerUri: Uri }
                           Name must be non-empty, 1-100 chars
                           Description must be non-empty, 1-500 chars
                           InputSchema must be valid JSON (JSON Schema format)
                           ServerUri must be a valid absolute URI pointing to an MCP server endpoint
                           (transport type — http, sse, or stdio — is inferred or configured at
                            infrastructure layer; the domain only stores the URI)

TokenUsage              -> { InputTokens: int, OutputTokens: int, CacheReadTokens: int,
                              CacheWriteTokens: int, TotalTokens: int (computed) }
                           TotalTokens = InputTokens + OutputTokens + CacheReadTokens + CacheWriteTokens;
                           all token counts must be >= 0;
                           AGGREGATABLE: values are summed across turns and subagent contributions
                           roll up into the parent session's TokenUsageByModel entries

ContextWindowSnapshot   -> { InputTokens: int, OutputTokens: int, CacheReadTokens: int,
                              CacheWriteTokens: int, TotalTokens: int (computed),
                              ModelContextLimit: int, UsagePercent: decimal (computed),
                              RecordedAt: DateTimeOffset }
                           TotalTokens = InputTokens + OutputTokens + CacheReadTokens + CacheWriteTokens;
                           UsagePercent = TotalTokens / ModelContextLimit * 100;
                           ModelContextLimit is the model's maximum context window (e.g., 200000 for Opus);
                           NOT AGGREGATABLE: each usage_update from the SDK replaces the previous snapshot
                           entirely — this is the CURRENT context size, not a cumulative total;
                           subagent context windows are independent and are never merged into the parent

SubAgentMetricsSnapshot -> { TotalCostUsd: decimal,
                              TokenUsageByModel: IReadOnlyDictionary<string, TokenUsage>,
                              ContextWindow: ContextWindowSnapshot }
                           TotalCostUsd and TokenUsageByModel are AGGREGATABLE into the parent session;
                           ContextWindow is independent of the parent and is NOT aggregated upward;
                           keyed by SubAgentId in SessionMetrics.SubAgentMetrics

SessionMetrics          -> { TotalCostUsd: decimal,
                              TokenUsageByModel: IReadOnlyDictionary<string, TokenUsage>,
                              ContextWindow: ContextWindowSnapshot?,
                              SubAgentMetrics: IReadOnlyDictionary<SubAgentId, SubAgentMetricsSnapshot>,
                              TurnCount: int,
                              LastTurnAt: DateTimeOffset? }
                           TotalCostUsd: running total — aggregates cost across all turns AND subagent costs;
                           TokenUsageByModel: keyed by model ID (e.g., "claude-opus-4-6");
                             aggregates across turns; subagent tokens roll up into the parent totals;
                           ContextWindow: REPLACED on each usage_update — not summed; null before first turn;
                           SubAgentMetrics: each subagent's own cost/tokens/context window snapshot;
                             subagent costs and tokens aggregate into the parent TotalCostUsd and
                             TokenUsageByModel, but subagent context windows do NOT;
                           TurnCount incremented each time a turn completes (session transitions Running -> Idle);
                           LastTurnAt updated each time TurnCount is incremented
```

---

## 8.6 Domain Events

```
AgentSessionStartedEvent            { SessionId, OwnerId, ProjectId, TaskId?, WorkQueueId?,
                                       WorktreeRef?, ExecutionMode }

AgentSessionTurnCompletedEvent      { SessionId, OwnerId, TurnCount, LastTurnAt }
                                    (raised when a turn ends without final output — session transitions
                                     Running -> Idle; HasPendingReview remains false)

SessionInterruptRequestedEvent      { SessionId, OwnerId }
                                    (raised when RequestInterrupt() transitions session to Interrupting;
                                     application layer writes `interrupt` command to Redis command stream)
SessionInterruptedEvent             { SessionId, OwnerId }
                                    (raised when ConfirmInterrupted() transitions Interrupting -> Interrupted;
                                     ACL dispatches ConfirmSessionInterruptedCommand after sidecar interrupt_ack)

SessionPauseRequestedEvent          { SessionId, OwnerId }
                                    (raised when RequestPause() transitions session to Pausing;
                                     application layer writes `pause` command to Redis command stream)
SessionPausedEvent                  { SessionId, OwnerId }
                                    (raised when ConfirmPaused() transitions Pausing -> Paused;
                                     ACL dispatches ConfirmSessionPausedCommand after sidecar pause_ack)

SessionResumeRequestedEvent         { SessionId, OwnerId }
                                    (raised when RequestResume() transitions session to Resuming;
                                     application layer writes `resume` command to Redis command stream)
SessionResumedEvent                 { SessionId, OwnerId }
                                    (raised when ConfirmResumed() transitions Resuming -> Running;
                                     ACL dispatches ConfirmSessionResumedCommand after sidecar resume_ack)

SessionCancelRequestedEvent         { SessionId, OwnerId }
                                    (raised when RequestCancel() transitions session to Cancelling;
                                     application layer writes `stop` command to Redis command stream)
SessionCancelledEvent               { SessionId, OwnerId }
                                    (raised when ConfirmCancelled() transitions Cancelling -> Cancelled;
                                     ACL dispatches ConfirmSessionCancelledCommand after sidecar stop_ack;
                                     also raised directly for Queued sessions that never started a sidecar)

AgentSessionOutputReadyEvent        { SessionId, OwnerId, ProjectId, TaskId?,
                                       FilesChanged, TestsAdded, AllTestsPassing }
                                    (session transitions Running -> Idle; HasPendingReview = true)
AgentSessionApprovedEvent           { SessionId, OwnerId, ProjectId, TaskId?,
                                       WorkQueueId?, WorkItemId?, CommitSha? }
                                    (session transitions Idle -> Completed)
AgentSessionClosedEvent             { SessionId, OwnerId }
                                    (session transitions Idle -> Completed without pending review;
                                     Bruno explicitly closed the session)
AgentSessionRejectedEvent           { SessionId, OwnerId, WorkQueueId?, WorkItemId? }
AgentSessionFailedEvent             { SessionId, OwnerId, FailureReason, WorkQueueId?, WorkItemId? }
AgentSessionRetriedEvent            { SessionId, OwnerId, RetryCount, AdditionalInstruction? }
SessionBudgetWarningEvent           { SessionId, OwnerId, SpentUsd, CapUsd, PercentUsed }
SessionBudgetExhaustedEvent         { SessionId, OwnerId, SpentUsd, CapUsd }

ContextNearingLimitEvent            { SessionId, OwnerId, UsagePercent, ModelContextLimit }
                                    (raised when context window usage crosses the warn threshold, e.g., 85%;
                                     UI shows a warning so Bruno can plan for the upcoming handoff;
                                     does not change session status)
ContextExhaustedEvent               { SessionId, OwnerId, ChainId? }
                                    (raised when session transitions Running -> ContextExhausted;
                                     ChainId is set if this session is already part of a chain;
                                     triggers the handoff workflow in the application layer)

SessionChainCreatedEvent            { ChainId, OwnerId, OriginalObjective, FirstSessionId }
SessionHandoffInitiatedEvent        { ChainId, FromSessionId, HandoffDocumentId }
                                    (raised when InitiateHandoff() creates the handoff document;
                                     ToSessionId will be filled in when the next session is attached)
SessionAddedToChainEvent            { ChainId, NewSessionId, Position }
                                    (raised when AttachSession() adds the continuation session to the chain)
SessionChainCompletedEvent          { ChainId, OwnerId, TotalSessions, TotalCostUsd }
SessionChainCancelledEvent          { ChainId, OwnerId }

SessionMetricsUpdatedEvent          { SessionId, TotalCostUsd, ContextWindowUsagePercent, TurnCount }
                                    (raised on each metrics update; UI subscribes for live cost/context display;
                                     ContextWindowUsagePercent is from the replaced snapshot, not cumulative)

AgentMessageRecordedEvent           { SessionId, MessageId, Source: AgentMessageSource,
                                       SubAgentId?, Timestamp }
                                    (raised when the ACL translates a raw SDK assistant/user message
                                     into a classified domain message)

ToolCallStartedEvent                { SessionId, ToolCallId, ToolName, SubAgentId? }
                                    (raised when the ACL observes a tool_use block from the SDK)
ToolCallCompletedEvent              { SessionId, ToolCallId, ToolName, Status: ToolCallStatus,
                                       SubAgentId?, IsError: bool }
                                    (raised when the ACL attaches a tool_result to the ToolCall entity;
                                     the result is attached to the call — never raised as a message event)

SubAgentSessionStartedEvent         { ParentSessionId, SubAgentId, ModelId }
                                    (raised when the ACL detects a subagent spawn from the SDK stream)
SubAgentSessionCompletedEvent       { ParentSessionId, SubAgentId }
SubAgentSessionFailedEvent          { ParentSessionId, SubAgentId, FailureReason }

WorkQueueCreatedEvent               { QueueId, OwnerId, ProjectId, ExecutionMode, ItemCount }
WorkQueueStartedEvent               { QueueId, OwnerId, ProjectId }
WorkQueueAdvancedEvent              { QueueId, CompletedItemId, NextItemId? }
WorkQueueItemFailedEvent            { QueueId, ItemId, SessionId }
WorkQueuePausedEvent                { QueueId, OwnerId }
WorkQueueResumedEvent               { QueueId, OwnerId }
WorkQueueCancelledEvent             { QueueId, OwnerId }
WorkQueueReorderedEvent             { QueueId, OwnerId }
WorkQueueBudgetUpdatedEvent         { QueueId, OwnerId, NewHardCapUsd }
WorkQueueCompletedEvent             { QueueId, OwnerId, ProjectId, TotalItemCount,
                                       CompletedCount, FailedCount, TotalCostUsd }

AgentTemplateCreatedEvent           { TemplateId, OwnerId, Name }
AgentTemplateUpdatedEvent           { TemplateId, OwnerId }
AgentTemplateDeactivatedEvent       { TemplateId, OwnerId }
AgentTemplateActivatedEvent         { TemplateId, OwnerId }

AgentApiTaskCreatedEvent            { SessionId, OwnerId, CreatedTaskId, LinkedTaskId?,
                                       ProjectId, Source: "AgentApi" }
                                    (raised when an agent calls the Agent API to create a follow-up task;
                                     CreatedTaskId references the new Task in the Task context)

MessageEnqueuedEvent                { SessionId, MessageId, Priority: MessagePriority,
                                       Source: MessageSource }
                                    (raised when a new message is added to the SessionMessageQueue)
MessageDeliveredEvent               { SessionId, MessageId, DeliveredAt: DateTimeOffset }
                                    (raised when the Node.js sidecar acknowledges receipt of the message
                                     via the command stream acknowledgement)
MessageCancelledEvent               { SessionId, MessageId }
                                    (raised when a Pending message is cancelled before delivery)
MessagePromotedToImmediateEvent     { SessionId, MessageId }
                                    (raised when a Queued message's priority is elevated to Immediate)

SessionAllocatedEvent               { SessionId, ActiveCount: int, MaxCount: int }
                                    (raised by SessionPool when a session slot is allocated;
                                     ActiveCount and MaxCount enable UI pool utilization display)
SessionReleasedEvent                { SessionId, ActiveCount: int, MaxCount: int }
                                    (raised by SessionPool when a session slot is freed on
                                     completion, failure, or cancellation)
SessionPoolExhaustedEvent           { AttemptedSessionId, CurrentCount: int, MaxCount: int }
                                    (raised when StartAgentSessionCommand cannot be fulfilled because
                                     the pool is at capacity; the session remains Queued)
```

---

## 8.7 Use Cases

```
Commands:
├── StartAgentSessionCommand         { ProjectId, TaskId?, TemplateId?, Name?,
│                                       BudgetCapUsd: decimal, AutoCreateWorktree: bool,
│                                       ChainId?: SessionChainId }
│       -> Creates an AgentSession (Status = Queued), generates API key, if AutoCreateWorktree
│          publishes AgentSessionStartedEvent which the Projects context handles to create a worktree.
│          Before spawning the process, calls SessionPool.CanAllocate(); if false, raises
│          SessionPoolExhaustedEvent and returns a domain error without changing session status.
│          When ChainId is provided, the new session is attached to the existing chain
│          (used internally by the handoff workflow via AttachSession()).
│          Returns the plaintext API key (shown once) and session ID.
│
├── InterruptAgentSessionCommand     { SessionId }
│       -> Calls session.RequestInterrupt(); publishes SessionInterruptRequestedEvent.
│          Application layer writes `interrupt` command to Redis command stream.
│          Session transitions to Interrupting; waits for sidecar interrupt_ack.
│
├── ConfirmSessionInterruptedCommand { SessionId }
│       (Internal — dispatched by IAgentRuntimeEventConsumer ACL on interrupt_ack from sidecar)
│       -> Calls session.ConfirmInterrupted(); publishes SessionInterruptedEvent.
│          Session transitions Interrupting -> Interrupted.
│
├── PauseAgentSessionCommand         { SessionId }
│       -> Calls session.RequestPause(); publishes SessionPauseRequestedEvent.
│          Application layer writes `pause` command to Redis command stream.
│          Session transitions to Pausing; waits for sidecar pause_ack.
│
├── ConfirmSessionPausedCommand      { SessionId }
│       (Internal — dispatched by IAgentRuntimeEventConsumer ACL on pause_ack from sidecar)
│       -> Calls session.ConfirmPaused(); publishes SessionPausedEvent.
│          Session transitions Pausing -> Paused.
│
├── ResumeAgentSessionCommand        { SessionId }
│       -> Calls session.RequestResume(); publishes SessionResumeRequestedEvent.
│          Application layer writes `resume` command to Redis command stream.
│          Session transitions to Resuming; waits for sidecar resume_ack.
│          Application layer also drains pending Immediate messages from SessionMessageQueue
│          before resume, per the message queue delivery invariant.
│
├── ConfirmSessionResumedCommand     { SessionId }
│       (Internal — dispatched by IAgentRuntimeEventConsumer ACL on resume_ack from sidecar)
│       -> Calls session.ConfirmResumed(); publishes SessionResumedEvent.
│          Session transitions Resuming -> Running.
│
├── CancelAgentSessionCommand        { SessionId }
│       -> Calls session.RequestCancel(); publishes SessionCancelRequestedEvent.
│          Application layer writes `stop` command to Redis command stream.
│          Session transitions to Cancelling; waits for sidecar stop_ack.
│          For sessions in Queued status (no sidecar yet), ConfirmCancelled is called immediately.
│
├── ConfirmSessionCancelledCommand   { SessionId }
│       (Internal — dispatched by IAgentRuntimeEventConsumer ACL on stop_ack from sidecar)
│       -> Calls session.ConfirmCancelled(); publishes SessionCancelledEvent.
│          Session transitions Cancelling -> Cancelled.
│
├── ApproveSessionOutputCommand      { SessionId }
│       -> Calls session.ApproveOutput(); publishes AgentSessionApprovedEvent.
│          Only valid when session Status == Idle AND HasPendingReview == true.
│          Application layer coordinates: calls Task context to complete the assigned task,
│          and publishes event for Projects context to merge the worktree.
│
├── RejectSessionOutputCommand       { SessionId, Reason?: string }
│       -> Calls session.RejectOutput(reason); publishes AgentSessionRejectedEvent.
│          Only valid when session Status == Idle AND HasPendingReview == true.
│
├── CloseAgentSessionCommand         { SessionId }
│       -> Calls session.Close(); publishes AgentSessionClosedEvent.
│          Only valid when session Status == Idle AND HasPendingReview == false.
│          Explicit closure when Bruno is done and there is no output to review.
│
├── RetryAgentSessionCommand         { SessionId, AdditionalInstruction?: string }
│       -> Calls session.Retry(); publishes AgentSessionRetriedEvent
│
├── RecordContextNearingLimitCommand { SessionId, UsagePercent: decimal }
│       (Internal — dispatched by IAgentRuntimeEventConsumer ACL on context_nearing_limit from sidecar)
│       -> Calls session.RecordContextNearingLimit(usagePercent); publishes ContextNearingLimitEvent.
│          No status change; warning only.
│
├── RecordContextExhaustedCommand    { SessionId }
│       (Internal — dispatched by IAgentRuntimeEventConsumer ACL on context_exhausted from sidecar)
│       -> Calls session.RecordContextExhausted(); publishes ContextExhaustedEvent.
│          Session transitions Running -> ContextExhausted.
│          Application layer reacts to ContextExhaustedEvent: if session.ChainId is set,
│          loads the chain and waits for the handoff_document event;
│          if session.ChainId is null, creates a new chain via CreateSessionChainCommand.
│
├── RecordHandoffDocumentCommand     { SessionId, ChainId: SessionChainId,
│                                       Summary: string, RemainingWork: string,
│                                       KeyDecisions?: string }
│       (Internal — dispatched by IAgentRuntimeEventConsumer ACL on handoff_document from sidecar)
│       -> Loads the SessionChain; calls chain.InitiateHandoff(fromSessionId, summary,
│          remainingWork, keyDecisions); publishes SessionHandoffInitiatedEvent.
│          Application layer then dispatches StartAgentSessionCommand with ChainId set,
│          passing the handoff content as the new session's objective context.
│          The new session creation triggers chain.AttachSession() via SessionAddedToChainEvent.
│
├── CreateSessionChainCommand        { OwnerId, OriginalObjective, FirstSessionId }
│       (Internal — dispatched by application layer when a session exhausts context for the first time
│        and has no ChainId; creates the chain retroactively)
│       -> Calls SessionChain.Create(); publishes SessionChainCreatedEvent.
│          Updates AgentSession.ChainId = new chain ID.
│
├── CompleteSessionChainCommand      { ChainId }
│       -> Loads the chain; calls chain.Complete(); publishes SessionChainCompletedEvent.
│          Only valid when the current session's status is Idle or Completed.
│
├── CancelSessionChainCommand        { ChainId }
│       -> Loads the chain; calls chain.Cancel(); publishes SessionChainCancelledEvent.
│          Application layer dispatches CancelAgentSessionCommand for all non-terminal sessions
│          in the chain.
│
├── StartWorkQueueCommand            { ProjectId, TaskIds: IReadOnlyList<TaskId>,
│                                       ExecutionMode, BudgetPerSessionUsd: decimal,
│                                       TotalBudgetCapUsd?: decimal,
│                                       RequiresVerificationGate: bool,
│                                       MaxParallelSessions?: int,
│                                       TemplateId? }
│       -> Creates a WorkQueue with WorkItems for each task, then calls queue.Start().
│          For Parallel mode: creates up to MaxParallelSessions agent sessions immediately.
│          For Sequential mode: creates only the first agent session and holds others Queued.
│          Cross-context: Projects context creates worktrees in response to session start events.
│
├── PauseWorkQueueCommand            { QueueId }
│       -> Calls queue.Pause(); running sessions are paused via PauseAgentSessionCommand
│
├── ResumeWorkQueueCommand           { QueueId }
│       -> Calls queue.Resume(); re-dispatches ready items to sessions
│
├── CancelWorkQueueCommand           { QueueId }
│       -> Calls queue.Cancel(); cancels all non-terminal sessions in the queue
│
├── ReorderWorkQueueCommand          { QueueId, OrderedItemIds: IReadOnlyList<WorkItemId> }
│       -> Calls queue.ReorderItems(); only valid when queue is still Pending
│
├── CreateAgentTemplateCommand       { Name, ModelId, SystemPrompt?, AllowedTools,
│                                       ClassificationRules?, Schedule?, DefaultBudgetCapUsd,
│                                       CustomToolDefinitions?: IReadOnlyList<McpToolDefinition> }
│       -> Creates AgentTemplate; publishes AgentTemplateCreatedEvent
│
├── UpdateAgentTemplateCommand       { TemplateId, Name?, SystemPrompt?,
│                                       AllowedTools?, DefaultBudgetCapUsd? }
│       -> Updates template fields; publishes AgentTemplateUpdatedEvent
│
├── AddTemplateClassificationRuleCommand  { TemplateId, MatchField, MatchPattern,
│                                            ClassifyAs, MinPriority? }
│       -> Calls template.AddClassificationRule(); publishes AgentTemplateUpdatedEvent
│
├── RemoveTemplateClassificationRuleCommand { TemplateId, RuleId }
│       -> Calls template.RemoveClassificationRule(); publishes AgentTemplateUpdatedEvent
│
├── AddTemplateMcpToolCommand        { TemplateId, Name, Description, InputSchema, ServerUri }
│       -> Calls template.AddCustomToolDefinition(); publishes AgentTemplateUpdatedEvent
│
├── RemoveTemplateMcpToolCommand     { TemplateId, ToolName }
│       -> Calls template.RemoveCustomToolDefinition(); publishes AgentTemplateUpdatedEvent
│
├── DeactivateAgentTemplateCommand   { TemplateId }
│       -> Calls template.Deactivate(); publishes AgentTemplateDeactivatedEvent
│
├── UpdateSessionMetricsCommand      { SessionId, SubAgentId?: SubAgentId, ModelId: string,
│                                       CostUsd: decimal, TokenUsage: TokenUsage,
│                                       ContextWindow: ContextWindowSnapshot }
│       (Replaces RecordSessionBudgetSpendCommand for the usage_update ACL mapping.
│        Called by the streaming infrastructure as each usage_update event arrives.
│        Calls session.UpdateMetrics() which:
│          1. Aggregates CostUsd into Metrics.TotalCostUsd (and Budget.EstimatedCostUsd)
│          2. Aggregates TokenUsage into Metrics.TokenUsageByModel[modelId]
│          3. REPLACES Metrics.ContextWindow with the new snapshot (NOT additive)
│          4. When SubAgentId is set: updates Metrics.SubAgentMetrics[subAgentId] and
│             also aggregates CostUsd and TokenUsage into the parent session's totals
│        May publish SessionBudgetWarningEvent or SessionBudgetExhaustedEvent + auto-pause.)
│
├── RecordAgentMessageCommand        { SessionId, Source: AgentMessageSource,
│                                       SubAgentId?, Content: string }
│       (Dispatched by IAgentRuntimeEventConsumer ACL when translating a raw SDK message.
│        Constructs an AgentMessage entity and attaches it to the correct owner:
│        parent AgentSession when Source != SubAgent; SubAgentSession when Source == SubAgent.)
│
├── RecordToolCallCommand            { SessionId, ToolName: string, Input: string,
│                                       SubAgentId? }
│       (Dispatched by ACL when a tool_use block is observed. Creates a ToolCall entity
│        with Status = Pending. Raises ToolCallStartedEvent.)
│
├── RecordToolCallResultCommand      { SessionId, ToolCallId, Result: string, IsError: bool,
│                                       SubAgentId? }
│       (Dispatched by ACL when a tool_result arrives. Attaches ToolResult to the matching
│        ToolCall entity and transitions its Status to Success or Error.
│        Raises ToolCallCompletedEvent. Never creates a standalone AgentMessage.)
│
├── RecordSessionOutputCommand       { SessionId, OutputLineCount: int, FilesChanged: int,
│                                       TestsAdded: int, AllTestsPassing: bool, CommitSha?: string }
│       (Called by the agent runtime when it signals completion.
│        Calls session.RecordOutput(); publishes AgentSessionOutputReadyEvent.
│        Session transitions Running -> Idle; HasPendingReview = true.
│        For queue items with RequiresVerificationGate: marks WorkItem.VerificationPassed = true.)
│
├── EnqueueSessionMessageCommand     { SessionId, Content: string,
│                                       Priority: MessagePriority, Source: MessageSource }
│       -> Loads or creates the SessionMessageQueue for the session.
│          Calls queue.Enqueue(); publishes MessageEnqueuedEvent.
│          For Immediate priority: application layer calls IAgentRuntime.SendSteeringMessageAsync()
│          immediately if the session is Running or Idle (about to resume); otherwise the message
│          stays Pending until the session transitions to Running.
│          For Queued priority: message is held until the Node.js sidecar requests the next
│          pending message via the command stream polling loop.
│          NOTE: Enqueue is only valid for sessions in non-terminal, non-ContextExhausted statuses.
│
├── CancelQueuedMessageCommand       { SessionId, MessageId: Guid }
│       -> Loads the SessionMessageQueue; calls queue.Cancel(messageId);
│          publishes MessageCancelledEvent.
│          Returns a domain error if the message is already Delivered.
│
└── PromoteMessageToImmediateCommand { SessionId, MessageId: Guid }
        -> Loads the SessionMessageQueue; calls queue.Promote(messageId);
           publishes MessagePromotedToImmediateEvent.
           Application layer calls IAgentRuntime.SendSteeringMessageAsync() if session is Running.

Agent API Commands (authenticated with per-session AgentApiKey, not user JWT):
├── AgentCreateTaskCommand           { SessionId, Title, Description?, Priority,
│                                       Tags?: IReadOnlyList<string>,
│                                       LinkedTaskId?, ProjectId }
│       -> Validates session is Running; creates a Task via Task context (cross-context coordination);
│          raises AgentApiTaskCreatedEvent with the new TaskId linked back to the session.
│          Returns the created TaskId.
│
└── AgentLogDecisionCommand          { SessionId, Content: string }
        -> Records a SessionLogChunk tagged as "decision" for audit trail.
           Content must be 1-2000 chars. Does not change session status.

Queries:
├── GetAgentSessionQuery             { SessionId } -> AgentSessionDto
│       (includes Budget, Metrics, Output if set, WorktreeRef, ChainId if part of a chain)
│
├── ListAgentSessionsQuery           { ProjectId?, Status?, Page, PageSize }
│                                         -> PagedResult<AgentSessionSummaryDto>
│       (summary includes: name, status, hasPendingReview, taskId, totalCostUsd,
│        contextWindowUsagePercent, turnCount, elapsed time)
│
├── GetSessionOutputStreamQuery      { SessionId } -> IAsyncEnumerable<SessionLogChunk>
│       (returns all buffered log chunks in sequence order; used to replay logs for review)
│
├── GetSessionMessagesQuery          { SessionId, SubAgentId? } -> IReadOnlyList<AgentMessageDto>
│       (returns messages for the session; when SubAgentId is provided, returns that subagent's
│        messages only; when null, returns only parent-session messages, never subagent messages)
│
├── GetSessionToolCallsQuery         { SessionId, SubAgentId? } -> IReadOnlyList<ToolCallDto>
│       (returns tool calls with their results; SubAgentId scopes to a specific subagent)
│
├── GetSessionMetricsQuery           { SessionId } -> SessionMetricsDto
│       (returns full metrics: TotalCostUsd, TokenUsageByModel per model, ContextWindow snapshot,
│        SubAgentMetrics per subagent with their own context windows, TurnCount, LastTurnAt;
│        used by the metrics detail panel in the UI)
│
├── GetSessionChainQuery             { ChainId } -> SessionChainDto
│       (returns full chain: all ChainedSessions in order, all HandoffDocuments,
│        CurrentSessionId, Status, OriginalObjective, TotalCostUsd aggregated across sessions)
│
├── ListSessionChainsQuery           { OwnerId, Status?: SessionChainStatus, Page, PageSize }
│                                         -> PagedResult<SessionChainSummaryDto>
│       (summary includes: chainId, originalObjective, status, sessionCount, totalCostUsd, createdAt)
│
├── GetWorkQueueQuery                { QueueId } -> WorkQueueDto
│       (includes Items with their statuses and assigned session IDs)
│
├── ListWorkQueuesQuery              { ProjectId?, Status?, Page, PageSize }
│                                         -> PagedResult<WorkQueueSummaryDto>
│
├── GetAgentTemplateQuery            { TemplateId } -> AgentTemplateDto
│
├── ListAgentTemplatesQuery          { IsActive?: bool, Page, PageSize }
│                                         -> PagedResult<AgentTemplateSummaryDto>
│
├── GetSessionHistoryQuery           { ProjectId?, DateFrom?, DateTo?, Page, PageSize }
│                                         -> PagedResult<AgentSessionHistoryDto>
│       (includes: session name, task title (from Task context), status, cost, duration, outcome)
│
├── GetSessionMessageQueueQuery      { SessionId }
│                                         -> IReadOnlyList<SessionMessageDto>
│       (returns all Pending messages in delivery order: Immediate first, then Queued by CreatedAt;
│        includes messageId, content preview, priority, status, createdAt for each message;
│        used by the UI to render the message queue panel on the session detail page)
│
└── GetSessionPoolUtilizationQuery   { }
                                          -> SessionPoolUtilizationDto
        (returns: ActiveCount, MaxCount, AvailableSlots, Percentage;
         used by the UI to show the pool utilization indicator in the session list header)
```

---

## 8.8 Repository Interface

```csharp
/// <summary>
/// Repository for AgentSession aggregate. Handles persistence and streaming log storage.
/// </summary>
public interface IAgentSessionRepository
{
    /// <summary>Loads a session by ID. Returns null if not found.</summary>
    Task<AgentSession?> GetByIdAsync(AgentSessionId id, CancellationToken ct);

    /// <summary>
    /// Lists sessions for the owning user, with optional filters.
    /// Results are ordered by CreatedAt descending.
    /// </summary>
    Task<PagedResult<AgentSession>> ListAsync(
        UserId ownerId,
        ProjectId? projectId,
        AgentSessionStatus? status,
        int page, int pageSize,
        CancellationToken ct);

    /// <summary>Persists a new session.</summary>
    Task AddAsync(AgentSession session, CancellationToken ct);

    /// <summary>Updates an existing session (status changes, budget updates, metrics, output).</summary>
    Task UpdateAsync(AgentSession session, CancellationToken ct);

    /// <summary>
    /// Appends a log chunk to the session's streaming output buffer.
    /// Chunks are stored in sequence order and are never modified after write.
    /// </summary>
    Task AppendLogChunkAsync(AgentSessionId sessionId, SessionLogChunk chunk, CancellationToken ct);

    /// <summary>
    /// Returns all log chunks for a session in sequence order.
    /// Used for log replay and audit trail display.
    /// </summary>
    Task<IReadOnlyList<SessionLogChunk>> GetLogChunksAsync(AgentSessionId sessionId, CancellationToken ct);

    /// <summary>
    /// Appends a classified AgentMessage to the session or its SubAgentSession.
    /// When subAgentId is provided, the message is attached to that SubAgentSession entity.
    /// </summary>
    Task AppendMessageAsync(AgentSessionId sessionId, AgentMessage message, CancellationToken ct);

    /// <summary>
    /// Returns messages for the session. When subAgentId is null, returns parent-session messages only.
    /// When subAgentId is provided, returns messages for that subagent only.
    /// </summary>
    Task<IReadOnlyList<AgentMessage>> GetMessagesAsync(
        AgentSessionId sessionId,
        SubAgentId? subAgentId,
        CancellationToken ct);

    /// <summary>
    /// Persists a new ToolCall entity in Pending status.
    /// </summary>
    Task AddToolCallAsync(AgentSessionId sessionId, ToolCall toolCall, CancellationToken ct);

    /// <summary>
    /// Updates a ToolCall with its result (transitions from Pending to Success or Error).
    /// </summary>
    Task UpdateToolCallAsync(AgentSessionId sessionId, ToolCall toolCall, CancellationToken ct);

    /// <summary>
    /// Returns tool calls for the session. When subAgentId is null, returns parent-session tool calls only.
    /// </summary>
    Task<IReadOnlyList<ToolCall>> GetToolCallsAsync(
        AgentSessionId sessionId,
        SubAgentId? subAgentId,
        CancellationToken ct);

    /// <summary>
    /// Returns sessions in the given history window, for all projects owned by the user.
    /// </summary>
    Task<PagedResult<AgentSession>> GetHistoryAsync(
        UserId ownerId,
        ProjectId? projectId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page, int pageSize,
        CancellationToken ct);

    /// <summary>
    /// Returns the count of sessions currently in Running or Paused status for the given user.
    /// Used by the SessionPool domain service to compute current utilization.
    /// </summary>
    Task<int> CountActiveSessionsAsync(UserId ownerId, CancellationToken ct);
}

/// <summary>
/// Repository for SessionChain aggregate. Includes ChainedSessions and HandoffDocuments
/// as part of the aggregate.
/// </summary>
public interface ISessionChainRepository
{
    /// <summary>Loads a chain by ID, including all sessions and handoff documents. Returns null if not found.</summary>
    Task<SessionChain?> GetByIdAsync(SessionChainId id, CancellationToken ct);

    /// <summary>
    /// Loads the chain that contains the given session ID.
    /// Used by the handoff workflow to locate the chain when an exhausted session's ChainId is set.
    /// Returns null if the session is not part of any chain.
    /// </summary>
    Task<SessionChain?> GetBySessionIdAsync(AgentSessionId sessionId, CancellationToken ct);

    /// <summary>
    /// Lists chains for the owning user with optional status filter.
    /// Results are ordered by CreatedAt descending.
    /// </summary>
    Task<PagedResult<SessionChain>> ListAsync(
        UserId ownerId,
        SessionChainStatus? status,
        int page, int pageSize,
        CancellationToken ct);

    /// <summary>Persists a new session chain.</summary>
    Task AddAsync(SessionChain chain, CancellationToken ct);

    /// <summary>
    /// Updates an existing chain (new session attached, handoff document added, status change).
    /// </summary>
    Task UpdateAsync(SessionChain chain, CancellationToken ct);
}

/// <summary>
/// Repository for WorkQueue aggregate. Includes WorkItems as part of the aggregate.
/// </summary>
public interface IWorkQueueRepository
{
    /// <summary>Loads a queue with all its work items. Returns null if not found.</summary>
    Task<WorkQueue?> GetByIdAsync(WorkQueueId id, CancellationToken ct);

    /// <summary>Lists queues for the owning user with optional project and status filters.</summary>
    Task<PagedResult<WorkQueue>> ListAsync(
        UserId ownerId,
        ProjectId? projectId,
        WorkQueueStatus? status,
        int page, int pageSize,
        CancellationToken ct);

    /// <summary>Persists a new work queue and all its initial work items.</summary>
    Task AddAsync(WorkQueue queue, CancellationToken ct);

    /// <summary>
    /// Updates an existing work queue and its items (status changes, reordering, item completion).
    /// </summary>
    Task UpdateAsync(WorkQueue queue, CancellationToken ct);
}

/// <summary>
/// Repository for AgentTemplate aggregate.
/// </summary>
public interface IAgentTemplateRepository
{
    /// <summary>Loads a template by ID. Returns null if not found.</summary>
    Task<AgentTemplate?> GetByIdAsync(AgentTemplateId id, CancellationToken ct);

    /// <summary>Lists templates for the owning user, with optional active filter.</summary>
    Task<PagedResult<AgentTemplate>> ListAsync(
        UserId ownerId,
        bool? isActive,
        int page, int pageSize,
        CancellationToken ct);

    /// <summary>Persists a new template.</summary>
    Task AddAsync(AgentTemplate template, CancellationToken ct);

    /// <summary>Updates an existing template (rules, schedule, deactivation, MCP tools).</summary>
    Task UpdateAsync(AgentTemplate template, CancellationToken ct);
}

/// <summary>
/// Repository for SessionMessageQueue aggregate.
/// The queue is keyed by AgentSessionId (1:1 with AgentSession).
/// </summary>
public interface ISessionMessageQueueRepository
{
    /// <summary>
    /// Loads the message queue for the given session.
    /// Returns an empty queue if none exists yet (queue is created on first Enqueue).
    /// </summary>
    Task<SessionMessageQueue> GetBySessionIdAsync(AgentSessionId sessionId, CancellationToken ct);

    /// <summary>
    /// Persists the full message queue state (adds new messages, updates delivered/cancelled status).
    /// Uses upsert semantics — creates the queue row if this is the first write for the session.
    /// </summary>
    Task SaveAsync(SessionMessageQueue queue, CancellationToken ct);

    /// <summary>
    /// Returns all Pending messages for the session, ordered for delivery:
    /// Immediate first, then Queued by CreatedAt ascending.
    /// Used by the application layer to render the UI queue panel.
    /// </summary>
    Task<IReadOnlyList<SessionMessage>> GetPendingAsync(AgentSessionId sessionId, CancellationToken ct);
}
```

---

## 8.9 API Endpoints

### Human-Facing Endpoints (User JWT required)

```
GET    /api/agents/sessions                          List sessions [Auth]
POST   /api/agents/sessions                          Start a new standalone session [Auth]
GET    /api/agents/sessions/{id}                     Get session detail [Auth]
POST   /api/agents/sessions/{id}/interrupt           Interrupt a running session [Auth]
POST   /api/agents/sessions/{id}/pause               Pause a running session [Auth]
POST   /api/agents/sessions/{id}/resume              Resume a paused session [Auth]
POST   /api/agents/sessions/{id}/cancel              Cancel a session [Auth]
POST   /api/agents/sessions/{id}/approve             Approve session output (Idle + HasPendingReview) [Auth]
POST   /api/agents/sessions/{id}/reject              Reject session output (Idle + HasPendingReview) [Auth]
POST   /api/agents/sessions/{id}/close               Explicitly close an idle session (no pending review) [Auth]
POST   /api/agents/sessions/{id}/retry               Retry a failed session [Auth]
GET    /api/agents/sessions/{id}/logs                Get buffered log chunks for replay [Auth]
GET    /api/agents/sessions/{id}/messages            Get classified messages for a session [Auth]
GET    /api/agents/sessions/{id}/tool-calls          Get tool calls with results for a session [Auth]
GET    /api/agents/sessions/{id}/metrics             Get detailed metrics (per-model tokens, context window,
                                                      subagent breakdowns) [Auth]
GET    /api/agents/sessions/history                  Session history with date range filters [Auth]

POST   /api/agents/sessions/{id}/messages/queue      Enqueue a steering or follow-up message [Auth]
GET    /api/agents/sessions/{id}/messages/queue      View pending message queue for a session [Auth]
DELETE /api/agents/sessions/{id}/messages/queue/{messageId}
                                                     Cancel a pending queued message [Auth]
PATCH  /api/agents/sessions/{id}/messages/queue/{messageId}/promote
                                                     Promote a queued message to immediate [Auth]

GET    /api/agents/pool                              Get session pool utilization [Auth]

POST   /api/agents/queues                            Create and start a work queue [Auth]
GET    /api/agents/queues                            List queues [Auth]
GET    /api/agents/queues/{id}                       Get queue detail with items [Auth]
POST   /api/agents/queues/{id}/pause                 Pause a queue [Auth]
POST   /api/agents/queues/{id}/resume                Resume a paused queue [Auth]
POST   /api/agents/queues/{id}/cancel                Cancel a queue [Auth]
PUT    /api/agents/queues/{id}/order                 Reorder queue items (Pending queues only) [Auth]
POST   /api/agents/queues/{id}/approve-all           Approve all Idle+HasPendingReview sessions in queue [Auth]

GET    /api/agents/chains                            List session chains [Auth]
GET    /api/agents/chains/{id}                       Get chain detail with sessions and handoff documents [Auth]
POST   /api/agents/chains/{id}/complete              Complete a chain (current session must be Idle or Completed) [Auth]
POST   /api/agents/chains/{id}/cancel                Cancel a chain and all non-terminal sessions [Auth]

GET    /api/agents/templates                         List templates [Auth]
POST   /api/agents/templates                         Create a template [Auth]
GET    /api/agents/templates/{id}                    Get template detail [Auth]
PUT    /api/agents/templates/{id}                    Update a template [Auth]
POST   /api/agents/templates/{id}/rules              Add a classification rule [Auth]
DELETE /api/agents/templates/{id}/rules/{ruleId}     Remove a classification rule [Auth]
POST   /api/agents/templates/{id}/tools              Add an MCP custom tool definition [Auth]
DELETE /api/agents/templates/{id}/tools/{toolName}   Remove an MCP custom tool definition [Auth]
POST   /api/agents/templates/{id}/deactivate         Deactivate a template [Auth]
POST   /api/agents/templates/{id}/activate           Activate a template [Auth]
```

### Agent API Endpoints (Per-session AgentApiKey required, not user JWT)

```
POST   /api/agent/tasks                              Create a follow-up task (AG-012)
POST   /api/agent/sessions/{id}/budget               Report token spend (incremental updates)
POST   /api/agent/sessions/{id}/output               Signal session completion with output summary
POST   /api/agent/sessions/{id}/log                  Append a decision log entry (AG-013)
```

### Real-Time Streaming

```
WS     /ws/agents/sessions/{id}/stream               WebSocket / SignalR hub for live log streaming
```

---

## 8.10 Application Layer Coordination

The Agent Sessions context coordinates with Task, Projects, and Notification contexts at the application layer. Contexts never call each other directly — coordination is handled by command handlers responding to domain events.

| Trigger | Agent Sessions | Task Context | Projects Context | Notification Context |
|---------|---------------|--------------|-----------------|---------------------|
| **Start session** | `AgentSession.Start()` -> `AgentSessionStartedEvent` | — | Creates worktree if `WorktreeRef` is set | — |
| **Session output approved** | `AgentSession.ApproveOutput()` -> `AgentSessionApprovedEvent` | `task.Complete()` (if TaskId is set) | Merges worktree branch; cleans up worktree | Sends completion notification |
| **Session closed (no review)** | `AgentSession.Close()` -> `AgentSessionClosedEvent` | — | — | — |
| **Session failed** | `AgentSession.RecordFailure()` -> `AgentSessionFailedEvent` | — | — | Sends failure notification via `NotificationContext` |
| **Budget exhausted** | `session.RequestPause()` -> `SessionBudgetExhaustedEvent` | — | — | Sends budget alert notification |
| **Context nearing limit** | `session.RecordContextNearingLimit()` -> `ContextNearingLimitEvent` | — | — | — (UI notified via SSE) |
| **Context exhausted** | `session.RecordContextExhausted()` -> `ContextExhaustedEvent` | — | — | Sends context exhausted notification |
| **Handoff document received** | `chain.InitiateHandoff()` -> `SessionHandoffInitiatedEvent` | — | — | — |
| **New session attached to chain** | `chain.AttachSession()` -> `SessionAddedToChainEvent` | — | Creates new worktree if session requires one | — |
| **Chain completed** | `chain.Complete()` -> `SessionChainCompletedEvent` | — | — | Sends chain completion notification |
| **Queue item approved (Sequential)** | `queue.AdvanceQueue()` -> `WorkQueueAdvancedEvent` | — | Creates next worktree if required | — |
| **Queue completed** | `WorkQueueCompletedEvent` | — | — | Sends queue completion notification |
| **Agent creates task via API** | `AgentApiTaskCreatedEvent` raised | `CreateTaskCommand` dispatched with agent-supplied fields | — | — |
| **Immediate message enqueued** | `MessageEnqueuedEvent` (Immediate) | — | — | — |
| **Pool slot freed** | `SessionReleasedEvent` | — | — | — |
| **Interrupt requested** | `SessionInterruptRequestedEvent` | — | — | — |
| **Pause requested** | `SessionPauseRequestedEvent` | — | — | — |
| **Resume requested** | `SessionResumeRequestedEvent` | — | — | — |
| **Cancel requested** | `SessionCancelRequestedEvent` | — | — | — |

**Lifecycle command flow** (two-phase pattern):

Every lifecycle action (interrupt, pause, resume, cancel) follows the same pattern:
1. User calls the API endpoint (e.g., `POST /api/agents/sessions/{id}/pause`)
2. .NET API handler dispatches domain command (e.g., `PauseAgentSessionCommand`)
3. Command handler calls the aggregate method (e.g., `session.RequestPause()`)
4. Aggregate transitions to intermediate state (e.g., `Pausing`) and raises REQUEST event
5. Application layer writes the infrastructure command to Redis (`pause` on command stream)
6. Node.js sidecar executes the command and writes acknowledgement to event stream (`pause_ack`)
7. `IAgentRuntimeEventConsumer` ACL reads the acknowledgement and dispatches CONFIRM command
8. CONFIRM command handler calls the aggregate confirm method (e.g., `session.ConfirmPaused()`)
9. Aggregate transitions to final state (e.g., `Paused`) and raises CONFIRMED event
10. UI receives the final state update via SSE/WebSocket

**Context exhaustion and handoff flow** (application layer):

When a session's context window is full, the sidecar emits events in sequence and the ACL translates them:

1. Sidecar emits `context_nearing_limit` at ~85% usage → ACL dispatches `RecordContextNearingLimitCommand` → UI shows warning
2. Sidecar emits `context_exhausted` (context window full) → ACL dispatches `RecordContextExhaustedCommand` → session transitions to `ContextExhausted`; `ContextExhaustedEvent` raised
3. Sidecar emits `handoff_document` (written as last action per system prompt instruction) → ACL dispatches `RecordHandoffDocumentCommand`
4. If session has no `ChainId`: application layer dispatches `CreateSessionChainCommand` to create the chain retroactively; updates session's `ChainId`
5. `RecordHandoffDocumentCommand` handler loads the chain and calls `chain.InitiateHandoff()` → `SessionHandoffInitiatedEvent` raised
6. Application layer dispatches `StartAgentSessionCommand` with `ChainId` set and the handoff content as the session's objective context
7. New session starts → `AgentSessionStartedEvent` raised → `chain.AttachSession(newSessionId)` called → `SessionAddedToChainEvent` raised
8. New session proceeds normally; old session remains in `ContextExhausted` as a permanent record

**Steering message delivery flow** (application layer, not domain):

When `EnqueueSessionMessageCommand` is dispatched with `Priority = Immediate` and the session's current status is `Running`:

1. The command handler calls `queue.Enqueue()` to record the message in the domain aggregate.
2. The command handler then calls `IAgentRuntime.SendSteeringMessageAsync(handle, content, Immediate)`.
3. The infrastructure layer writes a `steering_message` command to the Redis command stream (`agent:session:{sessionId}:commands`).
4. The Node.js sidecar reads the command and calls `query.interrupt()` then injects the message as the next input.
5. When the sidecar acknowledges delivery, the infrastructure layer dispatches `MarkDeliveredInternalCommand` which calls `queue.MarkDelivered(messageId)` and raises `MessageDeliveredEvent`.

If the session is `Idle`, `Interrupted`, `Paused`, or `Queued` when an Immediate message is enqueued, the message remains `Pending` until the session transitions to `Running`, at which point the application layer delivers all pending Immediate messages before the next turn begins.

---

## 8.11 Anti-Corruption Layer (IAgentRuntime)

The Agent Sessions context does not call Claude Code or the Claude Agent SDK directly. It delegates to two ACL ports:

- **`IAgentRuntime`** — controls the lifecycle of Node.js sidecar processes (start, stop, pause, resume, steering). Implemented in the infrastructure layer using `child_process` spawn or equivalent. Returns an `AgentProcessHandle` that identifies the running process.
- **`IAgentRuntimeEventConsumer`** — a background service that reads raw SDK events from Redis Streams, translates them via the ACL mappings defined in section 8.3, and dispatches domain commands to the application layer. This includes translating sidecar acknowledgement events (`interrupt_ack`, `pause_ack`, `resume_ack`, `stop_ack`) into the corresponding CONFIRM commands that complete lifecycle state transitions, and translating `context_nearing_limit`, `context_exhausted`, and `handoff_document` events into the handoff workflow commands.

This separation keeps the domain clean of SDK-specific types. The Node.js sidecar pattern (see section 8.2) means `.NET` never imports the TypeScript Agent SDK directly — all SDK interaction happens in the Node.js process and is mediated through Redis Streams.

```csharp
/// <summary>
/// Anti-corruption layer for the Claude Agent SDK.
/// Controls the lifecycle of Node.js sidecar processes — one per agent session.
/// Translates between process lifecycle operations and domain session concepts.
/// </summary>
public interface IAgentRuntime
{
    /// <summary>
    /// Starts a new Node.js sidecar process for the given session configuration.
    /// The process connects to the Claude Agent SDK and begins streaming raw events
    /// to Redis Streams under key agent:session:{sessionId}.
    /// Returns a handle identifying the running process for subsequent control operations.
    /// </summary>
    Task<Result<AgentProcessHandle, DomainError>> StartSessionAsync(
        AgentSessionConfig config,
        CancellationToken ct);

    /// <summary>
    /// Terminates the Node.js sidecar process (SIGTERM + cleanup).
    /// Writes `stop` to the command stream; sidecar responds with `stop_ack`.
    /// Does not transition domain session status — the ACL dispatches
    /// ConfirmSessionCancelledCommand when stop_ack is received.
    /// </summary>
    Task<Result<DomainError>> StopSessionAsync(AgentProcessHandle handle, CancellationToken ct);

    /// <summary>
    /// Requests suspension of the Node.js sidecar process.
    /// Writes `pause` to the command stream; sidecar responds with `pause_ack`.
    /// Does not transition domain session status — the ACL dispatches
    /// ConfirmSessionPausedCommand when pause_ack is received.
    /// </summary>
    Task<Result<DomainError>> PauseSessionAsync(AgentProcessHandle handle, CancellationToken ct);

    /// <summary>
    /// Requests resumption of a suspended Node.js sidecar process.
    /// Writes `resume` to the command stream; sidecar responds with `resume_ack`.
    /// Does not transition domain session status — the ACL dispatches
    /// ConfirmSessionResumedCommand when resume_ack is received.
    /// </summary>
    Task<Result<DomainError>> ResumeSessionAsync(AgentProcessHandle handle, CancellationToken ct);

    /// <summary>
    /// Injects a steering message into a running agent session.
    /// For Immediate priority: writes a steering_message command to the Redis command stream
    /// (agent:session:{sessionId}:commands). The Node.js sidecar reads the command and calls
    /// query.interrupt() followed by session.send(content) (V2 SDK) or injects via
    /// UserPromptSubmit hook (V1 SDK fallback).
    /// For Queued priority: writes the message to the command stream with a deferred flag;
    /// the sidecar enqueues it internally and delivers it after the current SDK turn completes.
    /// Does not transition domain session status — callers must observe MessageDeliveredEvent.
    /// </summary>
    Task<Result<DomainError>> SendSteeringMessageAsync(
        AgentProcessHandle handle,
        string content,
        MessagePriority priority,
        CancellationToken ct);
}

/// <summary>
/// Configuration passed to IAgentRuntime.StartSessionAsync.
/// Carries everything the Node.js sidecar needs to launch a Claude Agent SDK session.
/// </summary>
public sealed record AgentSessionConfig(
    AgentSessionId SessionId,
    string ApiKey,
    string ModelId,
    string? SystemPrompt,
    IReadOnlyList<string> AllowedTools,
    WorktreeRef? WorktreeRef,
    string Objective,
    IReadOnlyList<McpToolDefinition> CustomTools  // Empty list if no custom MCP tools for this session
);

/// <summary>
/// Opaque handle returned by IAgentRuntime.StartSessionAsync.
/// Identifies the running Node.js process for pause/resume/stop/steering operations.
/// </summary>
public sealed record AgentProcessHandle(string RuntimeJobId);
```

---

## 8.12 Session Pool Domain Service

The `SessionPool` domain service enforces the global concurrency cap and provides utilization metrics. It is a stateless service that delegates state reads to `IAgentSessionRepository.CountActiveSessionsAsync()` and state changes to events. It is not an aggregate — it has no persistent state of its own.

```csharp
/// <summary>
/// Domain service that manages the session concurrency pool.
/// Enforces the global maximum concurrent sessions cap (default: 20, configurable via
/// SessionPoolConfig). Allocation and release are published as domain events for
/// UI utilization display and audit logging.
/// </summary>
public interface ISessionPool
{
    /// <summary>
    /// Returns true if a new session can be allocated without exceeding the pool cap.
    /// Reads the current active session count from IAgentSessionRepository.
    /// </summary>
    Task<bool> CanAllocateAsync(UserId ownerId, CancellationToken ct);

    /// <summary>
    /// Allocates a slot for the given session.
    /// Returns SessionAllocatedEvent on success.
    /// Returns PoolExhaustedError if the pool is at capacity (caller should raise
    /// SessionPoolExhaustedEvent and leave the session in Queued status).
    /// </summary>
    Task<Result<SessionAllocatedEvent, DomainError>> AllocateAsync(
        UserId ownerId,
        AgentSessionId sessionId,
        CancellationToken ct);

    /// <summary>
    /// Releases the slot held by the given session (called on Completed, Failed, or Cancelled).
    /// Returns SessionReleasedEvent with updated pool utilization.
    /// </summary>
    Task<SessionReleasedEvent> ReleaseAsync(
        UserId ownerId,
        AgentSessionId sessionId,
        CancellationToken ct);

    /// <summary>
    /// Returns the current pool utilization for the UI utilization indicator.
    /// </summary>
    Task<SessionPoolUtilization> GetUtilizationAsync(UserId ownerId, CancellationToken ct);
}

/// <summary>
/// Pool utilization snapshot. Returned by ISessionPool.GetUtilizationAsync and
/// carried by SessionAllocatedEvent / SessionReleasedEvent.
/// </summary>
public sealed record SessionPoolUtilization(
    int ActiveCount,
    int MaxCount,
    int AvailableSlots,
    decimal UtilizationPercent
);
```

---

## Design Notes

| Item | Type | Detail |
|------|------|--------|
| AG-014 | Partial coverage | AG-014 (agents add people/comms learnings via API) has no scenario coverage and the People and Comms contexts are not yet designed. The `AgentLogDecisionCommand` provides a general log-entry mechanism for now. A dedicated `POST /api/agent/learnings` endpoint should be added once the Comms and People bounded contexts are designed. |
| AG-017 | Architecture decision | AG-017 (Claude Agent SDK integration) is absorbed by the Event-Sourced Sidecar Pattern (section 8.2) and the `IAgentRuntime` / `IAgentRuntimeEventConsumer` ACL ports (section 8.11). The domain is SDK-agnostic by design. SDK-specific wiring (Redis Stream consumption, Node.js process management, raw event parsing) belongs entirely in the infrastructure layer. |
| WorktreeRef | Migration pending | The `WorktreeRef` value object currently stores BranchName + LocalPath strings. The Projects context (`docs/domain/contexts/projects.md`) now exposes `WorktreeId` (Guid wrapper). In a future revision, `AgentSession.WorktreeRef` should be updated to carry `WorktreeId` from the Projects context so the association is a typed cross-context reference rather than a structural copy of path data. |
| Real-time streaming | Infrastructure note | `RecordLogChunk` appends to a persistent buffer and does not raise a domain event. Live streaming to the UI is handled by SignalR at the infrastructure layer — the hub pushes chunks as they arrive from the Redis Stream. The repository's `AppendLogChunkAsync` provides durable storage for log replay. |
| Scheduled automation | Deferred | The `AgentSchedule` value object on `AgentTemplate` defines when a scheduled template runs, but the scheduler infrastructure (e.g., a Quartz.NET job or Hangfire trigger) is an infrastructure concern. The domain event `AgentSessionStartedEvent` is the sole trigger — the scheduler dispatches a `StartAgentSessionCommand` on the configured cron schedule. |
| SubAgentSession persistence | Infrastructure note | `SubAgentSession` is a child entity of `AgentSession`. Its messages and tool calls are stored alongside the parent session in the repository. The `IAgentSessionRepository` methods accept an optional `SubAgentId` parameter to scope reads to a specific subagent, keeping the repository interface clean without requiring a separate repository for subagents. |
| Steering message V2 SDK | Open question | The V2 SDK (`unstable_v2_*`) has a clean `session.send(content)` method for injecting messages into a running session. The V1 SDK requires using `query.interrupt()` + re-invocation or the `UserPromptSubmit` hook. The `IAgentRuntime.SendSteeringMessageAsync()` interface is SDK-version-agnostic; the infrastructure implementation chooses the mechanism. When V2 stabilises, the infrastructure layer should migrate to `session.send()` exclusively. Until then, V1 hook injection is the fallback. |
| Process handle persistence | Open question | `AgentProcessHandle` is an in-memory opaque identifier for a running Node.js process. On .NET backend restart, the handles are lost. The infrastructure layer must store `RuntimeJobId` in the database alongside the `AgentSession` record so that steering commands and stop signals can be re-associated with the correct sidecar process after a restart. This is an infrastructure concern but should be noted in the implementation spec. |
| SessionPool config storage | Open question | `SessionPoolConfig.MaxConcurrentSessions` is described as user-level configuration. The domain design assumes it is readable by the `ISessionPool` service, but the storage location (user preferences table, environment config, or a dedicated settings record) is unresolved. This should be addressed in the implementation checkpoint. |
| MCP tool schema validation | Open question | `McpToolDefinition.InputSchema` is stored as a JSON string. The application layer should validate it against JSON Schema meta-schema on input (in the command handler), but the domain only enforces non-emptiness and valid JSON. The infrastructure layer passes the schema verbatim to the Node.js sidecar, which forwards it to the MCP server. Stricter validation (e.g., ensuring the schema references only supported input types) may be needed once MCP tool usage patterns are established. |
| Message queue delivery on session resume | Open question | When a Paused session resumes, the application layer must drain all Pending Immediate messages from the `SessionMessageQueue` before the session resumes normal execution. The ordering guarantee (all Immediate messages before any Queued messages) is enforced by `GetNextPending()`, but the application layer coordination for the resume flow needs a dedicated use case in the implementation spec. |
| Intermediate state persistence on restart | Open question | If the .NET backend restarts while a session is in an intermediate state (Interrupting, Pausing, Resuming, Cancelling), the sidecar acknowledgement that would complete the transition may arrive after the backend comes back up. The `IAgentRuntimeEventConsumer` background service must handle replaying acknowledgement events from the Redis Stream offset where it left off. The session's intermediate state in the database serves as the signal to re-subscribe to the acknowledgement stream on restart. |
| Idle vs AwaitingReview status migration | Design decision | The previous `AwaitingReview` enum value is replaced by `Idle` with a `HasPendingReview: bool` flag. This was chosen because an Idle session with pending review and an Idle session without both accept follow-up messages — the distinction is presentational, not behavioral. The UI renders them differently ("Awaiting Review" vs "Idle") based on the flag. The old `AwaitingReview` → `Completed` transition is now `Idle (HasPendingReview=true)` → ApproveOutput → `Completed`, with a new `Close()` path for `Idle (HasPendingReview=false)` → `Completed`. |
| ContextExhausted detection threshold | Open question | The sidecar emits `context_nearing_limit` at a configurable threshold (suggested 85%) and `context_exhausted` when the window is full. The warn threshold should be configurable per session or template — a template designed for long-running tasks may want to warn earlier (e.g., 70%) to give more time to write the handoff document. The default threshold (85%) is hardcoded in the initial implementation; per-template configuration should be added in a follow-up. |
| Handoff system prompt requirement | Architecture note | The sidecar's system prompt MUST include an instruction to write a structured handoff document as the last action when the context window is nearly full. This is what produces the `handoff_document` event that the ACL translates to `RecordHandoffDocumentCommand`. Without this instruction in the system prompt, context exhaustion will produce only `context_exhausted` + `session_end` with no handoff content. The handoff instruction format should be standardised and included in all AgentTemplate system prompts either automatically by the infrastructure layer or by convention in the template. |
| SessionMetrics and SessionBudget cost sync | Design decision | `SessionMetrics.TotalCostUsd` and `SessionBudget.EstimatedCostUsd` represent the same value from different domain concerns. Both are updated atomically by `session.UpdateMetrics()`. The budget concern drives enforcement (hard cap, warnings); the metrics concern drives reporting and UI display. A future refactoring may merge them, but keeping them separate avoids burdening the SessionBudget value object with reporting semantics that belong in SessionMetrics. |
| ContextWindow snapshot semantics | Design note | The Claude SDK's `usage_update` event reports the current context as a cumulative snapshot of all tokens in the current message (input + output + cache_read + cache_write), not an incremental delta. Each update REPLACES the previous snapshot entirely. Implementors must not add successive ContextWindowSnapshot values — only the latest snapshot represents the current context size. This is distinct from TotalCostUsd and TokenUsageByModel which ARE incremental and MUST be summed. |
| SessionChain creation timing | Design decision | A `SessionChain` is created lazily — only when the first context exhaustion occurs. Standalone sessions that complete normally within a single context window never have a `SessionChain`. When exhaustion occurs, the application layer creates the chain retroactively (`CreateSessionChainCommand`) and updates the exhausted session's `ChainId`. This keeps the common case simple (no chain overhead) while making the handoff case fully traceable. |
