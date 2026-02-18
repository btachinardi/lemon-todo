# LemonDo v2 - Product Requirements Document (Draft)

> **Date**: 2026-02-18
> **Status**: Draft
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

```
+-------------------+
|     Projects      |<-------- aggregates tasks, links to people
|  (repos, deploy)  |
+-------------------+
     ^         |
     |         v
+--------+  +---------+     +----------+
| Tasks  |  | Agents  |<--->| Comms    |
| (v1)   |  | (Claude)|     | (unified)|
+--------+  +---------+     +----------+
     ^         |                  |
     |         v                  v
     |    +----------+     +----------+
     +--->|  People  |<----|  People  |
          | & Co's   |     | & Co's   |
          +----------+     +----------+
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

The most ambitious module. Enables Bruno to interact with Claude Code agent sessions directly from the LemonDo UI, automate task-to-agent workflows, and orchestrate parallel development across worktrees.

### 7.2 Core Concepts

- **Agent Session** = a Claude Code CLI session (or Claude Agent SDK session)
- **Work Queue** = ordered list of tasks assigned to agent processing
- **Execution Mode** = parallel (multiple worktrees) or sequential (one after another)
- **Budget** = token/cost limit per session or per queue
- **Agent API** = REST endpoints agents can call to interact with LemonDo (create tasks, update projects, log learnings)

### 7.3 Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| AG-001 | Start a new Claude Code agent session from UI | P0 | With project context |
| AG-002 | View active agent sessions and their status | P0 | Running, idle, completed, failed |
| AG-003 | View agent session output/logs in real-time | P0 | Streaming terminal output |
| AG-004 | Assign a task to an agent session | P0 | Task becomes the agent's objective |
| AG-005 | Auto-create worktree for agent session | P1 | Isolate agent work |
| AG-006 | Select multiple tasks and batch-assign to agent queue | P1 | "Start working on these 7 features" |
| AG-007 | Parallel execution mode: multiple agents on separate worktrees | P1 | For speed |
| AG-008 | Sequential execution mode: one agent finishes, next starts | P1 | For dependent work |
| AG-009 | Work queue with priority ordering | P1 | Higher priority tasks processed first |
| AG-010 | Budget management: set token/cost limits per session | P1 | Prevent runaway costs |
| AG-011 | Budget management: set limits per queue | P2 | Total budget for a batch |
| AG-012 | Agent API: expose REST endpoints for agents to create/manage tasks | P1 | Agents can create follow-up tasks |
| AG-013 | Agent API: expose endpoints for agents to update project state | P2 | Agents can log decisions, update docs |
| AG-014 | Agent API: expose endpoints for agents to add people/comms learnings | P2 | Auto-capture relationship data |
| AG-015 | Agent-driven email-to-task automation | P2 | Agent reads emails, creates categorized tasks |
| AG-016 | Agent session templates (predefined objectives, context, constraints) | P2 | Reusable agent configurations |
| AG-017 | Claude Agent SDK integration for custom agent behaviors | P2 | Beyond CLI sessions |
| AG-018 | Session history and audit trail | P1 | What did the agent do, when, cost |
| AG-019 | Approve/reject agent-proposed changes before merge | P1 | Human-in-the-loop |
| AG-020 | Agent notification on completion/failure | P0 | Desktop notification + in-app |

### 7.4 Key Scenarios

**S-AG-01: Batch Feature Development**
> Bruno selects 7 tasks from the backlog for the next version. He clicks "Create worktrees and start working." LemonDo creates 7 worktrees, starts 7 Claude Code sessions (within budget limits), each assigned to one task. Bruno monitors progress in a dashboard, reviews completed work, and approves merges.

**S-AG-02: Email-to-Task Automation**
> An agent is configured to process Bruno's inbox every morning. It reads customer feedback emails, creates bug fix tasks (tagged "customer-feedback") in the appropriate project, and creates improvement tasks for feature requests. Bruno reviews the generated tasks over coffee.

**S-AG-03: Sequential Quality Pipeline**
> Bruno assigns 5 tasks to a sequential queue for a project that can't have parallel worktrees. Agent #1 completes task 1, commits, and signals done. Agent #2 starts on task 2 with the updated codebase. Each agent runs the verification gate before signaling completion.

**S-AG-04: Agent Creates Follow-Up Work**
> While working on a feature, an agent discovers a related bug. It calls the LemonDo API to create a new bug fix task, tagged with the project and linked to the current feature task. Bruno sees the new task in his board without interrupting the agent's work.

---

## 8. Cross-Module Integration Points

These are the key integration points that make v2 more than the sum of its parts:

| Integration | Description |
|-------------|-------------|
| Task <-> Project | Tasks belong to projects; boards can be filtered by project |
| Task <-> Comms | Messages/threads can be linked to tasks as context |
| Task <-> People | Tasks can have associated people (assignees, reporters) |
| Task <-> Agent | Tasks can be assigned to agent sessions for automated work |
| Project <-> People | People can be linked to projects as collaborators/stakeholders |
| Project <-> Agent | Agent sessions operate within project worktrees |
| Comms <-> People | Messages auto-linked to people by email/handle; conversation history |
| Comms <-> Agent | Agents can process communications and create tasks |
| People <-> Agent | Agents can capture learnings about people from communications |

---

## 9. Non-Functional Requirements (v2 Additions)

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-001 | Single-user mode (Bruno only) | No multi-tenancy needed |
| NFR-V2-002 | Local-first: all project data stored locally | Git repos stay on disk |
| NFR-V2-003 | API for agent integration | RESTful, authenticated |
| NFR-V2-004 | Real-time agent session output streaming | WebSocket or SSE |
| NFR-V2-005 | Communication channel adapters must be pluggable | Adapter pattern |
| NFR-V2-006 | Budget tracking accurate to sub-dollar granularity | For agent cost control |
| NFR-V2-007 | Graceful degradation when external services are unavailable | Offline-capable for local features |
| NFR-V2-008 | All v1 features and tests continue to pass | Non-breaking evolution |

---

## 10. Technology Considerations

| Concern | Approach | Notes |
|---------|----------|-------|
| Git operations | `simple-git` (Node.js) or direct CLI calls | Worktree management |
| Process management | Node.js `child_process` or PM2-like runner | Dev server start/stop |
| Gmail integration | Google APIs (OAuth2 + Gmail API) | Read/send emails |
| WhatsApp | WhatsApp Business API or Baileys bridge | Research needed |
| Discord | Discord.js or REST API | Bot token |
| Slack | Slack Bolt SDK or REST API | App token |
| Claude Code | CLI subprocess or Claude Agent SDK | Agent sessions |
| ngrok | ngrok npm package or API | Tunnel management |
| Real-time streaming | WebSocket (Socket.io or native) or SSE | Agent output |
| Local DB expansion | SQLite (existing) extended with new tables | Same provider strategy |

---

## 11. Out of Scope for v2

- Multi-user/multi-tenant support (this is Bruno's personal tool)
- Mobile-specific UX for new modules (desktop-first, responsive later)
- Full CRM features (sales pipeline, deal tracking)
- Calendar integration (potential v3)
- Billing/invoicing
- Video calling integration
- Social media posting/management

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

## 13. Open Questions

1. **WhatsApp integration**: WhatsApp Business API requires a business account. Is Baileys (unofficial bridge) acceptable for personal use?
2. **LinkedIn**: No official messaging API. Scraping is fragile and ToS-violating. Defer or find alternative?
3. **Agent budget granularity**: Should budget be per-session, per-task, per-project, or per-day?
4. **Agent approval workflow**: Should agents auto-commit to feature branches, or require explicit approval for each commit?
5. **Technology pivot**: v1 is .NET + React. Should v2 modules use the same stack, or consider NestJS (per global CLAUDE.md tech stack) for new backend modules?
6. **Data model evolution**: How do we evolve the existing DB schema without breaking v1 features?
