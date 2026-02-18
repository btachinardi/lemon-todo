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

LemonDo is designed around Domain-Driven Design (DDD) with CQRS-light using Use Cases. The domain is decomposed into seven v1 bounded contexts, with four additional contexts planned for v2.

The three core v1 contexts are **Identity** (auth and RBAC), **Task** (task lifecycle and metadata), and **Board** (kanban spatial layout). Task and Board are deliberately separated: Task owns status transitions while Board owns card placement. Cross-context coordination is handled at the application layer by command handlers, never by direct coupling between domain objects.

The two supporting contexts are **Administration** (audit logs, protected data reveal, user management) and **Onboarding** (guided user journey tracking). The two generic contexts are **Analytics** (event collection and metrics) and **Notification** (email and in-app messaging).

All bounded contexts share a common kernel of primitive types: `UserId`, `Result<T, E>`, `PagedResult<T>`, `DomainEvent`, `Entity<TId>`, and `ValueObject`. All timestamps are `DateTimeOffset` in UTC.

---

## Bounded Contexts Map

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

### Context Map

| Context | Type | Responsibility |
|---------|------|----------------|
| **Identity** | Core | User registration, authentication, authorization, roles |
| **Task** | Core | Task lifecycle, status management, metadata (title, description, priority, tags, due date) |
| **Board** | Core | Kanban boards, columns, card placement, spatial ordering of tasks |
| **Administration** | Supporting | Audit logs, user management, system health, protected data handling |
| **Onboarding** | Supporting | User journey tracking, guided tours, progress tracking |
| **Analytics** | Generic | Event collection, funnel tracking, metrics aggregation |
| **Notification** | Generic | Email sending, in-app notifications, push notifications |

### Context Relationships

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

---

## Entity Relationship Diagram

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
