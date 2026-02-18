# Domain Design

> DDD domain design for LemonDo: bounded contexts, shared kernel, entity relationships, and API design.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [shared-kernel.md](./shared-kernel.md) | Shared types used across all bounded contexts (UserId, Result, Entity, etc.) | Active |
| [api-design.md](./api-design.md) | All API endpoints by context and cross-cutting concerns | Active |
| [contexts/](./contexts/) | Individual bounded context definitions | — |

---

## Summary

LemonDo is designed around Domain-Driven Design (DDD) with CQRS-light using Use Cases. The domain is decomposed into seven v1 bounded contexts and four additional v2 bounded contexts.

The three core v1 contexts are **Identity** (auth and RBAC), **Task** (task lifecycle and metadata), and **Board** (kanban spatial layout). Task and Board are deliberately separated: Task owns status transitions while Board owns card placement. Cross-context coordination is handled at the application layer by command handlers, never by direct coupling between domain objects.

The two supporting v1 contexts are **Administration** (audit logs, protected data reveal, user management) and **Onboarding** (guided user journey tracking). The two generic v1 contexts are **Analytics** (event collection and metrics) and **Notification** (email and in-app messaging).

The four v2 contexts extend LemonDo into Bruno's personal development command center. **Projects** owns git repository registration, worktrees, dev servers, and ngrok tunnels. **Comms** owns a unified communication inbox across Gmail, WhatsApp, Discord, Slack, LinkedIn, and GitHub. **People** owns a lightweight CRM for tracking persons and companies with notes, preferences, and project links. **Agents** owns AI agent session lifecycle, work queue orchestration, and budget management.

All bounded contexts share a common kernel of primitive types: `UserId`, `Result<T, E>`, `PagedResult<T>`, `DomainEvent`, `Entity<TId>`, and `ValueObject`. All timestamps are `DateTimeOffset` in UTC.

---

## Bounded Contexts Map

### v1 Contexts

```
+-------------------+     +-------------------+     +-------------------+
|     Identity      |     |   Task Context    |     |   Administration  |
|   (Auth + RBAC)   |<--->|  (Task Lifecycle) |<--->|   (Audit + Admin) |
+-------------------+     +-------------------+     +-------------------+
         |                     |          ^                    |
         |                     |          |                    |
         v                     v          |                    v
+-------------------+     +-------------------+     +-------------------+
|    Onboarding     |     |  Board Context    |     |   Notification    |
|   (User Journey)  |     | (Spatial Layout)  |     |  (Communication)  |
+-------------------+     +-------------------+     +-------------------+
         |                                                    |
         v                                                    |
+-------------------+                                         |
|    Analytics      |<----------------------------------------+
|   (Measurement)   |
+-------------------+
```

### v2 Contexts and Integration Points

```
                    +-------------------+
                    |     Identity      |
                    |   (Auth + RBAC)   |
                    +-------------------+
                      |    |    |    |
          +-----------+    |    |    +-----------+
          |                |    |                |
          v                v    v                v
+--------------+   +-----------+   +----------+   +----------+
|   Projects   |   |   People  |   |  Comms   |   |  Agents  |
| (Repos/Dev)  |   |   (CRM)   |   | (Inbox)  |   | (AI Orch)|
+--------------+   +----------+    +----------+   +----------+
     |    ^             |  ^           |  ^            |   |
     |    |  PersonId   |  |           |  |  auto-link |   |
     |    +-------------+  |           |  +------------+   |
     |        ProjectId    |           |  PersonCreated     |
     |    +----------------+           |  HandleAdded       |
     |    |                            |                    |
     |    | ProjectId (weak ref)       |                    |
     +<---+----------------------------+                    |
     |                                                      |
     |  AgentSessionStartedEvent (create worktree)          |
     |<-----------------------------------------------------+
     |                                                      |
     |  AgentSessionApprovedEvent (merge worktree)          |
     |<-----------------------------------------------------+
          |              |                         |
          v              v                         v
    +-----------+  +-----------+           +-------------------+
    |   Task    |  |   Task    |           |   Notification    |
    | (weak ref)|  |(complete  |           | (alerts, budget,  |
    | TaskId    |  | on approve)|          |  failures, comms) |
    +-----------+  +-----------+           +-------------------+
```

Legend:
- Solid arrow `-->` : downstream dependency (conformist or customer-supplier)
- `<-->` : bidirectional event-driven coupling
- `(weak ref)` : opaque ID stored as cross-context foreign reference; no validation at write time
- ACL boundary : People reads Comms via ICommsReadService (read-only anti-corruption layer)

---

## Context Map

### All Bounded Contexts

| Context | Version | Type | Responsibility |
|---------|---------|------|----------------|
| **Identity** | v1 | Core | User registration, authentication, authorization, roles |
| **Task** | v1 | Core | Task lifecycle, status management, metadata (title, description, priority, tags, due date) |
| **Board** | v1 | Core | Kanban boards, columns, card placement, spatial ordering of tasks |
| **Administration** | v1 | Supporting | Audit logs, user management, system health, protected data handling |
| **Onboarding** | v1 | Supporting | User journey tracking, guided tours, progress tracking |
| **Analytics** | v1 | Generic | Event collection, funnel tracking, metrics aggregation |
| **Notification** | v1 | Generic | Email sending, in-app notifications, push notifications |
| **Projects** | v2 | Core | Git repository registration, worktrees, dev servers, ngrok tunnels |
| **Comms** | v2 | Core | Unified communication inbox across Gmail, WhatsApp, Discord, Slack, LinkedIn, GitHub |
| **People** | v2 | Supporting | Person and company CRM — notes, contact handles, preferences, project links |
| **Agents** | v2 | Core | AI agent session lifecycle, work queue orchestration, budget management, human-in-the-loop approval |

### Context Files

| Context | Version | File |
|---------|---------|------|
| Identity | v1 | [contexts/identity.md](./contexts/identity.md) |
| Task | v1 | [contexts/tasks.md](./contexts/tasks.md) |
| Board | v1 | [contexts/boards.md](./contexts/boards.md) |
| Administration | v1 | [contexts/administration.md](./contexts/administration.md) |
| Onboarding | v1 | [contexts/onboarding.md](./contexts/onboarding.md) |
| Analytics | v1 | [contexts/analytics.md](./contexts/analytics.md) |
| Notification | v1 | [contexts/notification.md](./contexts/notification.md) |
| Projects | v2 Draft | [contexts/projects.md](./contexts/projects.md) |
| Comms | v2 Draft | [contexts/comms.md](./contexts/comms.md) |
| People | v2 Draft | [contexts/people.md](./contexts/people.md) |
| Agents | v2 Draft | [contexts/agents.md](./contexts/agents.md) |

---

## Context Relationships

### v1 Relationships (unchanged)

| Upstream | Downstream | Relationship |
|----------|------------|--------------|
| Identity | Task | Conformist (tasks reference user IDs) |
| Identity | Board | Conformist (boards reference user IDs) |
| Identity | Administration | Conformist (admin views user data) |
| Identity | Onboarding | Customer-Supplier (onboarding tracks identity events) |
| Task | Board | Conformist (board imports TaskId and TaskStatus from Task context) |
| Task | Analytics | Published Language (domain events -> analytics events) |
| Task | Onboarding | Published Language (task events -> onboarding progress) |
| Board | Analytics | Published Language (card events -> analytics events) |
| Identity | Notification | Customer-Supplier (user data for email) |
| Administration | Notification | Customer-Supplier (alerts, reports) |

### v2 Relationships

| Upstream | Downstream | Relationship | Integration Mechanism |
|----------|------------|--------------|----------------------|
| Identity | Projects | Conformist | `UserId` stored on Project, Worktree, DevServer, Tunnel for auth scoping |
| Identity | Comms | Conformist | `UserId` stored on Channel and Thread for auth scoping |
| Identity | People | Conformist | `UserId` stored on Person and Company as `OwnerId` |
| Identity | Agents | Conformist | `UserId` stored on AgentSession, WorkQueue, AgentTemplate as `OwnerId` |
| Task | Projects | Conformist (weak ref) | `TaskId` stored as opaque cross-context foreign reference on Project; no validation |
| Task | Comms | Conformist (weak ref) | `TaskId` stored in `CommLink` on Thread; no validation at write time |
| Task | Agents | Conformist (weak ref) | `TaskId` stored on AgentSession and WorkItem; Task context completes task on session approval |
| Projects | Comms | Conformist (weak ref) | `ProjectId` stored in `CommLink` on Thread; no validation at write time |
| Projects | People | Conformist (weak ref) | `ProjectId` stored in `ProjectLink` on Person and Company; no validation at write time |
| Projects | Agents | Customer-Supplier | Agents context publishes `AgentSessionStartedEvent`; Projects creates worktree. Agents publishes `AgentSessionApprovedEvent`; Projects merges worktree |
| People | Comms | Published Language | People publishes `PersonCreatedEvent` and `PersonContactHandleAddedEvent`; Comms subscribes to register handles for auto-linking incoming messages |
| Comms | People | ACL (read-only) | People reads linked message summaries from Comms via `ICommsReadService` ACL port for timeline and meeting briefing queries |
| Comms | Notification | Customer-Supplier | `MessageReceivedEvent` with priority >= High triggers Notification context to dispatch push/in-app alert |
| Agents | Task | Customer-Supplier | `AgentSessionApprovedEvent` triggers Task context to call `task.Complete()` for the linked task; `AgentApiTaskCreatedEvent` triggers `CreateTaskCommand` for agent-created follow-up tasks |
| Agents | Projects | Customer-Supplier | `AgentSessionStartedEvent` triggers Projects to create a worktree; `AgentSessionApprovedEvent` triggers Projects to merge and clean up the worktree |
| Agents | Notification | Customer-Supplier | `AgentSessionFailedEvent`, `SessionBudgetExhaustedEvent`, `SessionBudgetWarningEvent`, `AgentSessionApprovedEvent`, `WorkQueueCompletedEvent` all trigger Notification dispatches |

### Integration Pattern Key

| Pattern | Description |
|---------|-------------|
| **Conformist** | Downstream accepts upstream's model as-is; no translation layer |
| **Conformist (weak ref)** | Downstream stores an upstream ID as an opaque Guid; no cross-context loading or validation at write time |
| **Customer-Supplier** | Downstream depends on upstream, and upstream's team collaborates to meet downstream's needs |
| **Published Language** | Upstream publishes a stable event schema; downstream subscribes without tight coupling |
| **ACL (read-only)** | Downstream reads upstream data through an Anti-Corruption Layer port; upstream types never appear in downstream domain objects |

---

## Cross-Context Event Subscriptions

This table summarises which contexts subscribe to events published by other contexts. All subscriptions are handled by application-layer event handlers — never by direct domain object coupling.

| Event Published By | Event | Subscriber(s) | Handler Behaviour |
|--------------------|-------|---------------|-------------------|
| People | `PersonCreatedEvent` | Comms | Register person's email handles for incoming message auto-linking |
| People | `PersonContactHandleAddedEvent` | Comms | Register new handle for incoming message auto-linking |
| Projects | `PersonLinkedToProjectEvent` | People | No reverse subscription — People context writes the link; Projects stores the reference |
| Agents | `AgentSessionStartedEvent` | Projects | Create a git worktree matching `WorktreeRef` if set |
| Agents | `AgentSessionApprovedEvent` | Projects, Task, Notification | Projects: merge and clean up worktree; Task: call `task.Complete()` if `TaskId` set; Notification: send completion alert |
| Agents | `AgentSessionFailedEvent` | Notification | Send failure alert to user |
| Agents | `SessionBudgetExhaustedEvent` | Notification | Send budget exhaustion alert to user |
| Agents | `SessionBudgetWarningEvent` | Notification | Send budget 80% warning to user |
| Agents | `WorkQueueCompletedEvent` | Notification | Send queue completion summary to user |
| Agents | `AgentApiTaskCreatedEvent` | Task | Dispatch `CreateTaskCommand` with agent-supplied fields |
| Comms | `MessageReceivedEvent` (priority >= High) | Notification | Dispatch push/in-app notification to user |

---

## Shared Kernel

All v1 and v2 contexts use the shared kernel defined in [shared-kernel.md](./shared-kernel.md).

### Existing Shared Types (v1)

```
UserId              -> Guid wrapper (shared identity)
DateTimeOffset      -> All timestamps in UTC
Result<T, E>        -> Discriminated union for operation results
PagedResult<T>      -> { Items, TotalCount, Page, PageSize }
DomainEvent         -> Base class for all domain events
Entity<TId>         -> Base class with Id, CreatedAt, UpdatedAt
ValueObject         -> Base class with structural equality
```

### New Shared Types Needed for v2

These types appear as cross-context references in multiple v2 contexts and should be promoted to the shared kernel:

| Type | Currently defined in | Used by | Recommended Action |
|------|---------------------|---------|-------------------|
| `ProjectId` | Projects context (8.3) | People (ProjectLink), Comms (CommLink), Agents (AgentSession, WorkQueue) | Add to shared kernel as Guid wrapper |
| `TaskId` | Task context (v1) | Projects (LinkedTaskIds), Comms (CommLink), Agents (AgentSession, WorkItem) | Already in v1 shared vocabulary — confirm Guid wrapper is exported |
| `PersonId` | Projects context (8.3) / People context (8.3) | Projects (LinkedPersonIds), Comms (CommLink) | Owned by People context; Projects and Comms import it as a Guid wrapper |
| `WorktreeRef` | Agents context (8.3) | Used only by Agents — references Projects worktree by branch + path | Keep in Agents context for now; migrate to typed `WorktreeId` once Projects context is stable |

---

## Entity Relationship Diagram

### v1 Entities (unchanged)

```
+----------+       +-----------+
|   User   |1---N  |   Task    |
+----------+       +-----------+
| Id       |       | Id        |
| Email    |       | OwnerId  |----+
| Name     |       | Title     |    |
| Roles[]  |       | Priority  |    |
+----------+       | Status    |    |
     |             | DueDate   |    |
     |             | IsArchived|    |
     |             | Tags[]    |    |
     |             +-----------+    |
     |                  |           |
     |                  | TaskId    |
     |                  v           |
     |             +-----------+    |
     |             | TaskCard  |    |    +-----------+
     |             +-----------+    |    |  Column   |
     |             | TaskId   |----+    +-----------+
     |             | ColumnId |-------->| Id        |
     |             | Rank     |         | Name      |
     |             +-----------+         | TargetSt. |
     |                  |                | Pos       |
     |                  |                | MaxTasks  |
     |                  |                | NextRank  |
     |             +--------+            +-----------+
     |             | Board  |                 |
     |             +--------+                 |
     |1---N        | Id     |1---N  Columns---+
     +------------>| Name   |
     |             | Cards[]|1---N  TaskCards
     |             +--------+
     |
     |1---N  +------------------+
     +------>| AuditEntry       |
     |       +------------------+
     |       | Id               |
     |       | ActorId          |
     |       | Action           |
     |       | ResourceType     |
     |       | Details (redacted)|
     |       +------------------+
     |
     |1---1  +--------------------+
     +------>| OnboardingProgress |
     |       +--------------------+
     |       | UserId             |
     |       | Steps[]            |
     |       | IsCompleted        |
     |       +--------------------+
     |
     |1---N  +------------------+
     +------>| UserNotification |
             +------------------+
             | UserId           |
             | TemplateType     |
             | SentAt           |
             +------------------+
```

### v2 Aggregate Roots

```
+----------+
|   User   |  (Identity context)
+----------+
| Id       |
+----------+
     |
     | OwnerId (all v2 aggregates are user-scoped)
     |
     +-------+-------+-------+-------+
     |       |       |       |       |
     v       v       v       v       v

+---------+  +---------+  +---------+  +----------------+  +--------------+
| Project |  | Channel |  | Person  |  | AgentSession   |  |  WorkQueue   |
+---------+  +---------+  +---------+  +----------------+  +--------------+
| Id      |  | Id      |  | Id      |  | Id             |  | Id           |
| Name    |  | Type    |  | Name    |  | Status         |  | Name         |
| Path    |  | Status  |  | Emails  |  | Budget         |  | ExecutionMode|
| Status  |  | Filter  |  | Tags    |  | WorktreeRef?   |  | Items[]      |
| Tasks[] |  +---------+  | Notes[] |  | Output?        |  +--------------+
| People[]|       |       +---------+       |
+---------+       |          |   |          |
    |             |          |   |          |
    | 1-N         | 1-N      |   | Company  | TaskId (weak ref -> Task)
    v             v          v   v          |
+-----------+  +--------+  +-----+  +---+  v
| Worktree  |  | Thread |  | Note|  |Co.|  +---> Task (Task context, cross-ref)
+-----------+  +--------+  +-----+  +---+
| Id        |  | Id     |
| Branch    |  | Channel|
| Status    |  | Subject|        +--------------+
| Ahead     |  | Msgs[] |        | AgentTemplate|
| Behind    |  | Links[]|        +--------------+
+-----------+  +--------+        | Id           |
    |                            | ModelId      |
    | 1-N                        | Rules[]      |
    v                            | Schedule?    |
+----------+                     +--------------+
| DevServer|
+----------+
| Id       |
| Command  |
| Port     |
| Status   |
+----------+
    |
    | 0-1
    v
+--------+
| Tunnel |
+--------+
| Id     |
| Url    |
| Status |
+--------+
```
