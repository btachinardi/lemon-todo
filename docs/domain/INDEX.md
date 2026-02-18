# Domain Design

> DDD domain design for LemonDo: bounded contexts, shared kernel, entity relationships, and API design.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [shared-kernel.md](./shared-kernel.md) | Shared types used across all bounded contexts (UserId, Result, Entity, etc.) | Active |
| [api-design.md](./api-design.md) | All API endpoints by context and cross-cutting concerns | Active |
| [contexts/](./contexts/) | Individual bounded context definitions | — |
| [contexts/bridges/](./contexts/bridges/) | Integration bridge contexts connecting core domains | — |

---

## Summary

LemonDo is designed around Domain-Driven Design (DDD) with CQRS-light using Use Cases. The domain is decomposed into eleven bounded contexts: seven active contexts from v1 and four planned contexts for v2.

The three core v1 contexts are **Identity** (auth and RBAC), **Task** (task lifecycle and metadata), and **Board** (kanban spatial layout). Task and Board are deliberately separated: Task owns status transitions while Board owns card placement. Cross-context coordination is handled at the application layer by command handlers, never by direct coupling between domain objects.

The two supporting v1 contexts are **Administration** (audit logs, protected data reveal, user management) and **Onboarding** (guided user journey tracking). The two generic v1 contexts are **Analytics** (event collection and metrics) and **Notification** (email and in-app messaging).

The four v2 contexts extend LemonDo into Bruno's personal development command center. **Projects** owns git repository registration, worktrees, dev servers, and ngrok tunnels. **Comms** owns a unified communication inbox across Gmail, WhatsApp, Discord, Slack, LinkedIn, and GitHub. **People** owns a lightweight CRM for tracking persons and companies with notes, preferences, and project links. **Agents** owns AI agent session lifecycle and budget management.

Three **bridge contexts** coordinate the v2 core domains without coupling them. **ProjectAgentBridge** owns the session-to-worktree correlation and the WorkQueue aggregate (moved here from Agents). **AgentTaskBridge** owns the session-to-task correlation and drives task completion on session approval. **ProjectTaskBridge** owns the project-to-task link records and the auto-complete-on-merge behaviour.

All bounded contexts share a common kernel of primitive types: `UserId`, `Result<T, E>`, `PagedResult<T>`, `DomainEvent`, `Entity<TId>`, and `ValueObject`. All timestamps are `DateTimeOffset` in UTC.

---

## Bounded Contexts Map

### Active Contexts

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

### Planned Contexts (Draft)

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
     |                  |              |  ^              |
     |        ProjectId (weak ref)     |  |              |
     |          +--------+            |  +--PersonCreated|
     |          |                     |    HandleAdded   |
     |                                |                  |
     |           ...mediated by bridge contexts below... |
     +--------------------------------------------------++
                           |
          +----------------+--------------------+
          |                |                    |
          v                v                    v
+--------------------+ +------------------+ +------------------+
| ProjectAgentBridge | | AgentTaskBridge  | | ProjectTaskBridge|
| (session+worktree  | | (session+task    | | (project+task    |
|  correlation,      | |  correlation,    | |  links, auto-    |
|  WorkQueue)        | |  task completion)| |  complete)       |
+--------------------+ +------------------+ +------------------+
          |                |                    |
          v                v                    v
+---------+           +-----------+        +----------+
| Projects|           |   Tasks   |        |   Tasks  |
|(worktree|           |(complete  |        |(complete  |
| create/ |           | on approve|        | on merge) |
| delete) |           +----------+        +----------+
+---------+
          |
          v
+-------------------+
|   Notification    |
| (alerts, budget,  |
|  failures, queues)|
+-------------------+
```

### Bridge Context Pattern

Bridge contexts are thin integration bounded contexts that sit between two core domains. They own:
- Correlation aggregates (mapping IDs between contexts)
- Cross-context workflow state (multi-step operations spanning two contexts)
- Rich cross-context queries (hydrated views of data from two contexts)

They do NOT own domain logic that belongs in either upstream context.

See [contexts/bridges/INDEX.md](./contexts/bridges/INDEX.md) for the bridge pattern documentation.

---

## Context Map

### All Bounded Contexts

| Context | Status | Type | Responsibility |
|---------|--------|------|----------------|
| **Identity** | Active | Core | User registration, authentication, authorization, roles |
| **Task** | Active | Core | Task lifecycle, status management, metadata (title, description, priority, tags, due date) |
| **Board** | Active | Core | Kanban boards, columns, card placement, spatial ordering of tasks |
| **Administration** | Active | Supporting | Audit logs, user management, system health, protected data handling |
| **Onboarding** | Active | Supporting | User journey tracking, guided tours, progress tracking |
| **Analytics** | Active | Generic | Event collection, funnel tracking, metrics aggregation |
| **Notification** | Active | Generic | Email sending, in-app notifications, push notifications |
| **Projects** | Draft | Core | Git repository registration, worktrees, dev servers, ngrok tunnels |
| **Comms** | Draft | Core | Unified communication inbox across Gmail, WhatsApp, Discord, Slack, LinkedIn, GitHub |
| **People** | Draft | Supporting | Person and company CRM — notes, contact handles, preferences, project links |
| **Agents** | Draft | Core | AI agent session lifecycle, budget management, human-in-the-loop approval |
| **ProjectAgentBridge** | Draft | Bridge | Session-to-worktree correlation; WorkQueue orchestration; "start in worktree" and "merge on approve" workflows |
| **AgentTaskBridge** | Draft | Bridge | Session-to-task correlation; task completion on session approval; agent-created task linkage |
| **ProjectTaskBridge** | Draft | Bridge | Project-to-task link records; cross-context task queries; auto-complete tasks on worktree merge |

### Context Files

| Context | Status | File |
|---------|--------|------|
| Identity | Active | [contexts/identity.md](./contexts/identity.md) |
| Task | Active | [contexts/tasks.md](./contexts/tasks.md) |
| Board | Active | [contexts/boards.md](./contexts/boards.md) |
| Administration | Active | [contexts/administration.md](./contexts/administration.md) |
| Onboarding | Active | [contexts/onboarding.md](./contexts/onboarding.md) |
| Analytics | Active | [contexts/analytics.md](./contexts/analytics.md) |
| Notification | Active | [contexts/notification.md](./contexts/notification.md) |
| Projects | Draft | [contexts/projects.md](./contexts/projects.md) |
| Comms | Draft | [contexts/comms.md](./contexts/comms.md) |
| People | Draft | [contexts/people.md](./contexts/people.md) |
| Agents | Draft | [contexts/agents.md](./contexts/agents.md) |
| ProjectAgentBridge | Draft | [contexts/bridges/project-agent-bridge.md](./contexts/bridges/project-agent-bridge.md) |
| AgentTaskBridge | Draft | [contexts/bridges/agent-task-bridge.md](./contexts/bridges/agent-task-bridge.md) |
| ProjectTaskBridge | Draft | [contexts/bridges/project-task-bridge.md](./contexts/bridges/project-task-bridge.md) |

---

## Context Relationships

### Core Relationships (Active)

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

### Planned Context Relationships (Draft)

| Upstream | Downstream | Relationship | Integration Mechanism |
|----------|------------|--------------|----------------------|
| Identity | Projects | Conformist | `UserId` stored on Project, Worktree, DevServer, Tunnel for auth scoping |
| Identity | Comms | Conformist | `UserId` stored on Channel and Thread for auth scoping |
| Identity | People | Conformist | `UserId` stored on Person and Company as `OwnerId` |
| Identity | Agents | Conformist | `UserId` stored on AgentSession, AgentTemplate as `OwnerId` |
| Task | Projects | Conformist (weak ref) | `TaskId` stored as opaque cross-context foreign reference on Project; no validation |
| Task | Comms | Conformist (weak ref) | `TaskId` stored in `CommLink` on Thread; no validation at write time |
| Projects | Comms | Conformist (weak ref) | `ProjectId` stored in `CommLink` on Thread; no validation at write time |
| Projects | People | Conformist (weak ref) | `ProjectId` stored in `ProjectLink` on Person and Company; no validation at write time |
| People | Comms | Published Language | People publishes `PersonCreatedEvent` and `PersonContactHandleAddedEvent`; Comms subscribes to register handles for auto-linking incoming messages |
| Comms | People | ACL (read-only) | People reads linked message summaries from Comms via `ICommsReadService` ACL port for timeline and meeting briefing queries |
| Comms | Notification | Customer-Supplier | `MessageReceivedEvent` with priority >= High triggers Notification context to dispatch push/in-app alert |

### Bridge Relationships (Draft)

| Upstream | Bridge | Downstream | Mechanism |
|----------|--------|------------|-----------|
| Agents | ProjectAgentBridge | Projects | Bridge subscribes to `AgentSessionApprovedEvent` and `AgentSessionFailedEvent`; dispatches `CreateWorktreeCommand` and `DeleteWorktreeCommand` to Projects |
| Projects | ProjectAgentBridge | Agents | Bridge subscribes to `WorktreeCreatedEvent` and `WorktreeDeletedEvent`; resolves `WorkingDirectory` to pass to Agents via `StartAgentSessionCommand` |
| Agents | AgentTaskBridge | Tasks | Bridge subscribes to `AgentSessionApprovedEvent`, `AgentSessionFailedEvent`, `AgentSessionRejectedEvent`, `AgentApiTaskCreatedEvent`; dispatches `CompleteTaskCommand` to Tasks when approved |
| Projects | ProjectTaskBridge | Tasks | Bridge subscribes to `TaskLinkedToProjectEvent`, `TaskUnlinkedFromProjectEvent`, `WorktreeDeletedEvent`; dispatches `CompleteTaskCommand` to Tasks for auto-complete links |

### Integration Pattern Key

| Pattern | Description |
|---------|-------------|
| **Conformist** | Downstream accepts upstream's model as-is; no translation layer |
| **Conformist (weak ref)** | Downstream stores an upstream ID as an opaque Guid; no cross-context loading or validation at write time |
| **Customer-Supplier** | Downstream depends on upstream, and upstream's team collaborates to meet downstream's needs |
| **Published Language** | Upstream publishes a stable event schema; downstream subscribes without tight coupling |
| **ACL (read-only)** | Downstream reads upstream data through an Anti-Corruption Layer port; upstream types never appear in downstream domain objects |
| **Bridge** | A thin integration context that owns the cross-context correlation state and workflow; sits between two core contexts that must not be directly coupled |

---

## Cross-Context Event Subscriptions

This table summarises which contexts subscribe to events published by other contexts. All subscriptions are handled by application-layer event handlers — never by direct domain object coupling.

### Core Context Subscriptions

| Event Published By | Event | Subscriber(s) | Handler Behaviour |
|--------------------|-------|---------------|-------------------|
| People | `PersonCreatedEvent` | Comms | Register person's email handles for incoming message auto-linking |
| People | `PersonContactHandleAddedEvent` | Comms | Register new handle for incoming message auto-linking |
| Agents | `SessionBudgetExhaustedEvent` | Notification | Send budget exhaustion alert to user |
| Agents | `SessionBudgetWarningEvent` | Notification | Send budget 80% warning to user |
| Agents | `AgentSessionFailedEvent` | Notification | Send failure alert to user |
| Agents | `AgentSessionApprovedEvent` | Notification | Send completion alert to user |
| Comms | `MessageReceivedEvent` (priority >= High) | Notification | Dispatch push/in-app notification to user |

### Bridge Context Subscriptions

| Event Published By | Event | Bridge Subscriber | Handler Behaviour |
|--------------------|-------|------------------|-------------------|
| Agents | `AgentSessionStartedEvent` | ProjectAgentBridge | Create correlation; begin worktree resolution if required |
| Agents | `AgentSessionApprovedEvent` | ProjectAgentBridge | Begin worktree merge; advance work queue if queue item |
| Agents | `AgentSessionFailedEvent` | ProjectAgentBridge | Fail correlation; record queue item failure |
| Agents | `SessionCancelledEvent` | ProjectAgentBridge | Cancel correlation if still pending resolution |
| Agents | `AgentSessionStartedEvent` | AgentTaskBridge | Create task correlation record |
| Agents | `AgentSessionApprovedEvent` | AgentTaskBridge | Complete primary task; mark correlation TaskCompleted |
| Agents | `AgentSessionRejectedEvent` | AgentTaskBridge | Mark correlation Failed; do not complete task |
| Agents | `AgentSessionFailedEvent` | AgentTaskBridge | Mark correlation Failed; do not complete task |
| Agents | `AgentApiTaskCreatedEvent` | AgentTaskBridge | Record follow-up task on correlation |
| Projects | `WorktreeCreatedEvent` | ProjectAgentBridge | Activate correlation with resolved WorkingDirectory |
| Projects | `WorktreeDeletedEvent` | ProjectAgentBridge | Complete merge on correlation |
| Projects | `TaskLinkedToProjectEvent` | ProjectTaskBridge | Create active ProjectTaskLink record |
| Projects | `TaskUnlinkedFromProjectEvent` | ProjectTaskBridge | Deactivate ProjectTaskLink record |
| Projects | `WorktreeDeletedEvent` | ProjectTaskBridge | Auto-complete tasks where AutoCompleteOnMerge = true |
| ProjectAgentBridge | `WorkQueueCompletedEvent` | Notification | Send queue completion summary to user |
| ProjectAgentBridge | `WorkQueueItemFailedEvent` | Notification | Send queue item failure alert to user |

---

## Shared Kernel

All active and planned contexts use the shared kernel defined in [shared-kernel.md](./shared-kernel.md).

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

### Planned Shared Types (Draft)

These types appear as cross-context references in multiple v2 contexts and should be promoted to the shared kernel:

| Type | Currently defined in | Used by | Recommended Action |
|------|---------------------|---------|-------------------|
| `ProjectId` | Projects context (8.3) | People (ProjectLink), Comms (CommLink), ProjectAgentBridge, ProjectTaskBridge | Add to shared kernel as Guid wrapper |
| `TaskId` | Task context (v1) | Projects (LinkedTaskIds), Comms (CommLink), AgentTaskBridge, ProjectTaskBridge | Already in v1 shared vocabulary — confirm Guid wrapper is exported |
| `WorktreeId` | Projects context (8.3) | ProjectAgentBridge (correlation), AgentTaskBridge (WorkItem) | Add to shared kernel as Guid wrapper once Projects context is stable |
| `AgentSessionId` | Agents context (8.5) | ProjectAgentBridge (correlation), AgentTaskBridge (correlation) | Add to shared kernel as Guid wrapper |
| `PersonId` | People context | Projects (LinkedPersonIds), Comms (CommLink) | Owned by People context; Projects and Comms import it as a Guid wrapper |
| `WorktreeRef` | Agents context (8.5) | Used only by Agents — references Projects worktree by branch + path | Migrate to `WorktreeId` once ProjectAgentBridge is in place; keep for now for backwards compatibility |

---

## Entity Relationship Diagram

### Active Entities

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

### Planned Aggregate Roots (Draft)

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
| Project |  | Channel |  | Person  |  | AgentSession   |  | AgentTemplate|
+---------+  +---------+  +---------+  +----------------+  +--------------+
| Id      |  | Id      |  | Id      |  | Id             |  | Id           |
| Name    |  | Type    |  | Name    |  | Status         |  | Name         |
| Path    |  | Status  |  | Emails  |  | Budget         |  | ModelId      |
| Status  |  | Filter  |  | Tags    |  | WorktreeRef?   |  | Rules[]      |
| Tasks[] |  +---------+  | Notes[] |  | Output?        |  | Schedule?    |
| People[]|       |       +---------+       |            +--------------+
+---------+       |          |   |          |
    |             |          |   |          |
    | 1-N         | 1-N      |   | Company  | TaskId (weak ref -> Task)
    v             v          v   v          |
+-----------+  +--------+  +-----+  +---+  v
| Worktree  |  | Thread |  | Note|  |Co.|  +---> Task (Task context, cross-ref)
+-----------+  +--------+  +-----+  +---+
| Id        |  | Id     |
| Branch    |  | Channel|
| Status    |  | Subject|
| Ahead     |  | Msgs[] |
| Behind    |  | Links[]|
+-----------+  +--------+
    |
    | 1-N
    v
+----------+
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

### Bridge Aggregate Roots (Draft)

```
+------------------------------+   +------------------------------+
| AgentProjectCorrelation      |   | WorkQueue                    |
| (ProjectAgentBridge)         |   | (ProjectAgentBridge)         |
+------------------------------+   +------------------------------+
| CorrelationId                |   | WorkQueueId                  |
| AgentSessionId               |   | ProjectId                    |
| ProjectId                    |   | ExecutionMode                |
| WorktreeId?                  |   | Status                       |
| TaskId?                      |   | Items: WorkItem[]            |
| WorkQueueId?                 |   | Budget?                      |
| WorkItemId?                  |   +------------------------------+
| WorkingDirectory             |
| Status: CorrelationStatus    |
+------------------------------+

+------------------------------+   +------------------------------+
| AgentTaskCorrelation         |   | ProjectTaskLink              |
| (AgentTaskBridge)            |   | (ProjectTaskBridge)          |
+------------------------------+   +------------------------------+
| AgentTaskCorrelationId       |   | ProjectTaskLinkId            |
| AgentSessionId               |   | ProjectId                    |
| PrimaryTaskId?               |   | TaskId                       |
| FollowUpTaskIds[]            |   | AutoCompleteOnMerge          |
| Status: CorrelationStatus    |   | IsActive                     |
+------------------------------+   +------------------------------+
```
