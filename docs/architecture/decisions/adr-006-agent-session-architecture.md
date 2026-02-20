# ADR-006: Agent Sessions Use Event-Sourced Node.js Sidecars

> **Source**: Architecture decision — v2 Agent Sessions module design
> **Status**: Accepted
> **Last Updated**: 2026-02-18

---

## Context

The Agent Sessions module needs to run Claude Code sessions. The Claude Agent SDK is only available in TypeScript/Node.js — there is no .NET Agent SDK. The SDK's event stream model (tool results as user messages, flat message mixing between parent/child agents) is deeply coupled to the Anthropic API wire format and would corrupt our domain model if consumed directly.

The domain model in `docs/domain/contexts/agents.md` defines clean aggregates (`AgentSession`, `WorkQueue`, `AgentTemplate`) with well-typed value objects (`SessionLogChunk`, `TokenUsage`, `SessionOutput`) and an anti-corruption layer port (`IAgentRuntime`). Bridging the SDK's wire format into this clean domain requires an explicit translation boundary.

---

## Decision

Each agent session runs as an independent Node.js process using the Claude Agent SDK. The process publishes raw SDK events to Redis Streams. The .NET backend subscribes to these streams and transforms them into clean domain events via an Anti-Corruption Layer (ACL).

The React UI communicates exclusively with the .NET API backend — it never connects to Redis or the Node.js sidecars directly. The .NET API is the single gateway for all client interactions: it receives commands from the UI, writes them to Redis, and pushes session output back to the UI via SSE or WebSocket.

```
┌──────────┐  HTTP/SSE   ┌──────────────┐  cmd stream  ┌───────────┐  cmd stream  ┌─────────────────┐
│  React   │ ──────────→ │  .NET        │ ──────────→  │  Redis    │ ──────────→  │ Node.js process  │
│  UI      │ ←────────── │  API backend │ ←──────────  │  Streams  │ ←──────────  │ (Agent SDK)      │
│          │  SSE/WS     │  (ACL layer) │  raw events  │           │  raw events  │ 1 per session    │
└──────────┘              └──────────────┘              └───────────┘              └─────────────────┘
```

The Node.js sidecar is deliberately thin: it wraps the SDK, streams raw events to Redis, and does no domain logic. All business rules (budget enforcement, status transitions, audit trail) live in the .NET backend.

---

## Rationale

1. **SDK availability**: The Claude Agent SDK is TypeScript-only. A Node.js sidecar is the only viable path to using the SDK without reimplementing multi-turn management and tool orchestration from scratch.

2. **Domain purity**: The SDK's wire format is not a clean domain model. Tool results arrive as user messages; parent and sub-agent messages are mixed in a flat stream; token counts are reported incrementally at the transport layer. The ACL transforms these into structured domain entities (`ToolCall.Result`, `AgentMessage`, `SubAgentSession.Messages`, `TokenUsage`) before they touch the domain layer.

3. **Isolation**: One process per session means crashes are scoped to a single session. The .NET backend can kill or restart individual Node.js processes without affecting other running sessions or the application itself.

4. **Real-time streaming**: Redis Streams provide durable pub/sub for live UI updates. The .NET SignalR hub subscribes to the stream for a session and forwards chunks to the browser via SSE or WebSocket, satisfying the `GetSessionOutputStreamQuery` contract without polling. The UI never connects to Redis directly.

5. **Replay and audit**: The event stream is the audit trail. All raw events are appended to a Redis Stream keyed by session ID. The `IAgentSessionRepository.AppendLogChunkAsync` implementation writes to this stream. Log replay (AG-017) is a Redis range read, not a database scan.

6. **Budget enforcement**: The .NET backend is the authority on token budgets. It monitors token usage events from the stream and issues a `SIGTERM` to the Node.js process when the `SessionBudget.HardCapUsd` is reached, satisfying the `SessionBudgetExhaustedEvent` invariant without trusting the sidecar to self-terminate.

---

## Alternatives Considered

### 1. Direct API calls from .NET

Make HTTP calls to the Anthropic API directly from the .NET backend, bypassing the Agent SDK entirely.

**Rejected because**: The Agent SDK provides multi-turn conversation management, tool call orchestration, sub-agent spawning, and streaming — all implemented and tested by Anthropic. Reimplementing this from raw API calls is weeks of infrastructure work with high risk of drift as the API evolves.

### 2. Embedded Node.js in .NET (Jint, Edge.js, Node.js via subprocess with eval)

Run JavaScript inside the .NET process or as a tightly coupled subprocess.

**Rejected because**: Jint is an interpreter that does not support modern JS (ESM, async/await at full fidelity, native Node.js modules). Edge.js is effectively unmaintained. Tight coupling means a SDK crash takes down the .NET process. Debugging is significantly harder in a hybrid runtime.

### 3. gRPC between Node.js and .NET

Use a gRPC contract to communicate between the Node.js sidecar and the .NET backend.

**Rejected because**: gRPC requires Protobuf schema management for every event type. Redis Streams are schema-free JSON, which is more agile during early domain design when event shapes are still evolving. gRPC also has no built-in event replay — persistence and ordering must be added separately. Redis Streams provide both.

### 4. RabbitMQ or Azure Service Bus

Use a heavier message broker instead of Redis Streams.

**Rejected because**: LemonDo is a single-user personal tool. A managed message broker adds cost, operational complexity, and infrastructure dependencies that are disproportionate to the scale. Redis is lightweight, Aspire has first-class Redis integration, and Redis Streams cover all the required patterns (pub/sub, persistence, consumer groups, range reads for replay).

---

## Consequences

### Positive

- Clean domain model — SDK wire format never touches aggregates
- Crash isolation — a failed sidecar is a recoverable session failure, not an application failure
- Free audit trail — the Redis Stream IS the event log (AG-017 fulfilled)
- Real-time UI updates — .NET SignalR hub bridges Redis → browser via SSE/WebSocket without polling; UI is decoupled from Redis
- Budget enforcement — .NET is authoritative; sidecar cannot override hard caps
- Testable — `IAgentRuntime` port can be implemented as an in-memory test double for unit tests

### Negative

- Two runtime environments — Node.js and .NET must both be running for agent sessions to work
- Redis dependency — adds a required infrastructure component (Redis, or Azure Cache for Redis in production)
- Event serialization overhead — raw SDK events must be JSON-serialized for Redis and then deserialized and transformed by the ACL

### Mitigations

- The Node.js sidecar is intentionally thin (SDK wrapper + Redis publisher only). Maintenance surface is minimal.
- Redis is lightweight for single-user scale and is already a standard Aspire component. Aspire orchestrates both the Node.js sidecar processes and Redis as part of the app host.
- The ACL translation layer is isolated in the infrastructure implementation of `IAgentRuntime`. Domain code has no knowledge of Redis or the SDK event format.

---

## Key ACL Mappings

| SDK Event / Wire Format | Domain Model |
|-------------------------|-------------|
| Tool result as user message | `ToolCall.Result` (owned by `ToolCall` entity on `AgentSession`) |
| Mixed parent/sub-agent messages in flat stream | Separate `AgentMessage` vs `SubAgentSession.Messages` |
| Flat message source field (string) | Discriminated `Source` enum: `User`, `System`, `Agent`, `SubAgent`, `Webhook` |
| Raw incremental token counts | `TokenUsage` value object on `AgentSession` (via `UpdateBudgetSpend`) |
| Stream end / stop reason | `RecordSessionOutputCommand` dispatched to domain |
| Tool call with name + input | `ToolCall` entity: `Name`, `Input` (JSON), `Status` |

---

## Related Documents

- `docs/domain/contexts/agents.md` — full bounded context design including `IAgentRuntime` port (§8.11)
- `docs/product/modules/agents.md` — product requirements for the Agent Sessions module
- `docs/scenarios/agent-workflows.md` — user scenarios that drove this architecture
