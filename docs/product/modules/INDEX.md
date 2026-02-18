# Product Modules

> All functional requirements organized by product module, covering v1 active modules and v2 draft modules.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [tasks.md](./tasks.md) | Task management (FR-003) and list view (FR-005) | Active |
| [boards.md](./boards.md) | Kanban board (FR-004) | Active |
| [identity.md](./identity.md) | Authentication (FR-001), RBAC (FR-002), and onboarding (FR-008) | Active |
| [administration.md](./administration.md) | System administration (FR-006), HIPAA compliance (FR-007), communications & churn prevention (FR-009), product analytics (FR-010) | Active |
| [projects.md](./projects.md) | Project and git repository management | Draft (v2) |
| [comms.md](./comms.md) | Unified communication inbox across all channels | Draft (v2) |
| [people.md](./people.md) | People and company relationship management | Draft (v2) |
| [agents.md](./agents.md) | AI agent session management and orchestration | Draft (v2) |

---

## Summary

The v1 modules cover the complete task management lifecycle: users authenticate and are onboarded through the **identity** module, manage individual tasks through the **tasks** module, visualize work on Kanban boards through the **boards** module, and administrators operate compliance controls through the **administration** module.

The v2 modules extend the platform into a personal development command center. The **projects** module treats git repositories as first-class entities with worktree management, dev server controls, and ngrok tunneling. The **comms** module unifies Gmail, WhatsApp, Slack, Discord, LinkedIn, and GitHub notifications into a single priority-aware inbox. The **people** module provides lightweight CRM-style relationship tracking with notes, preferences, and cross-module linking. The **agents** module is the most ambitious: it enables Claude Code agent sessions to be launched, monitored, and orchestrated directly from the UI.

All four v2 modules interconnect intentionally — see the cross-module integration table below.

---

## Cross-Module Integration Points (v2)

> **Source**: Extracted from docs/PRD.md §8
> **Status**: Draft (v2)

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
