# ADR-007: Bidirectional Agent Session Communication with Domain-Level Message Queuing

> **Source**: Architecture decision — v2 Agent Sessions module design
> **Status**: Accepted
> **Last Updated**: 2026-02-18

---

## Context

ADR-006 established the event-sourced sidecar pattern where Node.js processes publish raw SDK events to Redis Streams and the .NET backend builds domain state (projections) via an Anti-Corruption Layer. That architecture is one-directional: information flows from the agent outward (agent → Redis → .NET backend).

Real agent sessions require two-way communication:

- Users need to send **steering messages** that influence the agent's current behavior mid-turn (e.g., "focus on the tests, not the implementation")
- Users need to send **interruption requests** — pause a running session or cancel it entirely
- Users need to queue **follow-up messages** that the agent processes after its current turn completes
- The message queue itself is a **user-facing feature** — users see pending messages, cancel them, and promote queued messages to immediate priority

The Claude Agent SDK's hook system (12 hook events including `PreToolUse`, `PostToolUse`, `Stop`, `SessionStart`, and `SessionEnd`) provides injection points for steering. These hooks run inside the Node.js host process, making them natural intercept points for inbound commands without interrupting the API call flow. The SDK's `query()` API also accepts messages mid-session for steering-style injection.

Without inbound communication modeled at the domain level, the message queue would be invisible to the .NET backend — meaning no audit trail, no domain invariants (priority, sequencing), and no user-facing management of pending messages.

---

## Decision

1. Agent session communication is **bidirectional**: events flow out (agent → Redis → .NET), commands flow in (.NET → Redis → agent sidecar)
2. **Redis Streams serves as both channels**: the existing outbound event stream (one per session) is complemented by a per-session inbound command stream
3. The **message queue is a first-class domain aggregate** (`SessionMessageQueue`), not an infrastructure detail — because users interact with it (view, cancel, reorder, promote)
4. Messages have domain-level **priority**:
   - `Immediate` — Injected into the running agentic loop via SDK hooks; used for steering messages and interruption requests. Bypasses the queue entirely.
   - `Queued` — Processed sequentially after the current turn completes; used for follow-up instructions and chained tasks.
5. A **session pool** (domain service) enforces a configurable maximum number of concurrent sessions (default: 20)
6. **MCP custom tools** are a domain concept — sessions and templates define tool configurations that extend agent capabilities per-project. Tool definitions belong in the domain, not hardcoded in infrastructure.
7. **Lifecycle state transitions are two-phase domain operations** — interrupt, pause, resume, and cancel each follow a REQUEST → intermediate state → CONFIRM pattern. The domain aggregate transitions to an intermediate state (e.g., `Pausing`) when the request is made, preventing duplicate requests. The final state (e.g., `Paused`) is only reached when the sidecar sends an acknowledgement, which the ACL translates into a domain CONFIRM command.

### Architecture

The React UI communicates only with the .NET API backend. All commands originate from the UI, are validated and executed as domain commands by .NET, then written to Redis for the sidecar. Session output travels the reverse path: sidecar → Redis → .NET backend → UI via SSE/WebSocket. Redis and Node.js are internal infrastructure — invisible to the browser.

```
┌──────────┐  HTTP/SSE   ┌──────────────┐  cmd stream  ┌───────────┐  cmd stream  ┌─────────────────┐
│  React   │ ──────────→ │  .NET        │ ──────────→  │  Redis    │ ──────────→  │ Node.js process  │
│  UI      │ ←────────── │  API backend │ ←──────────  │  Streams  │ ←──────────  │ (Agent SDK)      │
│          │  SSE/WS     │  (mediates   │  raw events/ │           │  raw events/ │ (subscribes to   │
└──────────┘              │   all comms) │  ack events  │           │  ack events  │  cmd stream)     │
                          └──────────────┘              └───────────┘              └─────────────────┘
```

The Node.js sidecar runs a simple `XREAD` consumer loop on the inbound command stream alongside the SDK's agentic loop. When a command arrives:

- `Immediate` priority messages are injected via the SDK hook intercept before the next tool call
- `Queued` messages are buffered and submitted as input after the current turn's `Stop` event fires
- `Pause` / `Cancel` / `Interrupt` commands trigger the appropriate SDK path (graceful drain or `query.interrupt()`)
- After executing each command, the sidecar writes an acknowledgement event to the outbound event stream (`pause_ack`, `resume_ack`, `stop_ack`, `interrupt_ack`)
- The `.NET` ACL reads the acknowledgement and dispatches the CONFIRM domain command to complete the state transition

The `.NET` backend publishes to the inbound command stream only after the relevant domain aggregate has validated and recorded the intent — the domain is the authority; Redis is the delivery mechanism.

---

## Rationale

1. **Message queue as domain**: The user interacts with the queue — they view pending messages, cancel them, and promote queued messages to immediate priority. This is business logic, not infrastructure plumbing. Modeling it as an aggregate gives it invariant enforcement, an audit trail, and full lifecycle management.

2. **Priority discrimination**: Steering messages are time-sensitive. A user redirecting an agent mid-turn must not wait for the current turn to finish. Queued follow-ups, by contrast, should not interrupt a running turn. The domain enforces this distinction; the sidecar command loop acts on it.

3. **Session pool as domain invariant**: Concurrent sessions consume API quota and compute. The maximum concurrent session limit is a business rule (not an infrastructure scaling knob) because it maps directly to budget and user experience guarantees. The domain service enforces it before a `StartAgentSessionCommand` is processed.

4. **MCP custom tools in the domain**: Tool definitions determine what an agent session is capable of. They belong on `AgentTemplate` and `AgentSession` aggregates, not hardcoded in the Node.js sidecar infrastructure. This lets users configure different tool profiles per project and per session, and lets the domain validate tool configurations before a session starts.

5. **SDK hooks enable non-disruptive steering**: The Claude Agent SDK's `PreToolUse`, `PostToolUse`, and permission hooks run synchronously inside the Node.js host process between API calls. They are the least-disruptive injection point for steering messages — the API call in flight is not cancelled; the hook simply shapes the next action.

6. **Redis as both channels**: Extending the existing Redis Streams topology to carry inbound commands keeps the infrastructure footprint minimal. The outbound stream already exists per session; the inbound stream is a lightweight companion. Both are lightweight at single-user scale (low throughput — bounded by human typing speed).

7. **Two-phase lifecycle transitions**: Modeling interrupt/pause/resume/cancel as two-phase operations (REQUEST event → intermediate state → sidecar ack → CONFIRM event → final state) prevents duplicate lifecycle requests and provides a clear audit trail of user intent vs. sidecar execution. The intermediate states are domain concepts, not infrastructure artifacts.

---

## Alternatives Considered

### 1. WebSocket direct to the Node.js sidecar

Open a WebSocket from the frontend (or .NET backend) directly to the running Node.js process for inbound messages.

**Rejected because**: This bypasses the domain entirely. The .NET backend would not see or record the messages, breaking the audit trail and the `SessionMessageQueue` invariant. It also couples the frontend to the sidecar's network address, which changes with every process restart.

### 2. Message queue as infrastructure only (invisible to users)

The queue exists in Redis or in memory inside the sidecar but is not exposed as a domain concept. Users cannot see, cancel, or reorder pending messages.

**Rejected because**: Queue management (viewing pending messages, cancelling, promoting) is an explicit user requirement in `docs/product/modules/agents.md`. An infrastructure-only queue cannot satisfy these requirements without recreating a domain model outside the domain layer.

### 3. No session pool limit — unlimited concurrent sessions

Allow users to start as many concurrent sessions as they want, relying on Anthropic API rate limits as the natural backstop.

**Rejected because**: API rate limits produce opaque errors, not user-friendly feedback. Budget exhaustion can happen faster than the user realizes if many sessions run simultaneously. The configurable pool limit gives the domain an explicit enforcement point with a clear domain event (`SessionPoolCapacityReachedEvent`) and a meaningful error message.

### 4. Single-phase lifecycle transitions (fire-and-forget)

Send interrupt/pause/resume/cancel as infrastructure-only operations without tracking intermediate states in the domain.

**Rejected because**: Without intermediate states, a second pause request sent while the sidecar is still processing the first would be accepted by the domain and result in duplicate Redis commands. The intermediate states (`Interrupting`, `Pausing`, `Resuming`, `Cancelling`) are domain invariants that prevent this class of concurrency bug and provide an unambiguous audit trail of what was requested vs. what was confirmed.

---

## Consequences

### Positive

- Full user control over running sessions — steering, pausing, cancelling, and queuing follow-ups
- Complete audit trail for all messages in both directions — inbound commands are domain events on `SessionMessageQueue`
- Budget protection — the session pool prevents uncontrolled concurrent spend
- Extensibility via MCP — tool capabilities are configurable per project and per template without sidecar code changes
- Consistent architecture — inbound commands use the same Redis Streams topology already established in ADR-006
- Duplicate-request protection — intermediate states prevent double-pause, double-cancel, etc.
- Clean UI decoupling — the React UI only knows about the .NET API; Redis topology can change without frontend changes

### Negative

- More complex Redis topology — two streams per session (outbound events + inbound commands)
- Node.js sidecar gains a command listener loop alongside the SDK's agentic loop
- More domain entities to maintain — `SessionMessageQueue` aggregate, `SessionMessage` entity, `MessagePriority` value object, session pool domain service
- More domain states to manage — four intermediate states in `AgentSessionStatus` and four corresponding confirmation commands

### Mitigations

- The inbound command stream is extremely low throughput (human typing speed). The `XREAD` consumer loop is a standard Redis pattern with negligible overhead.
- The sidecar remains deliberately thin: it listens on the command stream and acts on commands; it does not validate or enforce domain rules. Domain validation happens in .NET before anything is written to the command stream.
- The session pool domain service is a simple counter with a configurable limit. It is not a complex scheduler.
- Intermediate states resolve quickly (milliseconds to seconds for sidecar acknowledgement). They are transient in practice even if persistent in the domain model.

---

## Key Domain Additions (relative to ADR-006)

| Concept | Type | Responsibility |
|---------|------|----------------|
| `SessionMessageQueue` | Aggregate | Owns the ordered list of pending messages for a session; enforces priority rules; records all inbound messages |
| `SessionMessage` | Entity (on `SessionMessageQueue`) | Captures message content, priority, status (Pending / Delivered / Cancelled), and timestamps |
| `MessagePriority` | Value Object | Discriminates `Immediate` (hook injection) from `Queued` (post-turn delivery) |
| `SessionPool` | Domain Service | Enforces max concurrent sessions; gates `StartAgentSessionCommand` |
| `MCP tool config` | Value Object (on `AgentTemplate` / `AgentSession`) | Declares which MCP servers and tools are active for a session |
| Intermediate states | `AgentSessionStatus` values | `Interrupting`, `Pausing`, `Resuming`, `Cancelling` — prevent duplicate lifecycle requests; resolved by sidecar ack |
| REQUEST events | Domain Events | `SessionInterruptRequestedEvent`, `SessionPauseRequestedEvent`, `SessionResumeRequestedEvent`, `SessionCancelRequestedEvent` — record user intent |
| CONFIRM commands | Internal Commands | `ConfirmSessionInterruptedCommand`, `ConfirmSessionPausedCommand`, `ConfirmSessionResumedCommand`, `ConfirmSessionCancelledCommand` — dispatched by ACL on sidecar ack |

---

## Related Documents

- `docs/architecture/decisions/adr-006-agent-session-architecture.md` — the sidecar pattern this ADR extends
- `docs/domain/contexts/agents.md` — full bounded context design including `IAgentRuntime` port and `SessionMessageQueue`
- `docs/product/modules/agents.md` — product requirements for the Agent Sessions module (AG-001 through AG-020)
- `docs/scenarios/agent-workflows.md` — user scenarios that drove this architecture
- `docs/operations/research/claude-api.md` — Claude Agent SDK hook system reference
- `docs/operations/research/redis-streams.md` — Redis Streams topology and consumer group patterns
