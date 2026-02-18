# Implementation Checkpoints

> **Source**: Phase 3 planning — synthesized from all domain contexts, scenarios, and NFRs
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Module Dependency Analysis

The ordering of checkpoints is driven by the upstream/downstream relationships across bounded contexts. No checkpoint may require a context that has not been delivered in a prior checkpoint.

```
Identity (v1) ─────────────────────────────────────────────────────────┐
Task (v1)     ─────────────────────────────────┬───────────────────────┤
Board (v1)    ─────────────────────────────────┤                       │
                                                │                       │
         CP6                  CP7              CP7           CP8/CP9    │
     ┌──────────┐        ┌──────────┐    ┌──────────┐    ┌──────────┐  │
     │ Projects │◄───────│ People   │    │ProjectTask│    │  Agents  │◄─┘
     │ (git,    │        │ (CRM)    │    │  Bridge  │    │ (sessions│
     │  worktrees│        └──────────┘    └──────────┘    │  sidecar)│
     │  servers)│                                         └──────────┘
     └──────────┘                                              │
          │                                                    │
          │            CP9                    CP9              │
          │     ┌─────────────────┐   ┌─────────────────┐     │
          └────►│ ProjectAgent    │   │  AgentTask      │◄────┘
                │ Bridge          │   │  Bridge         │
                │ (WorkQueue,     │   │ (task↔session   │
                │  correlations)  │   │  correlation)   │
                └─────────────────┘   └─────────────────┘
                         │
                    CP10 ▼
                ┌──────────────┐
                │    Comms     │
                │ (Gmail first,│
                │  then others)│
                └──────────────┘
                         │
                    CP11 ▼
                ┌──────────────────────┐
                │  Integration Polish  │
                │  (cross-module UI,   │
                │   perf, prod-harden) │
                └──────────────────────┘
```

### Ordering Rationale

| Order | Module | Reason |
|-------|--------|--------|
| CP6 first | Projects | Foundation for worktree-based agent work; People and Agents both reference `ProjectId`; devs need repos registered before agents run in them |
| CP7 second | People + ProjectTaskBridge | People is simple CRUD with no hard dependencies; needed by Comms for auto-linking. ProjectTaskBridge only needs Projects + Tasks (v1) |
| CP8 third | Agent Core | Foundational session lifecycle, sidecar, Redis Streams; must exist before bridges can be built |
| CP9 fourth | Agent Intelligence + Bridges | Skills, chains, special tool calls, and both bridge contexts require Agent Core to be stable |
| CP10 fifth | Comms | Depends on People (auto-linking), Projects (CommLink weak ref), and Notification (v1) |
| CP11 sixth | Integration Polish | Full cross-module workflows, production hardening, performance; all contexts must already exist |

---

## Technology Spikes

These spikes carry implementation risk and must be completed before the blocking checkpoint begins. Each spike produces a working proof-of-concept committed to the `spike/` branch prefix.

| ID | Spike | Blocks | Risk | Effort | Acceptance Criteria |
|----|-------|--------|------|--------|---------------------|
| SK-01 | simple-git worktree operations (Node.js) | CP6 | Medium | S | `worktree add`, `worktree remove`, `worktree list`, status polling all work from a Node.js script in a real repo |
| SK-02 | ngrok REST API + .NET SDK tunnel lifecycle | CP6 | Low | S | Programmatic tunnel create/destroy, URL retrieval, and status polling verified against ngrok Hobbyist plan |
| SK-03 | Redis Streams XADD/XREAD with StackExchange.Redis + node-redis bidirectional | CP8 | Medium | M | .NET writes to command stream; Node.js reads and acks; Node.js writes to event stream; .NET reads; both ends verified with consumer groups and at-least-once delivery |
| SK-04 | Claude Agent SDK sidecar process lifecycle (Node.js spawn + graceful stop) | CP8 | High | M | Node.js process spawns, connects to Agent SDK, streams events to Redis, receives commands from Redis, terminates cleanly; budget cap termination tested |
| SK-05 | SignalR Hub → SSE bridge for real-time session output streaming | CP8 | Medium | M | React client receives streaming session log lines via SignalR Hub that bridges from Redis Stream; 200ms latency target met |
| SK-06 | Gmail OAuth2 + Push Notifications (Pub/Sub) | CP10 | Low | S | OAuth2 flow completes; Pub/Sub webhook receives new-mail events; thread read and normalized to domain Thread |
| SK-07 | Baileys (WhatsApp) stability and ToS risk assessment | CP10 | High | M | Spike determines: session stability under 72h continuous operation, account ban risk, and whether to include or defer; produces a go/no-go recommendation |
| SK-08 | Discord.Net Gateway WebSocket DM + server channel monitoring | CP10 | Low | S | Bot receives DMs and server messages; messages normalized to Thread/Message domain objects |

---

## Checkpoint Overview

| Checkpoint | Name | Module(s) | Key Deliverable | New Tests Target |
|------------|------|-----------|-----------------|-----------------|
| CP6 | Foundation & Projects | Projects | Git repo registration, worktrees, dev servers, ngrok tunnels | +180 tests |
| CP7 | People & Relationships | People, ProjectTaskBridge | Person/company CRM, project-task link auto-complete | +130 tests |
| CP8 | Agent Core | Agents | Session lifecycle, Redis sidecar, real-time output streaming | +200 tests |
| CP9 | Agent Intelligence | Agents (skills/chains), ProjectAgentBridge, AgentTaskBridge | WorkQueue, SessionChain, special tool calls, bridge workflows | +180 tests |
| CP10 | Communications | Comms | Unified inbox, Gmail, Discord, Slack adapters | +160 tests |
| CP11 | Integration & Polish | All modules | Cross-module workflows, production hardening, perf | +60 tests |

---

## CP6: Foundation & Projects

### Scope

Full implementation of the Projects bounded context: project registration, git worktree management, dev server process control, and ngrok tunnel integration. Establishes the v2 Aspire infrastructure additions (Redis container, Node.js sidecar scaffold).

### Prerequisites

- v1 codebase at production state (CP5 complete, all tests passing)
- Spikes SK-01 and SK-02 complete with go recommendations
- Redis Streams research (SK-03 can be in-flight — not needed until CP8)

### Tasks

| ID | Task | Layer | Context | Effort | Parallel? |
|----|------|-------|---------|--------|-----------|
| CP6-001 | Add Redis container to Aspire AppHost (local) + Azure Managed Redis config (prod) | Infrastructure | Infra | S | No |
| CP6-002 | Shared kernel: add `ProjectId`, `WorktreeId` Guid wrapper value objects | Domain | Shared | S | Yes |
| CP6-003 | Project aggregate root + domain events + invariants | Domain | Projects | M | Yes |
| CP6-004 | Worktree aggregate root + domain events + invariants | Domain | Projects | M | Yes |
| CP6-005 | DevServer aggregate root + domain events + invariants | Domain | Projects | M | Yes |
| CP6-006 | Tunnel aggregate root + domain events + invariants | Domain | Projects | S | Yes |
| CP6-007 | IProjectRepository + IWorktreeRepository + IDevServerRepository + ITunnelRepository interfaces | Domain | Projects | S | Yes |
| CP6-008 | IGitService ACL port (worktree add/remove/list/status/refresh, commit, log) | Domain | Projects | S | Yes |
| CP6-009 | IProcessService ACL port (start/stop/restart process, status poll) | Domain | Projects | S | Yes |
| CP6-010 | ITunnelService ACL port (create/destroy tunnel, get URL, health check) | Domain | Projects | S | Yes |
| CP6-011 | Register project use case: scan repo, detect tech stack, doc files, create aggregate | Application | Projects | M | No |
| CP6-012 | Update project metadata use case + archive/unarchive | Application | Projects | S | Yes |
| CP6-013 | Refresh project snapshot use case (re-scan, update commit SHA, branch count) | Application | Projects | S | Yes |
| CP6-014 | Link/unlink task to project use cases | Application | Projects | S | Yes |
| CP6-015 | Link/unlink person to project use cases | Application | Projects | S | Yes |
| CP6-016 | Create worktree use case (dispatch to IGitService, record WorktreeCreatedEvent) | Application | Projects | M | No |
| CP6-017 | Delete worktree use case (MarkDeleted + IGitService remove) | Application | Projects | S | Yes |
| CP6-018 | Refresh worktree status use case (polling handler) | Application | Projects | S | Yes |
| CP6-019 | Start/stop/restart dev server use cases (IProcessService) | Application | Projects | M | No |
| CP6-020 | Create/destroy ngrok tunnel use cases (ITunnelService) | Application | Projects | M | No |
| CP6-021 | EF Core entity configurations + migration for Projects, Worktrees, DevServers, Tunnels | Infrastructure | Projects | M | No |
| CP6-022 | EF Core repository implementations for all four aggregates | Infrastructure | Projects | M | Yes |
| CP6-023 | simple-git IGitService implementation (Node.js sidecar or direct .NET shell wrapper) | Infrastructure | Projects | L | No |
| CP6-024 | IProcessService implementation (.NET `Process` API + health polling background service) | Infrastructure | Projects | M | No |
| CP6-025 | ITunnelService implementation (ngrok REST API client) | Infrastructure | Projects | M | No |
| CP6-026 | Projects API controller (CRUD + worktree + dev server + tunnel endpoints) | Presentation | Projects | M | Yes |
| CP6-027 | Projects list page + project card UI component | Frontend | Projects | M | Yes |
| CP6-028 | Project detail page: metadata, tech stack tags, doc file viewer | Frontend | Projects | M | Yes |
| CP6-029 | Worktree management UI: list, create, delete, status indicators | Frontend | Projects | M | Yes |
| CP6-030 | Dev server controls UI: start/stop/restart, port badge, log tail | Frontend | Projects | M | Yes |
| CP6-031 | Ngrok tunnel UI: expose button, shareable URL display, status | Frontend | Projects | S | Yes |
| CP6-032 | Domain unit tests for all four aggregates (invariants, state transitions, event publication) | Tests | Projects | L | Yes |
| CP6-033 | Application handler integration tests (project registration, worktree create, dev server start) | Tests | Projects | M | Yes |
| CP6-034 | E2E: register project, create worktree, start dev server (Playwright) | Tests | Projects | M | No |

### Verification Gate

```
./dev verify
  ✓ Backend: all existing tests + CP6 domain + application + integration tests pass
  ✓ Frontend: all existing tests + CP6 component tests pass
  ✓ E2E: project registration flow, worktree creation, dev server start/stop
  ✓ Lint + TypeScript type check clean
  ✓ Build: Docker image builds without warnings
  ✓ Manual: register lemon-todo-v2 repo, create a worktree, start the Vite dev server, expose via ngrok
```

### Key Deliverables

- Projects module navigable from the main nav
- At least one real repository registered and displaying tech stack, branch info, and doc files
- Worktrees creatable and deletable from the UI with live status badges
- Dev server start/stop working against a real process
- Ngrok tunnel create/destroy working against the Hobbyist API

---

## CP7: People & Relationships

### Scope

Full implementation of the People bounded context (persons, companies, notes, preferences, tags, project links) plus the ProjectTaskBridge context (project-to-task links and auto-complete-on-merge behaviour).

### Prerequisites

- CP6 complete and verified
- Spike SK-01 complete (needed for ProjectTaskBridge worktree-merge trigger)

### Tasks

| ID | Task | Layer | Context | Effort | Parallel? |
|----|------|-------|---------|--------|-----------|
| CP7-001 | Shared kernel: add `PersonId` Guid wrapper value object | Domain | Shared | S | Yes |
| CP7-002 | Person aggregate root + domain events + invariants | Domain | People | M | Yes |
| CP7-003 | Company aggregate root + domain events + invariants | Domain | People | M | Yes |
| CP7-004 | Note entity + value objects (PersonName, PersonBio, ContactHandle, etc.) | Domain | People | M | Yes |
| CP7-005 | IPersonRepository + ICompanyRepository interfaces | Domain | People | S | Yes |
| CP7-006 | Create / update / archive person use cases | Application | People | M | No |
| CP7-007 | Add/remove email, phone contact handle use cases + PersonContactHandleAddedEvent publication | Application | People | S | Yes |
| CP7-008 | Add/remove tag, important date, note use cases | Application | People | S | Yes |
| CP7-009 | Link/unlink person ↔ company use cases | Application | People | S | Yes |
| CP7-010 | Link/unlink person ↔ project use cases | Application | People | S | Yes |
| CP7-011 | Create / update / archive company use cases | Application | People | M | Yes |
| CP7-012 | EF Core entity configurations + migration for Person, Company, Note | Infrastructure | People | M | No |
| CP7-013 | EF Core repository implementations for Person and Company | Infrastructure | People | M | Yes |
| CP7-014 | People API controller (CRUD for persons and companies, contact handles, notes, tags) | Presentation | People | M | Yes |
| CP7-015 | ProjectTaskLink aggregate + domain events + invariants (ProjectTaskBridge) | Domain | ProjectTaskBridge | S | Yes |
| CP7-016 | IProjectTaskLinkRepository interface (ProjectTaskBridge) | Domain | ProjectTaskBridge | S | Yes |
| CP7-017 | Event handler: TaskLinkedToProjectEvent → create ProjectTaskLink | Application | ProjectTaskBridge | S | Yes |
| CP7-018 | Event handler: TaskUnlinkedFromProjectEvent → deactivate ProjectTaskLink | Application | ProjectTaskBridge | S | Yes |
| CP7-019 | Event handler: WorktreeDeletedEvent → auto-complete tasks where AutoCompleteOnMerge = true | Application | ProjectTaskBridge | M | No |
| CP7-020 | EF Core config + migration for ProjectTaskLink | Infrastructure | ProjectTaskBridge | S | Yes |
| CP7-021 | Cross-context query: GetTasksForProject (reads Task context via ITaskReadService port) | Application | ProjectTaskBridge | M | No |
| CP7-022 | People list page + person card UI component | Frontend | People | M | Yes |
| CP7-023 | Person detail page: profile, contact handles, tags, notes, company memberships, project links | Frontend | People | M | Yes |
| CP7-024 | Company detail page: profile, members, project links, notes | Frontend | People | M | Yes |
| CP7-025 | Project detail: show linked people from Projects page (cross-module read) | Frontend | Projects | S | Yes |
| CP7-026 | Task board: filter by project (uses ProjectTaskBridge cross-context query) | Frontend | Board | S | Yes |
| CP7-027 | Domain unit tests for Person, Company, ProjectTaskLink aggregates | Tests | People/Bridge | L | Yes |
| CP7-028 | Integration tests: person create/contact/company link, ProjectTaskLink auto-complete flow | Tests | People/Bridge | M | Yes |
| CP7-029 | E2E: add person with handles, link to project, verify ProjectTaskBridge auto-complete (Playwright) | Tests | People/Bridge | M | No |

### Verification Gate

```
./dev verify
  ✓ Backend: all CP6 tests + CP7 domain + application + integration tests pass
  ✓ Frontend: all CP6 tests + CP7 component tests pass
  ✓ E2E: person creation, company link, project filter by person, task auto-complete on merge
  ✓ Event handler: PersonContactHandleAddedEvent fires when email added to person record
  ✓ ProjectTaskBridge: task linked to project shows in board filter; task auto-completes on worktree delete
  ✓ Lint + TypeScript type check clean
```

### Key Deliverables

- People module navigable from the main nav (persons list, detail, company detail)
- Person contact handles trigger events consumed by the (future) Comms context
- Task board filterable by project using ProjectTaskBridge cross-context query
- Worktree deletion auto-completes linked tasks where configured

---

## CP8: Agent Core

### Scope

Core Agent Session lifecycle: start/pause/resume/cancel, sidecar architecture, Redis Streams bidirectional communication, real-time session output streaming to the UI, budget enforcement, SessionPool, AgentTemplate, and the Agent API (per-session API key auth).

This checkpoint deliberately excludes: SessionChain, AgentSkill, WorkQueue/bridges, special tool calls. Those are CP9.

### Prerequisites

- CP6 complete and verified (worktrees must exist for agents to run in them)
- Spikes SK-03, SK-04, SK-05 complete with go recommendations
- Shared kernel: `AgentSessionId` Guid wrapper added

### Tasks

| ID | Task | Layer | Context | Effort | Parallel? |
|----|------|-------|---------|--------|-----------|
| CP8-001 | Shared kernel: add `AgentSessionId` Guid wrapper value object | Domain | Shared | S | Yes |
| CP8-002 | AgentSession aggregate root (status machine, budget, metrics, output, HasPendingReview) | Domain | Agents | L | No |
| CP8-003 | AgentTemplate aggregate root + domain events + invariants | Domain | Agents | M | Yes |
| CP8-004 | SessionMessageQueue aggregate root + domain events | Domain | Agents | M | Yes |
| CP8-005 | SessionPool domain service (concurrency cap, allocate/release) | Domain | Agents | M | Yes |
| CP8-006 | IAgentSessionRepository + IAgentTemplateRepository interfaces | Domain | Agents | S | Yes |
| CP8-007 | IAgentRuntime ACL port (StartSessionAsync, StopSessionAsync, SendSteeringMessage, etc.) | Domain | Agents | S | Yes |
| CP8-008 | IAgentRuntimeEventConsumer ACL port (background service interface) | Domain | Agents | S | Yes |
| CP8-009 | Start agent session use case (pool check, session start, allocate, dispatch to IAgentRuntime) | Application | Agents | M | No |
| CP8-010 | Pause/resume/cancel/interrupt use cases (write commands to Redis via IAgentRuntime) | Application | Agents | M | Yes |
| CP8-011 | Approve output use case (AgentSession.ApproveOutput → AgentSessionApprovedEvent) | Application | Agents | S | Yes |
| CP8-012 | Reject output use case (AgentSession.RejectOutput → AgentSessionRejectedEvent) | Application | Agents | S | Yes |
| CP8-013 | Retry session use case | Application | Agents | S | Yes |
| CP8-014 | Agent API: create task endpoint (POST /api/agent/tasks) with per-session API key auth | Application | Agents | M | No |
| CP8-015 | Agent API: log session output endpoint (POST /api/agent/output) | Application | Agents | S | Yes |
| CP8-016 | Budget enforcement: UpdateMetrics handler with 80% warning and hard cap termination | Application | Agents | M | No |
| CP8-017 | Session output streaming: SignalR Hub that bridges Redis Stream → SSE to React | Infrastructure | Agents | L | No |
| CP8-018 | IAgentRuntime implementation: spawn Node.js sidecar process, write command stream | Infrastructure | Agents | L | No |
| CP8-019 | Node.js sidecar entry point: Agent SDK init, event → Redis XADD, command XREAD loop | Infrastructure | Agents | L | No |
| CP8-020 | IAgentRuntimeEventConsumer implementation: Redis XREAD background service, ACL event → domain command dispatch | Infrastructure | Agents | L | No |
| CP8-021 | EF Core entity configurations + migration for AgentSession, AgentTemplate, SessionMessageQueue | Infrastructure | Agents | M | No |
| CP8-022 | EF Core repository implementations for AgentSession and AgentTemplate | Infrastructure | Agents | M | Yes |
| CP8-023 | Agent Sessions API controller (CRUD + lifecycle endpoints + Agent API sub-namespace) | Presentation | Agents | M | Yes |
| CP8-024 | Agents dashboard: session card grid, status badges, live token/cost tracker | Frontend | Agents | M | Yes |
| CP8-025 | Session detail panel: real-time log stream (SignalR), task list, budget indicator | Frontend | Agents | L | No |
| CP8-026 | Start session modal: objective input, model selector, working directory picker, budget config | Frontend | Agents | M | Yes |
| CP8-027 | Approve/reject review panel: output summary, files changed, tests added, approve/reject buttons | Frontend | Agents | M | Yes |
| CP8-028 | Budget alert notifications (80% warning, hard cap exhausted) → Notification context | Application | Agents | S | Yes |
| CP8-029 | Domain unit tests for AgentSession state machine (all status transitions, invariant violations) | Tests | Agents | L | Yes |
| CP8-030 | Domain property tests for AgentSession budget invariants (fast-check) | Tests | Agents | M | Yes |
| CP8-031 | Integration tests: session start → running → output → approve flow | Tests | Agents | M | No |
| CP8-032 | Integration tests: Redis command stream → sidecar → event stream → domain event dispatch | Tests | Agents | L | No |
| CP8-033 | E2E: start a real agent session, view live output, approve (Playwright) | Tests | Agents | M | No |

### Verification Gate

```
./dev verify
  ✓ Backend: all CP7 tests + CP8 domain/application/integration tests pass
  ✓ Frontend: all CP7 tests + CP8 component tests pass
  ✓ E2E: start agent session, watch real-time output, approve output
  ✓ Budget: session auto-pauses at hard cap; 80% warning notification fires
  ✓ API key auth: Agent API endpoints reject requests without valid per-session key
  ✓ Redis Streams: bidirectional flow verified under load (100 events/s for 60s)
  ✓ SignalR streaming: session log lines arrive in browser within 200ms of Redis publish
  ✓ SessionPool: concurrent session cap enforced; queue message shown when cap reached
  ✓ Lint + TypeScript type check clean
```

### Key Deliverables

- Agents dashboard navigable from main nav
- Agent session startable from UI (with working directory, objective, budget)
- Real-time log streaming visible in session detail panel
- Approve/reject review flow functional
- Budget cap enforcement with notifications
- Node.js sidecar process managed by the .NET API lifecycle
- Agent API endpoints authenticated and functional

---

## CP9: Agent Intelligence & Bridge Contexts

### Scope

AgentSkill (skills, memory pills, hot-loading), SessionChain (context-window handoff), special tool calls (AskUserQuestion, TodoWrite, PlanMode), auto-continue mode, ProjectAgentBridge (WorkQueue, worktree correlation), AgentTaskBridge (task completion on approval).

### Prerequisites

- CP8 complete and verified
- CP7 complete (ProjectTaskBridge exists; Tasks context available for AgentTaskBridge)

### Tasks

| ID | Task | Layer | Context | Effort | Parallel? |
|----|------|-------|---------|--------|-----------|
| CP9-001 | AgentSkill aggregate root + MemoryPill entity + domain events | Domain | Agents | M | Yes |
| CP9-002 | IAgentSkillRepository interface | Domain | Agents | S | Yes |
| CP9-003 | SessionChain aggregate root + HandoffDocument entity + domain events | Domain | Agents | M | Yes |
| CP9-004 | ISessionChainRepository interface | Domain | Agents | S | Yes |
| CP9-005 | Special tool call handlers in ACL: AskUserQuestion → WaitingForInput, TodoWrite → SessionTaskList, EnterPlanMode/ExitPlanMode → PlanMode flow | Application | Agents | L | No |
| CP9-006 | AnswerUserQuestion use case (transitions WaitingForInput → Running) | Application | Agents | S | Yes |
| CP9-007 | ApprovePlan / RejectPlan use cases (WaitingForApproval flow) | Application | Agents | S | Yes |
| CP9-008 | Auto-continue config use cases: enable/disable, configure validation loop | Application | Agents | M | No |
| CP9-009 | Enable/disable skill + RequestReload use cases (hot-loading via reload_config command) | Application | Agents | M | No |
| CP9-010 | Voluntary handoff use case (agent calls POST /api/agent/handoff) | Application | Agents | S | Yes |
| CP9-011 | SessionChain: initiate handoff use case, attach new session, complete chain | Application | Agents | M | No |
| CP9-012 | EF Core config + migration for AgentSkill, MemoryPill, SessionChain | Infrastructure | Agents | M | No |
| CP9-013 | EF Core repository implementations for AgentSkill and SessionChain | Infrastructure | Agents | M | Yes |
| CP9-014 | Skill management API endpoints (CRUD, attach to session, memory pill CRUD) | Presentation | Agents | M | Yes |
| CP9-015 | Session chain API endpoints + handoff document endpoints | Presentation | Agents | S | Yes |
| CP9-016 | Shared kernel: add `AgentSessionId` to v2 published vocabulary if not already done in CP8 | Domain | Shared | S | Yes |
| CP9-017 | AgentProjectCorrelation aggregate + domain events (ProjectAgentBridge) | Domain | ProjectAgentBridge | M | Yes |
| CP9-018 | WorkQueue + WorkItem aggregates + domain events (ProjectAgentBridge) | Domain | ProjectAgentBridge | L | No |
| CP9-019 | IAgentProjectCorrelationRepository + IWorkQueueRepository (ProjectAgentBridge) | Domain | ProjectAgentBridge | S | Yes |
| CP9-020 | Event handler: AgentSessionStartedEvent → create AgentProjectCorrelation, dispatch CreateWorktreeCommand | Application | ProjectAgentBridge | M | No |
| CP9-021 | Event handler: WorktreeCreatedEvent → activate correlation, dispatch StartAgentSessionCommand with resolved WorkingDirectory | Application | ProjectAgentBridge | M | No |
| CP9-022 | Event handler: AgentSessionApprovedEvent → dispatch merge (MarkDeleted worktree) | Application | ProjectAgentBridge | M | No |
| CP9-023 | Event handler: AgentSessionFailedEvent / SessionCancelledEvent → fail/cancel correlation | Application | ProjectAgentBridge | S | Yes |
| CP9-024 | WorkQueue orchestration use cases: create queue, enqueue items, process next item, complete/fail items | Application | ProjectAgentBridge | L | No |
| CP9-025 | EF Core config + migration for AgentProjectCorrelation, WorkQueue, WorkItem | Infrastructure | ProjectAgentBridge | M | No |
| CP9-026 | ProjectAgentBridge API endpoints (WorkQueue CRUD, correlation status) | Presentation | ProjectAgentBridge | M | Yes |
| CP9-027 | AgentTaskCorrelation aggregate + domain events (AgentTaskBridge) | Domain | AgentTaskBridge | M | Yes |
| CP9-028 | IAgentTaskCorrelationRepository (AgentTaskBridge) | Domain | AgentTaskBridge | S | Yes |
| CP9-029 | Event handler: AgentSessionStartedEvent → create AgentTaskCorrelation | Application | AgentTaskBridge | S | Yes |
| CP9-030 | Event handler: AgentSessionApprovedEvent → dispatch CompleteTaskCommand to Tasks context | Application | AgentTaskBridge | M | No |
| CP9-031 | Event handler: AgentSessionRejected/Failed → mark correlation Failed (task not completed) | Application | AgentTaskBridge | S | Yes |
| CP9-032 | Event handler: AgentApiTaskCreatedEvent → record follow-up task on correlation | Application | AgentTaskBridge | S | Yes |
| CP9-033 | EF Core config + migration for AgentTaskCorrelation | Infrastructure | AgentTaskBridge | S | Yes |
| CP9-034 | "Start Agents" multi-task batch UI: WorkQueue config panel, execution mode, budget per session | Frontend | Agents/Bridges | L | No |
| CP9-035 | Session detail: AskUserQuestion card, TodoWrite task list, plan mode approval panel | Frontend | Agents | M | Yes |
| CP9-036 | Session chain UI: chain visualization, handoff document viewer, continue-in-new-session button | Frontend | Agents | M | Yes |
| CP9-037 | Skill management UI: skill list, create/edit skill, memory pills list, attach to session | Frontend | Agents | M | Yes |
| CP9-038 | WorkQueue dashboard: queue progress, item statuses, budget tracker | Frontend | ProjectAgentBridge | M | Yes |
| CP9-039 | Task board: "Assign to Agent" button → start session with task as objective | Frontend | Agents/Tasks | M | No |
| CP9-040 | Domain tests for AgentSkill, SessionChain, WorkQueue, AgentProjectCorrelation, AgentTaskCorrelation | Tests | All | L | Yes |
| CP9-041 | Integration tests: batch agent start → WorkQueue → worktree creation → session start flow | Tests | Bridges | L | No |
| CP9-042 | Integration tests: session approved → task completed via AgentTaskBridge | Tests | Bridges | M | No |
| CP9-043 | E2E: select 3 tasks, start parallel agent batch, approve one, verify task auto-completion (Playwright) | Tests | All | L | No |

### Verification Gate

```
./dev verify
  ✓ Backend: all CP8 tests + CP9 domain/application/integration tests pass
  ✓ Frontend: all CP8 tests + CP9 component tests pass
  ✓ E2E: 3-task parallel batch → WorkQueue → agents run → approve → tasks auto-complete
  ✓ AskUserQuestion: session transitions to WaitingForInput; UI shows question card; answer resumes session
  ✓ TodoWrite: session task list updates live in UI without status change
  ✓ PlanMode: plan created in WaitingForApproval; approve continues; reject re-runs agent
  ✓ SessionChain: context-exhausted session produces handoff document; new session picks up
  ✓ Skill hot-load: adding skill to idle session triggers reload_config; sidecar acks
  ✓ AgentTaskBridge: approved session marks linked task completed in Task context
  ✓ Lint + TypeScript type check clean
```

### Key Deliverables

- Batch "Start Agents" from task board with WorkQueue UI
- Special tool calls (AskUserQuestion, TodoWrite, PlanMode) visible as interactive UI elements
- Session chain handoff working end-to-end
- AgentProjectBridge auto-creates/merges worktrees on session lifecycle
- AgentTaskBridge auto-completes tasks on session approval
- Skill library with memory pills and hot-loading

---

## CP10: Communications

### Scope

Full Comms bounded context: Channel management (Gmail, Discord, Slack, optionally WhatsApp pending SK-07), unified Thread inbox, message read/reply, priority management, cross-module linking (to projects, tasks, people), Notification context event integration for high-priority messages.

### Prerequisites

- CP7 complete (People context exists; PersonContactHandleAddedEvent subscription needed for auto-linking)
- CP8 complete (Notification context v1 exists for push alerts)
- Spikes SK-06, SK-07, SK-08 complete
- Slack API research complete (SlackNet SDK confirmed)

### Tasks

| ID | Task | Layer | Context | Effort | Parallel? |
|----|------|-------|---------|--------|-----------|
| CP10-001 | Channel aggregate root + ChannelCredential shadow property pattern + domain events | Domain | Comms | M | Yes |
| CP10-002 | Thread aggregate root + Message entity + CommLink value object + domain events | Domain | Comms | M | Yes |
| CP10-003 | IChannelRepository + IThreadRepository interfaces | Domain | Comms | S | Yes |
| CP10-004 | IChannelAdapter ACL port (connect, disconnect, sync, health check, reply) | Domain | Comms | S | Yes |
| CP10-005 | ICommsReadService ACL port (for People context to read linked message summaries) | Domain | Comms | S | Yes |
| CP10-006 | Connect/disconnect channel use cases | Application | Comms | M | No |
| CP10-007 | Sync messages use case (call adapter, normalize to Thread/Message, persist) | Application | Comms | M | No |
| CP10-008 | Mark read/unread, snooze, set priority use cases | Application | Comms | S | Yes |
| CP10-009 | Reply to thread use case (call adapter.Reply()) | Application | Comms | S | Yes |
| CP10-010 | Link thread to task/project/person use cases (CommLink — no validation) | Application | Comms | S | Yes |
| CP10-011 | Event handler: PersonCreatedEvent → register email handles for auto-linking | Application | Comms | S | Yes |
| CP10-012 | Event handler: PersonContactHandleAddedEvent → register new handle for auto-linking | Application | Comms | S | Yes |
| CP10-013 | AI priority suggestion use case (call IAiPriorityService ACL, store advisory score) | Application | Comms | M | No |
| CP10-014 | High-priority message notification: MessageReceivedEvent (priority >= High) → Notification context | Application | Comms | S | Yes |
| CP10-015 | EF Core entity configurations + migration for Channel, Thread, Message, CommLink | Infrastructure | Comms | M | No |
| CP10-016 | EF Core repository implementations for Channel and Thread | Infrastructure | Comms | M | Yes |
| CP10-017 | Gmail IChannelAdapter implementation (OAuth2, Pub/Sub push, thread normalize) | Infrastructure | Comms | L | No |
| CP10-018 | Discord IChannelAdapter implementation (Discord.Net, Gateway WebSocket, message normalize) | Infrastructure | Comms | L | No |
| CP10-019 | Slack IChannelAdapter implementation (SlackNet Socket Mode, message normalize) | Infrastructure | Comms | L | No |
| CP10-020 | WhatsApp IChannelAdapter implementation (Baileys bridge — only if SK-07 is go) | Infrastructure | Comms | L | No |
| CP10-021 | GitHub IChannelAdapter implementation (GitHub Notifications REST API, webhook normalize) | Infrastructure | Comms | M | No |
| CP10-022 | Comms API controller (channel CRUD + thread list/get + action endpoints) | Presentation | Comms | M | Yes |
| CP10-023 | Unified inbox page: channel filter sidebar, priority view, thread list, unread badge | Frontend | Comms | L | No |
| CP10-024 | Thread detail panel: message list, reply composer, CommLink controls | Frontend | Comms | M | Yes |
| CP10-025 | Channel connection UI: connect flow (OAuth modal for Gmail), status badge, sync status | Frontend | Comms | M | Yes |
| CP10-026 | People profile: communication timeline (read from ICommsReadService) | Frontend | People/Comms | M | No |
| CP10-027 | Cross-module: link thread → task (pre-fills task create modal with email subject) | Frontend | Comms/Tasks | M | No |
| CP10-028 | Domain tests for Channel and Thread aggregates (status transitions, invariants) | Tests | Comms | L | Yes |
| CP10-029 | Integration tests: Gmail adapter sync, Discord message normalize, auto-link by handle | Tests | Comms | L | Yes |
| CP10-030 | Integration tests: PersonContactHandleAddedEvent → handle registered → message auto-linked | Tests | Comms | M | No |
| CP10-031 | E2E: connect Gmail, receive message, link to task, verify task has comm attachment (Playwright) | Tests | Comms | M | No |

### Verification Gate

```
./dev verify
  ✓ Backend: all CP9 tests + CP10 domain/application/integration tests pass
  ✓ Frontend: all CP9 tests + CP10 component tests pass
  ✓ E2E: Gmail connect, inbox sync, read thread, link to task
  ✓ Auto-link: add email handle to person → future messages from that handle link automatically
  ✓ Priority notification: receive high-priority email → desktop notification fires
  ✓ Discord + Slack adapters: at least one message received and normalized per channel
  ✓ People timeline: person detail shows linked Comms messages
  ✓ Lint + TypeScript type check clean
  ✓ WhatsApp: either adapter working (go) or feature clearly deferred behind a feature flag (no-go)
```

### Key Deliverables

- Comms module navigable from main nav
- Gmail connected and syncing real inbox threads
- Discord and Slack adapters receiving messages
- Unified inbox with priority filter and unread counts
- Thread-to-task and thread-to-person linking working
- People profile shows communication timeline

---

## CP11: Integration & Polish

### Scope

Cross-module workflow completeness, production hardening, performance optimization to meet NFR targets, full E2E scenario coverage, and deployment readiness. No new bounded contexts — this checkpoint fills gaps, strengthens coverage, and prepares for production.

### Prerequisites

- CP10 complete and verified
- All bounded contexts implemented and individually tested

### Tasks

| ID | Task | Layer | Context | Effort | Parallel? |
|----|------|-------|---------|--------|-----------|
| CP11-001 | Global search: query across Projects, People, Comms threads, Tasks from a single search bar | Application/Frontend | Cross-module | L | No |
| CP11-002 | Notification context: subscribe to all v2 events (WorkQueueCompleted, WorkQueueItemFailed, budget alerts) | Application | Notification | M | Yes |
| CP11-003 | Project detail: unified view combining repo status + linked tasks + linked people + active agent sessions | Frontend | Cross-module | M | No |
| CP11-004 | Dashboard / home: activity feed aggregating events from all v2 modules | Frontend | Cross-module | M | No |
| CP11-005 | Performance audit: measure API p95 response times for all new endpoints; optimize hot paths | Infrastructure | All | M | Yes |
| CP11-006 | NFR-001 compliance: confirm p95 < 200ms across all new endpoints under load | Tests | All | M | No |
| CP11-007 | Redis Stream consumer group lag monitoring + alerting (Aspire dashboard metrics) | Infrastructure | Agents | S | Yes |
| CP11-008 | Node.js sidecar crash recovery: detect dead process, auto-restart up to MaxRetries, emit AgentSessionFailedEvent if recovery fails | Infrastructure | Agents | M | No |
| CP11-009 | Agent session cleanup: background service deletes terminal sessions older than configurable retention period | Infrastructure | Agents | S | Yes |
| CP11-010 | Channel credential rotation: detect expired OAuth tokens, trigger re-authentication flow in UI | Application/Frontend | Comms | M | No |
| CP11-011 | Comms sync health: background service polls channel health checks and emits ChannelHealthDegradedEvent | Infrastructure | Comms | S | Yes |
| CP11-012 | v2 OpenAPI spec update: add all new endpoints to the spec; regenerate openapi-typescript | Infrastructure | All | S | Yes |
| CP11-013 | v2 E2E scenario coverage: implement all remaining Playwright scenarios from agent-workflows.md and communications.md | Tests | All | L | No |
| CP11-014 | Full v2 regression: run complete test suite with v2 modules enabled; fix any regressions in v1 contexts | Tests | All | M | No |
| CP11-015 | Security review: confirm AES-256-GCM encryption on ChannelCredential; confirm API key hashing on AgentSession; audit v2 RBAC | Infrastructure | All | M | No |
| CP11-016 | Docker multi-stage build update: include Node.js sidecar in the API container image | Infrastructure | Agents | S | No |
| CP11-017 | Azure deployment update: add Redis Managed Redis resource to Terraform; update GitHub Actions workflow | Infrastructure | All | M | No |
| CP11-018 | v2 changelog + release notes preparation | Docs | All | S | No |

### Verification Gate

```
./dev verify
  ✓ Backend: full test suite passes (all CP1-CP11 tests, target > 1,400 total)
  ✓ Frontend: full test suite passes (target > 650 component + property tests)
  ✓ E2E: all scenarios from agent-workflows.md, project-management.md, communications.md, people-management.md covered
  ✓ Performance: API p95 < 200ms on all new endpoints (NFR-001.1)
  ✓ Redis Stream lag: < 500ms from sidecar event to domain event dispatch
  ✓ SignalR streaming: log lines arrive in browser < 200ms from Redis publish
  ✓ Security: ChannelCredential AES-256-GCM verified; AgentSession API key SHA-256 hash verified
  ✓ Crash recovery: kill Node.js sidecar mid-session; session auto-recovers or fails gracefully
  ✓ Docker: complete image builds and runs; Node.js sidecar functional inside container
  ✓ Azure: Terraform plan applies cleanly to staging; no infrastructure regressions
  ✓ Lint + TypeScript type check clean
  ✓ Build: zero warnings
```

### Key Deliverables

- Production-ready v2 deployment to lemondo.btas.dev
- All v2 modules visible and functional in the same navigation shell as v1
- Full cross-module workflows tested end-to-end
- All NFR targets met or documented as exceptions with workarounds

---

## Critical Path

The longest dependency chain is:

```
CP6 (Projects) → CP8 (Agent Core) → CP9 (Agent Intelligence + Bridges) → CP11 (Polish)
```

This chain is **critical** because:

1. CP8 cannot start until Projects worktrees exist (agents run inside worktrees)
2. CP9 cannot start until Agent Core is stable (skills, chains, and bridges depend on the session state machine)
3. CP11 cannot finalize until all contexts exist

**CP7 (People) and CP10 (Comms) are not on the critical path** — they can be worked in parallel with the Agent chain by a second developer or agent batch. CP7 must complete before CP10 starts (People context publishes events that Comms subscribes to), but neither blocks the Agent chain.

### Bottlenecks

| Bottleneck | Risk | Mitigation |
|------------|------|------------|
| Node.js sidecar (CP8-019) | Highest implementation risk; Claude Agent SDK sidecar is novel | Spike SK-04 must complete first; allocate L effort; plan for rework |
| Redis Streams bidirectional (CP8-020) | Complex event-loop integration | Spike SK-03 validates pattern; integration tests are mandatory |
| Baileys / WhatsApp (CP10-020) | ToS violation risk; may need deferral | Spike SK-07 produces go/no-go; feature-flag the adapter if no-go |
| WorkQueue orchestration (CP9-024) | Coordinates across three contexts | Requires CP7 (Projects) and CP8 (Agents) fully stable before starting |

---

## Effort Summary

### By Checkpoint

| Checkpoint | S tasks | M tasks | L tasks | XL tasks | Est. Total Effort |
|------------|---------|---------|---------|----------|-------------------|
| CP6 | 10 | 16 | 0 | 0 | ~9 weeks (solo) |
| CP7 | 10 | 14 | 1 | 0 | ~7 weeks (solo) |
| CP8 | 5 | 13 | 9 | 0 | ~10 weeks (solo) |
| CP9 | 10 | 18 | 7 | 0 | ~12 weeks (solo) |
| CP10 | 5 | 14 | 7 | 0 | ~10 weeks (solo) |
| CP11 | 5 | 9 | 2 | 0 | ~5 weeks (solo) |
| **Total** | **45** | **84** | **26** | **0** | **~53 weeks (solo)** |

*Estimates assume agent-assisted development (Claude Code sessions completing M tasks in 2-4h agent time + Bruno review). With parallel agent batches for parallelizable tasks, wall-clock time compresses significantly.*

### By Module

| Module | Checkpoints | Approx Tasks |
|--------|-------------|--------------|
| Projects | CP6 | 34 |
| People | CP7 | 20 |
| ProjectTaskBridge | CP7 | 7 |
| Agents (Core) | CP8 | 33 |
| Agents (Intelligence) | CP9 | 15 |
| ProjectAgentBridge | CP9 | 12 |
| AgentTaskBridge | CP9 | 7 |
| Comms | CP10 | 31 |
| Cross-module / Polish | CP11 | 18 |

### By Layer (Across All Checkpoints)

| Layer | Approx Tasks | Notes |
|-------|-------------|-------|
| Domain | 40 | Highest unit test density; many are parallel |
| Application | 45 | Event handlers and use cases; some ordering constraints |
| Infrastructure | 35 | Heaviest individual effort (sidecar, adapters, EF Core) |
| Presentation (API) | 20 | Controller thin slices; generated from domain |
| Frontend | 30 | Can be partially parallelized with backend tasks |
| Tests | 30 | Woven throughout every task; test-first always |

---

## References

- Domain contexts: [`docs/domain/contexts/`](../domain/contexts/)
- Bridge contexts: [`docs/domain/contexts/bridges/`](../domain/contexts/bridges/)
- Technology research: [`docs/operations/research/`](../operations/research/)
- Capability tiers beyond CP11: [`docs/roadmap/capability-tiers.md`](./capability-tiers.md)
- v2 product modules: [`docs/product/modules/`](../product/modules/)
- v2 PRD: [`docs/PRD.md`](../PRD.md)
