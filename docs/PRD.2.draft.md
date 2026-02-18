# LemonDo v2 - Product Requirements Document

> **Date**: 2026-02-18
> **Status**: Active (promoted from Draft — Phase 2 design complete)
> **Author**: Bruno (product owner)
> **Previous Version**: [Product Requirements](./product/INDEX.md) (v1 — Task Management Platform)
> **Branch**: `feature/v2-planning`

---

## 1. Vision Statement

LemonDo v1 is a feature-complete, production-grade task management platform with HIPAA-ready security, PWA offline support, and a polished UX across 1,094 tests and 7 patch releases. It demonstrates best-in-class SDLC practices: DDD architecture, TDD, gitflow, structured documentation, and checkpoint-based delivery.

**v2 evolves LemonDo from a task management app into Bruno's personal development command center.** The theme is "Bruno" — this platform becomes the unified hub for managing projects, communications, people, and AI agent workflows. The quality bar set in v1 (architecture, testing, documentation) carries forward as the foundation.

### Why v2?

The v1 SDLC practices (specs, guidelines, checkpoints, documentation hierarchy) are the best Bruno has ever implemented. Rather than starting fresh for personal tooling, v2 builds on this proven foundation to solve real daily pain points:

- **Scattered project management** — repos, worktrees, deployments, and tasks live in different tools
- **Fragmented communications** — important messages lost across Gmail, WhatsApp, Discord, Slack, LinkedIn
- **Disconnected people/company knowledge** — no central record of relationships, preferences, learnings
- **Manual agent orchestration** — Claude Code sessions require manual setup, task assignment, and monitoring

---

## 2. Target User

**Bruno** — a solo developer managing multiple projects, communications channels, and AI-assisted development workflows. All v2 features are designed for a single power user (Bruno), not multi-tenant SaaS.

---

## 3. Module Overview

v2 introduces four new modules alongside the existing Task Management core:

| Module | Codename | Purpose |
|--------|----------|---------|
| Existing | `tasks` | Task lifecycle, kanban boards, lists (v1) |
| New | `projects` | Repository management, worktrees, local dev, deployment |
| New | `comms` | Unified communication inbox across all channels |
| New | `people` | People & company relationship management |
| New | `agents` | AI agent session management and orchestration |

### Module Relationship Map

Cross-module integrations are mediated by dedicated bridge bounded contexts rather than direct coupling between core domains. See section 8 for details.

```
+----------+    +-------------------------+    +----------+
| Projects |<-->|  ProjectAgentBridge     |<-->|  Agents  |
+----------+    +-------------------------+    +----------+
     |                                              |
     |           +-------------------------+        |
     +---------->|  ProjectTaskBridge      |        |
     |           +-------------------------+        |
     |                      |                       |
     |           +-------------------------+        |
     |           |  AgentTaskBridge        |<-------+
     |           +-------------------------+
     |                      |
     |                      v
     |                 +----------+     +----------+
     +---------------->|  Tasks   |     |  Comms   |
                        +----------+     +----------+
                              |                |
                              v                v
                         +----------+    +----------+
                         |  People  |<-->|  People  |
                         +----------+    +----------+
```

---

## 4. Module 1: Project Management (`projects`)

### 4.1 Overview

A project is a **git repository** with associated metadata, configuration, and operational controls. Projects aggregate tasks, link to people (collaborators), and serve as the organizational unit for development workflows.

### 4.2 Core Concepts

- **Project** = a git repository (local or remote)
- **Worktree** = a git worktree within a project (for parallel feature development)
- **Environment** = a deployment target (local dev, staging, production)
- **Instructions** = development guidelines, deployment docs, architecture notes (markdown files already in repo)

### 4.3 Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| PM-001 | Register a project by pointing to a local git repository path | P0 | Scan for README, CLAUDE.md, GUIDELINES.md, etc. |
| PM-002 | View project metadata: name, description, tech stack, branch info | P0 | Auto-detect from repo |
| PM-003 | View and navigate project documentation (markdown files) | P0 | Render markdown in-app |
| PM-004 | Create and manage git worktrees from the UI | P1 | `git worktree add/remove/list` |
| PM-005 | View worktree status (current branch, dirty/clean, ahead/behind) | P1 | Real-time or polling |
| PM-006 | Install project dependencies from UI (npm install, dotnet restore, etc.) | P1 | Detect package manager |
| PM-007 | Start/stop/restart project dev servers remotely | P1 | Process management |
| PM-008 | Expose local dev servers via ngrok integration | P2 | For external testing |
| PM-009 | Aggregate tasks by project (link tasks to a project) | P0 | Filter boards/lists by project |
| PM-010 | Link people/companies to projects (collaborators, stakeholders) | P1 | Cross-module reference |
| PM-011 | Project-level settings (default branch, CI/CD status, environment vars) | P2 | Configuration management |
| PM-012 | View git log and branch history within the UI | P1 | Visual git graph |

### 4.4 Key Scenarios

**S-PM-01: Register Existing Project**
> Bruno opens LemonDo, clicks "Add Project", selects a local folder. LemonDo scans the repo, detects it's a pnpm monorepo with .NET backend, reads the README and CLAUDE.md, and creates a project card with tech stack tags and documentation links.

**S-PM-02: Parallel Feature Development**
> Bruno has 3 features to build for a project. He selects the project, clicks "Create Worktrees", names them after the feature branches. Each worktree appears as a tab within the project. He can see the status of each, start dev servers, and assign agent sessions to each.

**S-PM-03: Quick Demo Setup**
> Bruno needs to demo a feature to a client. He clicks "Expose" on a running dev server, LemonDo creates an ngrok tunnel and provides a shareable URL. When the demo ends, he clicks "Stop" and the tunnel closes.

---

## 5. Module 2: Communication Management (`comms`)

### 5.1 Overview

A unified inbox that aggregates messages from multiple communication channels into a single, filterable, priority-aware interface. The goal is: **never miss or forget an important message again.**

### 5.2 Core Concepts

- **Channel** = a communication source (Gmail, WhatsApp, Discord, Slack, LinkedIn, GitHub)
- **Thread** = a conversation/email thread within a channel
- **Message** = an individual message within a thread
- **Priority** = user-assigned or AI-suggested importance level
- **Link** = association between a message/thread and a project, task, or person

### 5.3 Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| CM-001 | Connect Gmail account(s) via OAuth2 | P0 | Read inbox, labels, threads |
| CM-002 | Connect WhatsApp via WhatsApp Business API or bridge | P1 | Read/send messages |
| CM-003 | Connect Discord via bot token | P1 | Monitor specific servers/channels |
| CM-004 | Connect Slack via bot/app token | P1 | Monitor specific workspaces/channels |
| CM-005 | Connect LinkedIn messaging (scraping or API if available) | P2 | Read messages, connection requests |
| CM-006 | Connect GitHub notifications | P1 | Issues, PRs, mentions, reviews |
| CM-007 | Unified inbox view with all messages across channels | P0 | Chronological or priority-sorted |
| CM-008 | Filter by channel, priority, read/unread, date range | P0 | Faceted filtering |
| CM-009 | View all priority messages across all channels | P0 | "Important" smart view |
| CM-010 | Reply to messages without leaving LemonDo | P1 | Per-channel send integration |
| CM-011 | Link messages/threads to projects | P1 | Cross-module reference |
| CM-012 | Link messages/threads to tasks (as attachments) | P1 | Context on tasks |
| CM-013 | Link messages to people/companies | P1 | Conversation history per person |
| CM-014 | AI-powered message categorization and priority suggestion | P2 | Based on sender, content, urgency keywords |
| CM-015 | Notification when new high-priority messages arrive | P1 | Desktop notification |
| CM-016 | Search across all channels simultaneously | P0 | Full-text search |

### 5.4 Key Scenarios

**S-CM-01: Morning Inbox Review**
> Bruno opens LemonDo's Comms tab. He sees 12 unread messages: 5 emails, 3 Slack messages, 2 WhatsApp, 1 Discord, 1 GitHub review request. He switches to "Priority" view and sees the 3 most important items highlighted. He replies to the urgent email directly from LemonDo.

**S-CM-02: Linking Communication to Work**
> A client sends an email with a bug report. Bruno reads it in the unified inbox, clicks "Create Task", which pre-fills the task with the email subject and links the email thread as an attachment. The task is automatically assigned to the client's project.

**S-CM-03: Person-Centric Communication History**
> Bruno is about to meet with a client. He opens their People profile and sees a timeline of all communications: recent emails, Slack messages, and a WhatsApp thread from last week. He's fully prepared without searching 5 different apps.

---

## 6. Module 3: People & Companies Management (`people`)

### 6.1 Overview

A lightweight CRM for tracking relationships with people and companies Bruno interacts with. Not a full CRM suite — focused on knowledge retention and relationship context.

### 6.2 Core Concepts

- **Person** = an individual Bruno interacts with
- **Company** = an organization (may have associated people)
- **Note** = a timestamped piece of information about a person/company
- **Tag** = categorization (client, colleague, friend, family, vendor, etc.)
- **Relationship Type** = professional, personal, or both

### 6.3 Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| PP-001 | Create and manage person records | P0 | Name, email(s), phone(s), photo |
| PP-002 | Create and manage company records | P0 | Name, website, industry, logo |
| PP-003 | Link people to companies (role, department) | P0 | Many-to-many |
| PP-004 | General information section (bio, role, how we met) | P0 | Free-form + structured |
| PP-005 | Notes/learnings section (timestamped entries) | P0 | "Mentioned daughter starts school in Sept" |
| PP-006 | Preferences section (communication style, timezone, tools they use) | P1 | Structured fields |
| PP-007 | Personal vs professional information separation | P1 | Toggle visibility context |
| PP-008 | Important dates tracking (birthdays, anniversaries, milestones) | P1 | With reminders |
| PP-009 | Link people to projects (role: collaborator, stakeholder, client) | P1 | Cross-module reference |
| PP-010 | Link people to communication messages/threads | P1 | Auto-link by email/handle match |
| PP-011 | Tag and categorize people/companies | P0 | Flexible tagging |
| PP-012 | Search across all people and companies | P0 | Full-text |
| PP-013 | Timeline view of all interactions with a person | P2 | Aggregate from comms + notes + tasks |
| PP-014 | Family/relationship tree for personal contacts | P3 | "Partner: Maria, Kids: Lucas, Sofia" |

### 6.4 Key Scenarios

**S-PP-01: Meeting Preparation**
> Before a call with a client, Bruno opens their profile. He sees they prefer async communication, use Slack for quick questions, and mentioned last month that they're migrating to AWS. He also sees they have a birthday next week. He sends a quick "happy birthday" message after the meeting.

**S-PP-02: Knowledge Capture**
> During a conversation, a colleague mentions they're an expert in Kubernetes. Bruno opens their profile, adds a note: "Strong K8s experience, offered to help with our deployment". This knowledge is now searchable and visible next time Bruno needs K8s help.

---

## 7. Module 4: Agent Sessions Management (`agents`)

### 7.1 Overview

The most ambitious module. Enables Bruno to interact with Claude Code agent sessions directly from the LemonDo UI, automate task-to-agent workflows, and orchestrate parallel development across worktrees. Architecture: .NET 10 backend + Node.js sidecar per session, with Redis Streams as the bidirectional event bus.

### 7.2 Core Concepts

- **Agent Session** = a Claude Code CLI session managed via Node.js sidecar process (one sidecar per session)
- **Agent Template** = reusable session configuration (objective, skills, model selection, budget defaults)
- **Agent Skill** = composable package of instructions, tools, and memory pills that agents load at session start
- **Memory Pill** = agent-recorded learning (Mistake, Tip, Guideline, Pattern, Convention) that improves skills over time via consolidation
- **Session Chain** = logical continuity across handoffs when context is exhausted or voluntarily handed off
- **Activity Stream** = typed real-time feed of session activity (messages, tool calls, subagent spawns, system events)
- **Auto-Continue** = automatic session continuation with configurable validation criteria (tests passing, coverage threshold) when deliverables don't meet the bar
- **Session Plan** = structured plan document created via EnterPlanMode/ExitPlanMode special tool calls; reviewable and approvable by Bruno before implementation begins
- **Session Task List** = progress tracker populated by TodoWrite tool calls; rendered as a visible progress sidebar in the session UI
- **Work Queue** = ordered list of tasks assigned to agent processing (owned by ProjectAgentBridge); supports parallel and sequential execution modes
- **Budget** = per-session and per-queue token/cost limits enforced as hard caps before overspend

### 7.3 Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| AG-001 | Start a new Claude Code agent session from UI | P0 |
| AG-002 | View active agent sessions and their status | P0 |
| AG-003 | View agent session output/logs in real-time | P0 |
| AG-004 | Assign a task to an agent session | P0 |
| AG-005 | Auto-create worktree for agent session | P1 |
| AG-006 | Select multiple tasks and batch-assign to agent queue | P1 |
| AG-007 | Parallel execution mode: multiple agents on separate worktrees | P1 |
| AG-008 | Sequential execution mode: one agent finishes, next starts | P1 |
| AG-009 | Work queue with priority ordering | P1 |
| AG-010 | Budget management: set token/cost limits per session | P1 |
| AG-011 | Budget management: set limits per queue | P2 |
| AG-012 | Agent API: expose REST endpoints for agents to create/manage tasks | P1 |
| AG-013 | Agent API: expose endpoints for agents to update project state | P2 |
| AG-014 | Agent API: expose endpoints for agents to add people/comms learnings | P2 |
| AG-015 | Agent-driven email-to-task automation | P2 |
| AG-016 | Agent session templates (predefined objectives, context, constraints) | P2 |
| AG-017 | Claude Agent SDK integration for custom agent behaviors | P2 |
| AG-018 | Session history and audit trail | P1 |
| AG-019 | Approve/reject agent-proposed changes before merge | P1 |
| AG-020 | Agent notification on completion/failure | P0 |
| AG-021 | Real-time structured activity stream (skimmable tool calls, messages, subagent events) | P0 |
| AG-022 | Expandable tool call detail with per-tool-type UI customization | P1 |
| AG-023 | Subagent compact view (status, elapsed, context window, last action, expandable) | P1 |
| AG-024 | Context window usage indicator for informed decision-making | P1 |
| AG-025 | Send immediate steering message to running agent (interrupt + inject) | P1 |
| AG-026 | Send queued follow-up message for after current turn | P1 |
| AG-027 | Create and manage agent skills (name, instructions, tools, subagent definitions) | P1 |
| AG-028 | Enable/disable skills per agent session | P1 |
| AG-029 | Skills compose into effective session config (instructions + tools + subagents merged) | P1 |
| AG-030 | Default skills on templates (auto-enabled for all sessions using that template) | P2 |
| AG-031 | Subagent definitions within skills (scoped, not global) | P2 |
| AG-032 | Agents record memory pills during skill usage (content, category) | P1 |
| AG-033 | View memory pills per skill (filterable by category, status) | P1 |
| AG-034 | Consolidate memory pills via dedicated agent session | P2 |
| AG-035 | Skill versioning (incremented on consolidation) | P2 |
| AG-036 | Agent API endpoint for recording memory pills | P2 |
| AG-037 | Auto-continue mode with deliverable validation | P2 |
| AG-038 | Configurable validation criteria (tests passing, coverage threshold, custom) | P2 |
| AG-039 | Agent-initiated voluntary handoff (not just context exhaustion) | P2 |
| AG-040 | Session chain view showing handoff documents between sessions | P2 |
| AG-041 | AskUserQuestion tool call renders as structured interactive prompt in session UI | P1 |
| AG-042 | Session transitions to WaitingForInput on AskUserQuestion; resumes on user answer | P1 |
| AG-043 | TodoWrite creates a visible progress tracker sidebar in session UI | P1 |
| AG-044 | EnterPlanMode creates a reviewable plan document visible in session UI | P2 |
| AG-045 | ExitPlanMode optionally requires user approval before agent proceeds to implementation | P2 |
| AG-046 | Select AI model per agent session at start time (override template default) | P1 |
| AG-047 | Model selection shows cost/capability tradeoff info to inform choice | P1 |
| AG-048 | Hot-load skills into Idle or Interrupted sessions without restart | P2 |
| AG-049 | Session reload indicator in UI during skill hot-loading | P2 |
| AG-050 | Sidecar reinitializes with merged config after skill hot-load | P2 |

Full scenario coverage is documented in `docs/scenarios/agent-workflows.md` — Functional Requirements Coverage Matrix.

### 7.4 Key Scenarios

Ten detailed scenarios are documented in `docs/scenarios/agent-workflows.md`. Summaries:

| ID | Name | Summary |
|----|------|---------|
| S-AG-01 | Batch Feature Development | Bruno selects 7 backlog tasks, starts parallel agent sessions with worktrees, monitors progress, and approves merges from a dashboard — shipping multiple features without manual intervention |
| S-AG-02 | Email-to-Task Automation | A scheduled agent processes Bruno's inbox overnight, creates categorized tasks from customer emails, and surfaces skipped items for manual review — reducing morning triage from 1 hour to minutes |
| S-AG-03 | Sequential Quality Pipeline | Five dependency-ordered tasks run one agent at a time with a verification gate between each handoff; agent #3 fails and is retried with an instruction; all 5 merge cleanly |
| S-AG-04 | Agent Creates Follow-Up Work | An agent discovers a latent bug mid-session, calls the Agent API to create a P1 bug task with full context, and continues its original work without interrupting Bruno |
| S-AG-05 | Real-Time Session Monitoring | Bruno watches the activity stream live, steers the agent away from a deprecated file using an immediate message, and queues follow-up instructions for after the current turn |
| S-AG-06 | Skills and Custom Tools | Bruno creates composable skills with instructions, tools, and subagent definitions; sessions are pre-loaded with the right capabilities without manual instruction pasting |
| S-AG-07 | Memory Pills and Skill Consolidation | Agents accumulate 7 memory pills across sessions; a consolidation run folds them into skill v2 instructions, incrementing the version and marking pills consolidated |
| S-AG-08 | Auto-Continue and Voluntary Handoff | A session auto-continues when coverage is below threshold (72% → 83% on retry); a separate session voluntarily hands off at 55% context to start fresh for the second half of a refactor |
| S-AG-09 | Interactive Feedback | Bruno approves a plan before implementation starts, answers one AskUserQuestion mid-session, and watches a TodoWrite progress tracker tick through all 5 tasks |
| S-AG-10 | Model Selection and Skill Hot-Loading | Bruno uses Haiku for a trivial formatting task ($0.12), Opus for architecture work; adds a forgotten skill to the running Opus session via hot-load without losing context |

---

## 8. Cross-Module Integration Points

Cross-module integrations are handled by dedicated **bridge bounded contexts** rather than direct coupling between core domains. This keeps Projects, Tasks, Agents, Comms, and People pure — each context knows only its own domain. See `docs/domain/contexts/bridges/INDEX.md` for the full bridge context design.

| Bridge | Connects | Responsibility |
|--------|----------|----------------|
| **ProjectAgentBridge** | Projects + Agents | Owns `AgentProjectCorrelation` (session ↔ project ↔ worktree linkage) and the `WorkQueue` aggregate. Orchestrates "start agent in project worktree" and "agent completed → merge worktree" workflows |
| **AgentTaskBridge** | Agents + Tasks | Owns `AgentTaskCorrelation` (session ↔ task linkage). On session approval, marks the corresponding task complete. When an agent creates a follow-up task, links it back to the originating session |
| **ProjectTaskBridge** | Projects + Tasks | Associates tasks with projects via `ProjectTaskLink`. Provides cross-context queries ("all tasks for project X"). On worktree merge, triggers task completion for linked tasks |

Additional weaker cross-module references (not requiring a bridge):

| Integration | Description |
|-------------|-------------|
| Task <-> Comms | Messages/threads linked to tasks as context (weak reference by TaskId) |
| Task <-> People | Tasks reference people by PersonId (assignees, reporters) |
| Project <-> People | People linked to projects by PersonId (collaborators, stakeholders) |
| Comms <-> People | Messages auto-linked to people by email/handle match |
| Comms <-> Agent | Agents process communications and create tasks via Agent API |
| People <-> Agent | Agents capture learnings about people via Agent API (AG-014) |

---

## 9. Non-Functional Requirements

v2 adds 88 requirements across 12 categories to the existing v1 NFRs. Full details are in `docs/product/nfr.md`.

Representative highlights:

| ID | Category | Requirement | Target |
|----|----------|-------------|--------|
| NFR-V2-01.2 | Agent Performance | Activity stream event latency (SDK event → UI render) | < 500ms |
| NFR-V2-03.4 | Budget Accuracy | Budget hard-cap enforcement timing | Cap enforced BEFORE session exceeds it |
| NFR-V2-04.8 | Skills System | Composed system prompt must fit model context window | Error surfaced before session start if exceeded |
| NFR-V2-06.1 | Security | Agent API key entropy | 256-bit cryptographically random |
| NFR-V2-09.1 | Reliability | Sidecar crash detection | Within 5s; session marked Failed; pool slot released |
| NFR-V2-10.1 | Scalability | Maximum concurrent agent sessions | 20 (configurable) |
| NFR-V2-11.1 | Local-First | Projects module availability offline | Fully functional without internet |
| NFR-V2-12.7 | v1 Non-Regression | Database migrations | Additive only — no v1 table drops or column removals |

See `docs/product/nfr.md` for the complete list covering: Agent Session Performance, Real-Time Streaming, Budget & Metrics Accuracy, Skills System, Communication Adapters, Security, Bridge Context Performance, Data & Storage, Reliability & Recovery, Scalability Constraints, Local-First Behaviour, and v1 Non-Regression.

---

## 10. Technology Decisions

Technology research is documented in `docs/operations/research/INDEX.md`. Decisions made during Phase 2:

| Concern | Decision | Notes |
|---------|----------|-------|
| Git operations | `simple-git` (Node.js) via sidecar | Worktree management; sidecar already required for Claude Agent SDK |
| Process management | Node.js `child_process` in sidecar | Dev server start/stop; natural fit with sidecar architecture |
| Agent sessions | .NET 10 API + Node.js sidecar per session | Claude Agent SDK is TypeScript-only; sidecar bridges to .NET backend |
| Inter-process communication | Redis Streams (XADD/XREAD) | Bidirectional event bus between Node.js sidecar and .NET API; Aspire auto-provisions locally |
| Real-time streaming (API → Frontend) | SSE / SignalR | SignalR hub for activity stream; SSE for simpler feeds |
| Gmail integration | Google APIs (OAuth2 + Gmail API) | Free; Pub/Sub push notifications available |
| WhatsApp | Baileys (unofficial bridge) — Technology Spike Required | Official Cloud API not suitable for personal inbox; Baileys carries ToS risk |
| Discord | Discord.Net SDK (Bot Gateway WebSocket) | Free; confirmed compatible |
| Slack | SlackNet SDK (Socket Mode, internal app) | Free; internal app exemption retains full rate limits |
| ngrok | `@ngrok/ngrok` npm package via sidecar | $8/month Hobbyist plan; removes interstitial pages |
| LinkedIn | Deferred to v3 | No reliable API; scraping is ToS-violating and fragile |
| Agent model | Anthropic Claude API (Sonnet 4.6 default) | ~$15-$40/month depending on session volume |
| Local DB | SQLite (dev) / SQL Server (prod) — additive migrations | Same provider strategy as v1; new tables only |

---

## 11. Out of Scope for v2

- Multi-user/multi-tenant support (this is Bruno's personal tool)
- Mobile-specific UX for new modules (desktop-first, responsive later)
- Full CRM features (sales pipeline, deal tracking)
- Calendar integration (potential v3)
- Billing/invoicing
- Video calling integration
- Social media posting/management
- LinkedIn integration (deferred to v3)

---

## 12. Success Criteria

| Metric | Target |
|--------|--------|
| Projects registered and actively managed | 3+ |
| Communication channels connected | 3+ (Gmail + Slack + Discord minimum) |
| Agent sessions completed successfully | 10+ per week |
| Tasks auto-created by agents | 50% of bug fix tasks |
| Time to find a person's communication history | < 10 seconds |
| Morning inbox review time | < 5 minutes (down from 15+) |
| Parallel agent development efficiency | 3x faster than sequential manual work |

---

## 13. Resolved Questions

All open questions from the initial draft have been resolved during Phase 2 design:

| Question | Decision |
|----------|----------|
| **WhatsApp integration** | Use Baileys (unofficial bridge) for personal use. A dedicated technology spike is required to validate stability and ToS risk before implementation. See `docs/operations/research/whatsapp-api.md`. |
| **LinkedIn** | Deferred to v3. No reliable API or scraping approach exists. v2 focuses on Gmail, Slack, Discord, WhatsApp, and GitHub. |
| **Agent budget granularity** | Per-session budgets with pool-level caps. Budget = `maxCostUsd` + `maxTotalTokens`. WorkQueue adds a `QueueBudget` aggregate in ProjectAgentBridge. Hard caps are enforced before overspend, never after (NFR-V2-03.4). |
| **Agent approval workflow** | Human-in-the-loop via `HasPendingReview` flag. Agents work on feature branches; changes require explicit approval before merge (AG-019). The AgentTaskBridge drives task completion on approval. |
| **Technology stack** | .NET 10 backend (unchanged from v1) + Node.js sidecar per session (required for Claude Agent SDK). Redis Streams as the event bus. React 19 frontend unchanged. ADR-006 and ADR-007 document the architecture decisions. |
| **Data evolution** | Additive migration strategy. New tables for v2 modules; existing v1 tables untouched. EF Core migrations handle schema changes. NFR-V2-12.7 enforces this as a non-regression requirement. |
