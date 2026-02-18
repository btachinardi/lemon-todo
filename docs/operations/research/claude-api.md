# Anthropic Claude API and Agent SDK

> **Source**: Extracted from docs/operations/research.md §10
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date Researched**: 2026-02-18
> **Purpose**: Core AI integration for the `agents` module — starting Claude Code agent sessions programmatically, streaming session output, tracking token usage and costs, and enabling agent-to-LemonDo API callbacks.
> **Recommendation**: Use — official Python and TypeScript SDKs are excellent; .NET has an official SDK; the Claude Agent SDK directly addresses LemonDo's session orchestration needs.

---

## 10. Anthropic Claude API and Agent SDK

### 10.1 Capabilities

**Anthropic Messages API (Claude API)**:
- Send messages to any Claude model and receive streaming or batch responses
- Full tool use (function calling) support for structured agent behavior
- Prompt caching to reduce costs on repeated context (system prompts, large documents)
- Batch API for asynchronous processing at 50% cost discount
- Token usage reporting in every API response (`input_tokens`, `output_tokens`, `cache_read_input_tokens`)

**Claude Agent SDK** (formerly Claude Code SDK):
- Programmatically launch and manage Claude Code agent sessions
- Stream session output as an async iterator of typed messages
- Session management: create, resume, and fork sessions by session ID
- Built-in tools: Read, Write, Edit, Bash, Glob, Grep, WebSearch, WebFetch
- Lifecycle hooks: `PreToolUse`, `PostToolUse`, `Stop`, `SessionStart`, `SessionEnd`
- Subagent orchestration: spawn specialized sub-agents within a parent session
- MCP (Model Context Protocol) server integration for connecting to external systems
- Permission control: allowlist specific tools, block dangerous ones, require human approval
- File checkpointing: restore files to any previous state in a conversation

The Agent SDK is the right tool for LemonDo's AG-001 through AG-020 requirements. The Messages API is the right tool for AG-014 (agents adding comms/people learnings via LLM analysis).

### 10.2 Authentication

**Auth flow**: API key passed via `ANTHROPIC_API_KEY` environment variable or `x-api-key` header; stored in Azure Key Vault for production

**Scopes required**: Not applicable — API key grants full access to all Claude models within the organization's tier limits

### 10.3 Rate Limits

Limits are per organization, per model family, per minute. Single-user personal use starts at Tier 1 (after purchasing $5 credit):

| Tier | RPM (Sonnet 4.x) | ITPM (Sonnet 4.x) | OTPM (Sonnet 4.x) | Advance Requirement |
|------|-------------------|-------------------|-------------------|--------------------|
| Tier 1 | 50 | 30,000 | 8,000 | $5 credit purchase |
| Tier 2 | 1,000 | 450,000 | 90,000 | $40 cumulative |
| Tier 3 | 2,000 | 800,000 | 160,000 | $200 cumulative |
| Tier 4 | 4,000 | 2,000,000 | 400,000 | $400 cumulative |

For LemonDo's single-user agent orchestration (scenario: 7 parallel agents per S-AG-01), Tier 1's 50 RPM is the binding constraint. Each agent session makes multiple API calls. Tier 2 is the practical minimum for parallel agent workloads. Cached tokens do not count toward ITPM limits — prompt caching on CLAUDE.md and system prompts is essential.

Exceeded limits return HTTP 429 with a `retry-after` header.

### 10.4 Pricing

| Model | Input (per MTok) | Output (per MTok) | Cache Read (per MTok) | Best For |
|-------|-----------------|-------------------|----------------------|----------|
| Claude Opus 4.6 | $5.00 | $25.00 | $0.50 | Complex reasoning, architecture decisions |
| Claude Sonnet 4.6 | $3.00 | $15.00 | $0.30 | Balanced — recommended for agent sessions |
| Claude Haiku 4.5 | $1.00 | $5.00 | $0.10 | Simple tasks, classification, routing |

Batch API provides 50% discount on all models (async, non-real-time use only).

**Estimated monthly cost (single user)**: Highly variable based on session count and length. A rough estimate for 10 agent sessions/week at ~50K tokens each: approximately $15-$40/month using Claude Sonnet 4.6. Budget tracking (AG-010) is essential to prevent unexpected spend.

### 10.5 SDK Options

| Platform | Package | Maintained | Last Updated | Notes |
|----------|---------|------------|--------------|-------|
| .NET (official) | `Anthropic` (NuGet) | Yes — Anthropic official | February 18, 2026 (v12.6.0) | Targets .NET Standard 2.0, .NET 8, .NET 9; compatible with .NET 10 |
| .NET (community) | `Anthropic.SDK` (NuGet) | Yes — community | 2025 (v5.9.0) | Targets .NET 8 and .NET 10 explicitly; supports Semantic Kernel |
| TypeScript (official) | `@anthropic-ai/sdk` (npm) | Yes — Anthropic official | Active | Primary SDK for TypeScript/Node.js |
| TypeScript — Agent SDK | `@anthropic-ai/claude-agent-sdk` (npm) | Yes — Anthropic official | Active | Session management, built-in tools, streaming |
| Python — Agent SDK | `claude-agent-sdk` (PyPI) | Yes — Anthropic official | Active | Session management, same capabilities as TypeScript |

The Agent SDK (TypeScript and Python) is what powers Claude Code. For LemonDo's backend (.NET), use the official `Anthropic` NuGet package for the Messages API. For agent session management, the Node.js sidecar approach using the TypeScript Agent SDK (`@anthropic-ai/claude-agent-sdk`) is the most natural fit — launch Node.js processes from .NET via `child_process`-equivalent, stream output over SSE or WebSocket.

### 10.6 Risks

| Risk | Level | Detail |
|------|-------|--------|
| API stability | Low | Anthropic maintains backward compatibility; model deprecations are announced well in advance |
| Vendor lock-in | Medium | Token cost tracking, agent session format, and CLAUDE.md conventions are Anthropic-specific; switching AI provider would require significant refactor |
| Rate limit impact | Medium | Tier 1's 50 RPM can constrain parallel agent workloads; upgrading to Tier 2 requires $40 cumulative spend |
| Pricing risk | Medium | Costs scale directly with agent usage; without budget controls (AG-010), a runaway agent session could incur unexpected charges |
| Agent SDK maturity | Medium | The Agent SDK is recently renamed from Claude Code SDK; V2 session APIs are marked `unstable_v2_*`; breaking changes are possible |
| No .NET Agent SDK | Medium | The Agent SDK is Python and TypeScript only; .NET backend must call agent sessions via a Node.js sidecar process or subprocess |

### 10.7 Alternatives

| Option | SDK (.NET) | Pricing | Complexity | Recommendation |
|--------|-----------|---------|------------|----------------|
| Anthropic Claude API + Agent SDK | Official (Messages); Node.js sidecar for Agent SDK | Usage-based ($3-$25/MTok) | Medium | Primary — purpose-built for this use case |
| OpenAI GPT-4o + Assistants API | Community (Betalgo.OpenAI) | Usage-based | Medium | Fallback — no equivalent to Claude Code's agent loop |
| Azure OpenAI | Official (Azure.AI.OpenAI) | Usage-based + Azure hosting | Medium | Fallback — enterprise path, less relevant for single-user |

### 10.8 References

- [Claude Agent SDK Overview](https://platform.claude.com/docs/en/agent-sdk/overview)
- [Claude Agent SDK — TypeScript GitHub](https://github.com/anthropics/claude-agent-sdk-typescript)
- [Claude Agent SDK — Python GitHub](https://github.com/anthropics/claude-agent-sdk-python)
- [Anthropic Messages API Pricing](https://platform.claude.com/docs/en/about-claude/pricing)
- [Anthropic API Rate Limits](https://platform.claude.com/docs/en/api/rate-limits)
- [Anthropic .NET SDK NuGet](https://www.nuget.org/packages/Anthropic/)
- [Anthropic TypeScript SDK npm](https://www.npmjs.com/package/@anthropic-ai/sdk)
- [Claude Agent SDK npm](https://www.npmjs.com/package/@anthropic-ai/claude-agent-sdk)

---

## 10A. Claude Agent SDK — Deep Dive

> **Date Researched**: 2026-02-18
> **Purpose**: Detailed SDK internals for the `agents` module ACL design — event types, message structure, subagent routing, session lifecycle, and .NET interop patterns.
> **Recommendation**: Use (Node.js sidecar pattern) — the SDK is the only supported path for programmatic Claude Code session management. The event stream is well-documented and maps cleanly to a domain ACL.

---

### 10A.1 Package Facts

| Field | Value |
|-------|-------|
| npm package | `@anthropic-ai/claude-agent-sdk` |
| Current version | 0.2.45 (as of 2026-02-18, published ~17 hours prior) |
| Release cadence | Near-daily releases tracking Claude Code CLI parity |
| Node.js requirement | 18+ |
| Key peer dependency | `zod ^3.24.1` |
| License | Anthropic Commercial Terms of Service |
| GitHub | `anthropics/claude-agent-sdk-typescript` |
| Python equivalent | `claude-agent-sdk` (PyPI) — same capabilities, different syntax |

The package is updated extremely frequently (multiple times per week) to maintain parity with the Claude Code CLI. Version numbers follow the Claude Code CLI version (e.g., SDK 0.2.45 tracks CLI v2.1.45). This is both a strength (features ship immediately) and a risk (consumers of the SDK must update frequently to avoid drift).

---

### 10A.2 SDK Architecture: How It Works Internally

The SDK is **not** a thin HTTP client wrapper around the Anthropic API. It operates by:

1. **Spawning the Claude Code CLI** as a Node.js subprocess
2. **Communicating via stdin/stdout** — the SDK writes configuration to stdin; the CLI streams structured JSON messages to stdout
3. **The CLI handles** authentication, the agent loop, tool execution, session persistence, and all direct API calls to `api.anthropic.com`

This architecture means:
- The SDK requires the Claude Code CLI to be installed (bundled as a dependency in `@anthropic-ai/claude-agent-sdk` — no separate install needed)
- Agent tool execution happens inside the CLI subprocess, not in the Node.js host process
- The Node.js host process only handles configuration, message routing, and hook callbacks
- For LemonDo's .NET backend, this means the "sidecar" is a Node.js process wrapping the SDK, not just a raw HTTP proxy

---

### 10A.3 V1 API: The `query()` Function

The primary V1 entry point. Returns an `AsyncGenerator` of `SDKMessage` objects.

```typescript
// Signature
function query({
  prompt: string | AsyncIterable<SDKUserMessage>,
  options?: Options
}): Query  // extends AsyncGenerator<SDKMessage, void>
```

The `Query` object is both the async iterator and a control handle:

```typescript
interface Query extends AsyncGenerator<SDKMessage, void> {
  interrupt(): Promise<void>           // Stop the agent mid-execution
  rewindFiles(userMessageUuid): Promise<void>  // Restore files to checkpoint
  setPermissionMode(mode): Promise<void>
  setModel(model?): Promise<void>
  setMaxThinkingTokens(n): Promise<void>
  supportedCommands(): Promise<SlashCommand[]>
  supportedModels(): Promise<ModelInfo[]>
  mcpServerStatus(): Promise<McpServerStatus[]>
  accountInfo(): Promise<AccountInfo>
}
```

Key `Options` fields relevant to LemonDo:

| Option | Type | LemonDo Use |
|--------|------|-------------|
| `prompt` | `string` | The agent's task objective (from the LemonDo task description) |
| `systemPrompt` | `string \| preset` | Use `{ type: 'preset', preset: 'claude_code' }` to load CLAUDE.md |
| `settingSources` | `SettingSource[]` | Include `'project'` to load CLAUDE.md from the worktree |
| `cwd` | `string` | Worktree path — sets the agent's working directory |
| `model` | `string` | Model override (e.g., `'claude-sonnet-4-6'`) |
| `allowedTools` | `string[]` | Allowlist — use `['Read','Write','Edit','Bash','Glob','Grep','Task']` |
| `permissionMode` | `PermissionMode` | `'bypassPermissions'` for headless CI, `'default'` for supervised |
| `maxTurns` | `number` | Hard ceiling on turns — critical for AG-010 budget control |
| `maxBudgetUsd` | `number` | Hard USD ceiling per session — maps directly to AG-010 |
| `resume` | `string` | Session ID to continue from a previous query |
| `forkSession` | `boolean` | Branch from resumed session without modifying original |
| `agents` | `Record<string, AgentDefinition>` | Define named subagents |
| `hooks` | `Partial<Record<HookEvent, HookCallbackMatcher[]>>` | Lifecycle callbacks |
| `mcpServers` | `Record<string, McpServerConfig>` | Connect external systems (e.g., LemonDo's own MCP server) |
| `enableFileCheckpointing` | `boolean` | Enable AG-019 file rewind capability |
| `abortController` | `AbortController` | Programmatic cancellation |

---

### 10A.4 Complete `SDKMessage` Type Union

Every message yielded by `query()` is one of these seven types. This is the core ACL boundary — LemonDo's Node.js sidecar must classify each incoming message and forward the appropriate representation to the .NET backend.

```typescript
type SDKMessage =
  | SDKSystemMessage          // Session init or compact boundary
  | SDKAssistantMessage       // Claude's text responses and tool invocations
  | SDKUserMessage            // Tool results and user inputs (NOT just human text)
  | SDKUserMessageReplay      // Replayed user messages when resuming sessions
  | SDKResultMessage          // Final outcome with cost and usage totals
  | SDKPartialAssistantMessage // Token-by-token streaming (opt-in)
  | SDKCompactBoundaryMessage  // Context window compaction event
```

#### `SDKSystemMessage` — Session Initialization

The **first message** in every session. Extract `session_id` here.

```typescript
type SDKSystemMessage = {
  type: "system";
  subtype: "init";
  uuid: UUID;
  session_id: string;       // CAPTURE THIS — needed for resume, fork, audit trail
  apiKeySource: ApiKeySource;
  cwd: string;              // Effective working directory (worktree path)
  tools: string[];          // Active tool list
  mcp_servers: { name: string; status: string }[];
  model: string;            // Active model name
  permissionMode: PermissionMode;
  slash_commands: string[];
  output_style: string;
}
```

#### `SDKAssistantMessage` — Claude's Responses and Tool Invocations

```typescript
type SDKAssistantMessage = {
  type: "assistant";
  uuid: UUID;
  session_id: string;
  message: APIAssistantMessage;      // Standard Anthropic SDK message type
  parent_tool_use_id: string | null; // SEE SUBAGENT SECTION — non-null = from subagent
}
```

The `message.content` array is the key field. It contains a mix of typed blocks:

```typescript
// Content block types within message.content:
{ type: "text"; text: string }                       // Claude's reasoning/response
{ type: "tool_use"; id: string; name: string; input: ToolInput }  // Tool invocation
```

**Important**: Multiple content blocks can appear in a single `SDKAssistantMessage`. A message that calls three tools in parallel will have a text block followed by three `tool_use` blocks — all sharing the same message `uuid`. This has implications for cost deduplication (see §10A.8).

#### `SDKUserMessage` — Tool Results (NOT just human input)

This is the observation Bruno reported: **tool call results arrive as `SDKUserMessage` objects**, not as a separate result type. The Anthropic wire format treats tool results as user-turn content (the model receives them as if the user provided them).

```typescript
type SDKUserMessage = {
  type: "user";
  uuid?: UUID;
  session_id: string;
  message: APIUserMessage;           // Standard Anthropic SDK message type
  parent_tool_use_id: string | null; // Non-null = this result is from a subagent tool
}
```

The `message.content` array contains:

```typescript
// Tool result block:
{
  type: "tool_result";
  tool_use_id: string;  // Matches the tool_use block's `id` field in the preceding assistant message
  content: string | Array<{ type: "text"; text: string } | { type: "image"; ... }>;
  is_error?: boolean;
}
```

When `is_error: true`, the tool execution failed. The `content` field contains the error message. The model receives this and decides whether to retry or surface the error.

**For the ACL**: the sequence is always `SDKAssistantMessage` (tool_use block) → `SDKUserMessage` (tool_result block). You pair them by `tool_use_id`. The `uuid` field on `SDKUserMessage` may be `undefined` for programmatically injected messages.

#### `SDKResultMessage` — Final Outcome

The **last message** in every session. Contains authoritative cost and usage totals.

```typescript
// Success variant
type SDKResultMessage = {
  type: "result";
  subtype: "success";
  uuid: UUID;
  session_id: string;
  duration_ms: number;
  duration_api_ms: number;
  is_error: false;
  num_turns: number;
  result: string;             // Claude's final text answer
  total_cost_usd: number;     // Authoritative total cost — use this for AG-010
  usage: NonNullableUsage;    // Aggregate token counts
  modelUsage: { [modelName: string]: ModelUsage };  // Per-model breakdown
  permission_denials: SDKPermissionDenial[];
  structured_output?: unknown;
}

// Error variants (subtype field changes)
type SDKResultMessage = {
  type: "result";
  subtype:
    | "error_max_turns"         // Hit maxTurns limit — AG-010 budget guard triggered
    | "error_during_execution"  // Tool execution or API error
    | "error_max_budget_usd"    // Hit maxBudgetUsd limit — AG-010 cost guard triggered
    | "error_max_structured_output_retries";
  // ... same fields, but `result` is absent; `errors: string[]` is present
}
```

The `modelUsage` map is critical for AG-010 multi-model cost tracking (main agent on Sonnet, subagents on Haiku):

```typescript
type ModelUsage = {
  inputTokens: number;
  outputTokens: number;
  cacheReadInputTokens: number;
  cacheCreationInputTokens: number;
  webSearchRequests: number;
  costUSD: number;             // Per-model USD cost
  contextWindow: number;
}
```

#### `SDKPartialAssistantMessage` — Token-Level Streaming (Opt-In)

Only emitted when `includePartialMessages: true` is set. Wraps raw Anthropic API streaming events.

```typescript
type SDKPartialAssistantMessage = {
  type: "stream_event";
  event: RawMessageStreamEvent;  // Raw Anthropic API streaming event
  parent_tool_use_id: string | null;
  uuid: UUID;
  session_id: string;
}
```

The `event` field contains standard Anthropic streaming events:

| Event type | What it means |
|------------|---------------|
| `message_start` | New API call beginning |
| `content_block_start` | New text or tool_use block starting |
| `content_block_delta` with `text_delta` | Text token arrived |
| `content_block_delta` with `input_json_delta` | Tool input JSON fragment |
| `content_block_stop` | Block complete |
| `message_delta` | Stop reason and final token counts |
| `message_stop` | Message complete |

**Known limitation**: When `maxThinkingTokens` is set, `stream_event` messages are NOT emitted. Extended thinking and partial streaming are mutually exclusive.

#### `SDKCompactBoundaryMessage` — Context Compaction

Emitted when the context window is compacted (either automatically or via `/compact`).

```typescript
type SDKCompactBoundaryMessage = {
  type: "system";
  subtype: "compact_boundary";
  uuid: UUID;
  session_id: string;
  compact_metadata: {
    trigger: "manual" | "auto";
    pre_tokens: number;  // Token count before compaction
  };
}
```

---

### 10A.5 The Event Stream Message Flow

The full sequence for a single-turn session with one tool call:

```
1. SDKSystemMessage      { type: "system", subtype: "init", session_id: "..." }
   → Session established. Capture session_id.

2. SDKAssistantMessage   { type: "assistant", message: { content: [
                             { type: "text", text: "I'll read the file..." },
                             { type: "tool_use", id: "tu_001", name: "Read", input: { file_path: "..." } }
                           ]}, parent_tool_use_id: null }
   → Claude's reasoning + tool invocation. Note tool_use id.

3. SDKUserMessage        { type: "user", message: { content: [
                             { type: "tool_result", tool_use_id: "tu_001",
                               content: "file contents here..." }
                           ]}, parent_tool_use_id: null }
   → Tool result (NOT a human message). Pair with tu_001 from step 2.

4. SDKAssistantMessage   { type: "assistant", message: { content: [
                             { type: "text", text: "Based on the file, I found..." }
                           ]}, parent_tool_use_id: null }
   → Final response.

5. SDKResultMessage      { type: "result", subtype: "success",
                           total_cost_usd: 0.0023,
                           modelUsage: { "claude-sonnet-4-6": { costUSD: 0.0023, ... } } }
   → Session complete. Authoritative cost figure.
```

For streaming output (`includePartialMessages: true`), steps 2 and 4 are preceded by multiple `SDKPartialAssistantMessage` events before the complete `SDKAssistantMessage` arrives.

---

### 10A.6 Subagent Architecture and Message Mixing

This is the most important section for LemonDo's ACL design because subagent messages are interleaved with parent messages in a single stream, and the only differentiator is `parent_tool_use_id`.

#### How Subagents Are Invoked

Subagents are invoked via the `Task` built-in tool. The parent agent emits a `tool_use` block with `name: "Task"`:

```typescript
// Parent SDKAssistantMessage content block:
{
  type: "tool_use",
  id: "tu_task_001",
  name: "Task",
  input: {
    description: "Review auth module",
    prompt: "Analyze the authentication code for security issues",
    subagent_type: "code-reviewer"  // Matches a key in options.agents
  }
}
```

#### The `parent_tool_use_id` Field

Every `SDKMessage` carries `parent_tool_use_id: string | null`. This field is the routing key:

- `null` → message is from the **parent agent**
- A `tool_use_id` string (e.g., `"tu_task_001"`) → message is from the **subagent** that was invoked by that tool call

```
Parent stream:
  SDKAssistantMessage { parent_tool_use_id: null }           ← parent reasoning
  SDKAssistantMessage { parent_tool_use_id: null }           ← parent invokes Task tool
    ↓ subagent spawned for "tu_task_001"
  SDKAssistantMessage { parent_tool_use_id: "tu_task_001" }  ← subagent reading files
  SDKUserMessage      { parent_tool_use_id: "tu_task_001" }  ← subagent tool result
  SDKAssistantMessage { parent_tool_use_id: "tu_task_001" }  ← subagent final answer
    ↑ subagent completes
  SDKUserMessage      { parent_tool_use_id: null }           ← Task tool result (subagent output)
  SDKAssistantMessage { parent_tool_use_id: null }           ← parent continues
```

**Key implication for the ACL**: The stream from a single `query()` call contains messages from all active subagents, interleaved. You cannot assume messages arrive in a single-agent-first order. Multiple subagents can run in parallel, so multiple non-null `parent_tool_use_id` values may appear concurrently.

#### Subagent Cannot Nest

The docs are explicit: subagents cannot spawn their own subagents. Do not include `Task` in a subagent's `tools` array. The nesting depth is exactly 1.

#### Subagent Resume

Subagents can be resumed independently. After a subagent completes, the Task tool result contains an `agentId` that can be used to resume just that subagent in a future session. The pattern:

1. Extract `session_id` from `SDKSystemMessage`
2. Parse the Task tool result in `SDKUserMessage` content to find `agentId`
3. On next `query()`, pass `resume: sessionId` and include the `agentId` in the prompt

#### Subagent Hooks

Two hook events specifically for subagent lifecycle:

```typescript
// Fired when a subagent task begins
type SubagentStartHookInput = BaseHookInput & {
  hook_event_name: "SubagentStart";
  agent_id: string;
  agent_type: string;  // The subagent_type from AgentInput
}

// Fired when a subagent task completes
type SubagentStopHookInput = BaseHookInput & {
  hook_event_name: "SubagentStop";
  stop_hook_active: boolean;
}
```

Also new in v0.2.33: `TeammateIdle` and `TaskCompleted` hook events for agent team coordination (documented in CHANGELOG but not yet in the main type reference at time of research).

---

### 10A.7 Hook System

Hooks intercept and can modify agent behavior at defined lifecycle points. They run in the same Node.js process as the SDK consumer (not inside the CLI subprocess).

#### Complete `HookEvent` Enum

```typescript
type HookEvent =
  | "PreToolUse"        // Before any tool executes — can block or modify input
  | "PostToolUse"       // After successful tool execution — can add context
  | "PostToolUseFailure" // After tool execution fails
  | "Notification"      // Agent status notification
  | "UserPromptSubmit"  // Before user prompt is sent — can augment context
  | "SessionStart"      // Session initialization — can inject context
  | "SessionEnd"        // Session teardown
  | "Stop"              // Agent has decided to stop
  | "SubagentStart"     // Subagent task beginning
  | "SubagentStop"      // Subagent task ending
  | "PreCompact"        // Before context window compaction
  | "PermissionRequest" // Tool permission check
```

#### Hook Input Shapes Relevant to LemonDo

**`PreToolUse`** — use this to intercept and log every tool call to the LemonDo audit trail (AG-018):

```typescript
type PreToolUseHookInput = {
  hook_event_name: "PreToolUse";
  session_id: string;
  cwd: string;
  tool_name: string;
  tool_input: unknown;  // Typed in ToolInput union
}
```

**`PostToolUse`** — use this to capture tool results before they go back to the model:

```typescript
type PostToolUseHookInput = {
  hook_event_name: "PostToolUse";
  tool_name: string;
  tool_input: unknown;
  tool_response: unknown;
}
```

**`SessionEnd`** — use this to trigger AG-020 completion notification:

```typescript
type SessionEndHookInput = {
  hook_event_name: "SessionEnd";
  reason: ExitReason;  // String from EXIT_REASONS array (not fully documented)
}
```

**`PermissionRequest`** — use this for AG-019 human-in-the-loop approval:

```typescript
type PermissionRequestHookInput = {
  hook_event_name: "PermissionRequest";
  tool_name: string;
  tool_input: unknown;
  permission_suggestions?: PermissionUpdate[];
}
```

#### Hook Return Values

Hooks return `HookJSONOutput`. The `hookSpecificOutput` field varies by event:

```typescript
// For PreToolUse — approve, block, or modify the tool call:
hookSpecificOutput: {
  hookEventName: "PreToolUse";
  permissionDecision?: "allow" | "deny" | "ask";
  updatedInput?: Record<string, unknown>;  // Modify tool input before execution
}

// For UserPromptSubmit and SessionStart — inject additional context:
hookSpecificOutput: {
  hookEventName: "UserPromptSubmit" | "SessionStart";
  additionalContext?: string;  // Appended to prompt context
}
```

To block a tool call from `PreToolUse`: return `{ continue: false, stopReason: "User denied" }`.

---

### 10A.8 Token Usage and Cost Tracking

#### Deduplication Rule (Critical)

Multiple `SDKAssistantMessage` objects can share the same `uuid` (when parallel tools fire). **Only charge once per unique `uuid`**. The pattern:

```typescript
const processedIds = new Set<string>();

for await (const msg of query({ prompt })) {
  if (msg.type === "assistant" && !processedIds.has(msg.uuid)) {
    processedIds.add(msg.uuid);
    // Record msg.message.usage for this turn
  }
  if (msg.type === "result") {
    // msg.total_cost_usd is the authoritative total — use this for AG-010
    // msg.modelUsage gives per-model breakdown for multi-model sessions
  }
}
```

#### Per-Model Breakdown

`SDKResultMessage.modelUsage` is a map of model name to `ModelUsage`. For LemonDo's multi-model scenarios (Sonnet for parent, Haiku for subagents):

```typescript
for (const [model, usage] of Object.entries(result.modelUsage)) {
  // model: e.g. "claude-sonnet-4-6", "claude-haiku-4-5"
  // usage.costUSD: cost for that model
  // usage.inputTokens, usage.outputTokens, etc.
}
```

#### Budget Controls (AG-010 Mapping)

The SDK has two native budget controls that map directly to AG-010:

| SDK Option | AG-010 Feature | Behavior When Hit |
|------------|---------------|-------------------|
| `maxTurns: number` | Turn limit | `result.subtype === "error_max_turns"` |
| `maxBudgetUsd: number` | USD ceiling | `result.subtype === "error_max_budget_usd"` |

Both are set in `Options`. When either limit triggers, the SDK emits a final `SDKResultMessage` with the appropriate error subtype before stopping. LemonDo should always set both.

---

### 10A.9 Session Lifecycle

#### Create

```typescript
// Session ID is in the first SDKSystemMessage
for await (const msg of query({ prompt, options })) {
  if (msg.type === "system" && msg.subtype === "init") {
    const sessionId = msg.session_id;  // Store in DB for AG-018 audit trail
  }
}
```

#### Resume

```typescript
// Continue same session (history appended)
query({ prompt: "next task", options: { resume: sessionId } })

// Fork session (new branch; original preserved)
query({ prompt: "alternative approach", options: { resume: sessionId, forkSession: true } })
```

#### Pause/Cancel

- **Interrupt mid-stream**: `query.interrupt()` — stops the current turn without closing the session. The session is resumable.
- **AbortController**: pass `abortController` in options; call `abortController.abort()` to cancel. Session may not be cleanly resumable depending on when abort fires.
- **`maxTurns`**: passive limit that stops the agent after N turns. Cleanly resumable.

#### Session Persistence

Sessions persist on the local filesystem (in the Claude Code CLI's working directory). Default cleanup: 30 days via `cleanupPeriodDays`. Session transcripts are stored separately from the main conversation, so subagent transcripts survive main conversation compaction.

#### V2 API Session Interface (Unstable Preview)

The V2 API simplifies multi-turn sessions from async generator coordination to explicit `send()`/`stream()` pairs:

```typescript
// V2 — simpler multi-turn pattern
import { unstable_v2_createSession, unstable_v2_resumeSession } from "@anthropic-ai/claude-agent-sdk";

await using session = unstable_v2_createSession({ model: "claude-sonnet-4-6" });

// Turn 1
await session.send("Analyze the auth module");
for await (const msg of session.stream()) {
  // same SDKMessage types as V1
}

// Turn 2 (same session, context preserved)
await session.send("Now fix the vulnerabilities you found");
for await (const msg of session.stream()) { ... }

// Resume later
const session2 = unstable_v2_resumeSession(sessionId, { model: "claude-sonnet-4-6" });
```

The `Session` interface:

```typescript
interface Session {
  send(message: string): Promise<void>;
  stream(): AsyncGenerator<SDKMessage>;  // Same message types as V1
  close(): void;
}
```

**V2 limitations vs V1**: session forking (`forkSession`) is V1-only. V2 does not yet support forking. V2 is marked `unstable_v2_*` — APIs may change.

---

### 10A.10 MCP Integration for the LemonDo Agent API

The Agent SDK supports MCP servers as the extension mechanism for giving agents access to custom tools (AG-012, AG-013, AG-014). LemonDo's "Agent API" (the REST endpoints agents can call) should be exposed as an MCP server.

Four MCP transport types are supported:

```typescript
type McpServerConfig =
  | { type?: "stdio"; command: string; args?: string[]; env?: Record<string, string> }
  | { type: "sse"; url: string; headers?: Record<string, string> }
  | { type: "http"; url: string; headers?: Record<string, string> }
  | { type: "sdk"; name: string; instance: McpServer }  // In-process MCP server
```

For LemonDo, the recommended pattern is `type: "http"` pointing at the ASP.NET Core backend's MCP endpoint:

```typescript
// In the Node.js sidecar
query({
  prompt: task.description,
  options: {
    mcpServers: {
      "lemondo": {
        type: "http",
        url: "https://lemondo-api/mcp",  // ASP.NET Core MCP endpoint
        headers: { "Authorization": `Bearer ${agentApiToken}` }
      }
    }
  }
})
```

The SDK also supports in-process MCP servers via `createSdkMcpServer()` and the `tool()` helper, which could be used for lightweight callbacks that don't require HTTP round-trips.

---

### 10A.11 .NET Interop Patterns

The Agent SDK has no .NET port. Two viable patterns for .NET to .NET/Node.js integration:

#### Pattern A: Node.js Sidecar Process (Recommended)

The .NET API spawns a Node.js process that runs the Agent SDK. Communication is over stdin/stdout or a local HTTP/WebSocket server.

```
.NET API (ASP.NET Core)
  → spawn Node.js sidecar process (per session or per pool)
    → runs @anthropic-ai/claude-agent-sdk
      → spawns Claude Code CLI subprocess
        → calls api.anthropic.com

Output flow:
  CLI → SDK (stdout JSON) → Node.js sidecar (classifies messages) → .NET (SSE or WebSocket)
```

The Node.js sidecar's responsibilities:
1. Accept session start command from .NET (via stdin or HTTP)
2. Run `query()` with the provided options
3. Classify each `SDKMessage` and forward to .NET over SSE or WebSocket
4. Handle lifecycle commands from .NET (interrupt, abort)

This is the pattern the Anthropic hosting guide recommends ("expose HTTP/WebSocket endpoints for external clients while the SDK runs internally within the container").

#### Pattern B: WebSocket Bridge (community project `claude-agent-server`)

An existing community project (`github.com/dzhng/claude-agent-server`) wraps the Agent SDK in a WebSocket server. The .NET backend connects as a WebSocket client.

Protocol:
- Client → Server: `{ type: "user_message", data: SDKUserMessage }` or `{ type: "interrupt" }`
- Server → Client: `{ type: "connected" }`, `{ type: "sdk_message", data: SDKMessage }`, `{ type: "error", data: string }`

.NET can consume this with `System.Net.WebSockets.ClientWebSocket`. The bridge approach is simpler to start with but adds a network hop and a third-party dependency.

#### Pattern Comparison

| Pattern | Latency | Complexity | Control | Recommendation |
|---------|---------|------------|---------|----------------|
| Node.js sidecar (stdin/stdout) | Lowest | Medium | Full | Use for production |
| Node.js sidecar (local HTTP/SSE) | Low | Medium | Full | Acceptable alternative |
| WebSocket bridge (community) | Low | Low | Partial | Prototyping only |

---

### 10A.12 Permission Modes

```typescript
type PermissionMode =
  | "default"            // canUseTool callback required — use for AG-019 human approval
  | "acceptEdits"        // Auto-approve file edits, ask for other actions
  | "bypassPermissions"  // No prompts — use for headless CI/CD agents
  | "plan"               // No execution — agent produces a plan only
```

For LemonDo's supervised mode (AG-019): use `default` + `canUseTool` callback that sends a permission request to the frontend via SSE, waits for user response, then returns `allow` or `deny`.

For LemonDo's automated mode (S-AG-01 batch development): use `bypassPermissions` with `allowedTools` allowlist and `maxBudgetUsd` guard.

---

### 10A.13 Known Issues and Risks (SDK-Specific)

| Issue | Detail | Impact on LemonDo |
|-------|--------|-------------------|
| V2 API is unstable | All `unstable_v2_*` exports may have breaking changes before stabilization | Use V1 `query()` for production; adopt V2 only after it stabilizes |
| Release velocity | SDK updates near-daily to track CLI parity; semver minor bumps may include behavior changes | Pin versions in production; test updates before deploying |
| Streaming + thinking mutually exclusive | `maxThinkingTokens` disables `stream_event` messages | Choose one or the other per session; cannot do both |
| Subagent nesting depth = 1 | Subagents cannot spawn subagents | S-AG-01 parallel architecture must stay at depth 1 |
| Windows long prompt failure | Subagents with very long prompts fail on Windows due to 8191 char CLI limit | Not relevant for Linux containers on Azure Container Apps |
| Session persistence is local filesystem | Sessions are stored on the host where the CLI runs | Node.js sidecar must run on persistent storage; ephemeral containers lose session history |
| Background subagent premature stream close | Fixed in v0.2.45 — earlier versions closed the stream before background subagents finished | Stay on 0.2.45+ |
| No built-in retry on 429 | SDK does not auto-retry on rate limit errors | Implement retry with exponential backoff in the Node.js sidecar |

---

### 10A.14 ACL Design Implications

Bruno's observations are confirmed by the documentation:

1. **Tool results appear as user messages** — Confirmed. `SDKUserMessage` with `content[].type === "tool_result"` is the mechanism. The wire format follows the Anthropic Messages API pattern where tool results are user-turn content.

2. **Subagent messages mixed with parent messages** — Confirmed. All messages from all depths flow through a single `AsyncGenerator`. The `parent_tool_use_id` field is the only way to route them. The ACL must maintain a routing table: `parent_tool_use_id → subagent session entity`.

3. **SDK is an event stream of typed events** — Confirmed. The union type `SDKMessage` with 7 variants is the canonical representation.

**Recommended ACL responsibilities for the Node.js sidecar:**

| Raw SDK Type | ACL Domain Event |
|-------------|-----------------|
| `{ type: "system", subtype: "init" }` | `AgentSessionStarted { sessionId, model, tools, cwd }` |
| `{ type: "assistant", parent_tool_use_id: null }` | `AgentThought { text }` or `AgentInvokedTool { toolName, toolInput }` |
| `{ type: "assistant", parent_tool_use_id: X }` | `SubagentThought { parentToolUseId: X, text }` |
| `{ type: "user", content[].type: "tool_result" }` | `ToolExecuted { toolName, toolUseId, output, isError }` |
| `{ type: "result", subtype: "success" }` | `AgentSessionCompleted { totalCostUsd, modelUsage, numTurns }` |
| `{ type: "result", subtype: "error_max_budget_usd" }` | `AgentSessionBudgetExceeded { totalCostUsd }` |
| `{ type: "result", subtype: "error_max_turns" }` | `AgentSessionTurnLimitReached { numTurns }` |
| `{ type: "result", subtype: "error_during_execution" }` | `AgentSessionFailed { errors }` |
| `{ type: "stream_event" }` | `AgentStreamToken { text }` (forwarded as SSE to frontend) |
| `{ type: "system", subtype: "compact_boundary" }` | `AgentContextCompacted { preTokens, trigger }` |

---

### 10A.15 References (Agent SDK Deep Dive)

- [Claude Agent SDK TypeScript Reference (V1)](https://platform.claude.com/docs/en/agent-sdk/typescript)
- [Claude Agent SDK V2 Preview](https://platform.claude.com/docs/en/agent-sdk/typescript-v2-preview)
- [Streaming Output Guide](https://platform.claude.com/docs/en/agent-sdk/streaming-output)
- [Subagents Guide](https://platform.claude.com/docs/en/agent-sdk/subagents)
- [Session Management Guide](https://platform.claude.com/docs/en/agent-sdk/sessions)
- [Cost Tracking Guide](https://platform.claude.com/docs/en/agent-sdk/cost-tracking)
- [Hosting Guide](https://platform.claude.com/docs/en/agent-sdk/hosting)
- [Claude Agent SDK CHANGELOG.md](https://github.com/anthropics/claude-agent-sdk-typescript/blob/main/CHANGELOG.md)
- [claude-agent-server (WebSocket bridge, community)](https://github.com/dzhng/claude-agent-server)
- [Claude Agent SDK npm package](https://www.npmjs.com/package/@anthropic-ai/claude-agent-sdk)
- [Agent SDK Demos (official examples)](https://github.com/anthropics/claude-agent-sdk-demos)
