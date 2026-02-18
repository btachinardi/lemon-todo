# Bridge Contexts

> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## Overview

Bridge contexts are thin integration bounded contexts whose sole responsibility is to coordinate between two or more core domains that must not be directly coupled. They own the correlation state and cross-context workflow logic that cannot live inside either upstream domain.

The pattern follows these rules:

1. **A bridge context knows more than either upstream** — By design, the bridge is the one place that understands both sides. This is intentional and bounded: the knowledge lives only in the bridge, never leaking back into the core contexts.
2. **Core contexts remain pure** — Projects does not know about agent sessions. Agents does not know about worktree paths. Tasks does not know about approval workflows. The bridge mediates.
3. **Bridges own correlation aggregates** — Each bridge context owns at least one aggregate that records the linkage between entities in different contexts (e.g., `AgentProjectCorrelation` maps an `AgentSessionId` to a `ProjectId` + `WorktreeId`).
4. **Bridges subscribe to upstream events** — They never call upstream contexts directly. They react to published domain events and dispatch commands to the appropriate context via the application layer.
5. **Bridges are thin by principle** — They should not accumulate business logic. If a bridge grows a rich domain model, that is a signal that a dedicated context is needed.

---

## Bridge Contexts

| Context | File | Connects | Responsibility |
|---------|------|----------|----------------|
| **ProjectAgentBridge** | [project-agent-bridge.md](./project-agent-bridge.md) | Projects + Agents | Owns `AgentProjectCorrelation` (session ↔ project ↔ worktree linkage) and the `WorkQueue` aggregate (moved here from Agents context). Orchestrates the "start agent in project worktree" and "agent completed → merge worktree" workflows. |
| **AgentTaskBridge** | [agent-task-bridge.md](./agent-task-bridge.md) | Agents + Tasks | Owns `AgentTaskCorrelation` (session ↔ task linkage). When a session is approved, marks the corresponding task complete. When an agent creates a follow-up task, links it back to the originating session and task. |
| **ProjectTaskBridge** | [project-task-bridge.md](./project-task-bridge.md) | Projects + Tasks | Associates tasks with projects via `ProjectTaskLink`. Provides cross-context queries ("all tasks for project X"). When a worktree is merged, can trigger task completion for linked tasks. |

---

## When to Use the Bridge Pattern

Use a bridge when:

- Two core contexts need to exchange rich state (not just a foreign ID reference)
- A multi-step workflow spans two contexts with intermediate state that must be persisted
- Neither upstream context is an appropriate owner of the cross-context workflow state
- The coupling would otherwise require a core context to import types from another core context

Do NOT use a bridge for simple weak references (a `TaskId` field on a project is fine — that does not need a bridge). Use a bridge when the relationship has its own lifecycle, status, or domain events.

---

## Context Map Position

Bridge contexts sit between the core contexts they connect. They subscribe to events from both sides and may publish their own events that other contexts (e.g., Notification) can subscribe to.

```
+----------+    +------------------------+    +----------+
| Projects |<-->| ProjectAgentBridge     |<-->|  Agents  |
+----------+    +------------------------+    +----------+
     |                                             |
     |          +------------------------+         |
     |          | ProjectTaskBridge      |         |
     +--------->|                        |         |
                +------------------------+         |
                          ^                        |
                          |    +----------------+  |
                          +----| AgentTaskBridge|<-+
                               +----------------+
                                       |
                                       v
                                 +----------+
                                 |  Tasks   |
                                 +----------+
```

---

## Relationship to Core Context Map

Bridge contexts are documented separately from the main context map in `docs/domain/INDEX.md` because they are integration concerns, not domain concerns. The core context map in INDEX.md shows the high-level event flow. This folder provides the full design of each bridge context.
