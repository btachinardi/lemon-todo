# Bounded Contexts

> All bounded context definitions for the LemonDo domain, including entities, value objects, domain events, use cases, and repository interfaces.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [identity.md](./identity.md) | User registration, authentication, authorization, roles, protected data | Active |
| [tasks.md](./tasks.md) | Task lifecycle, status management, metadata (title, description, priority, tags, due date) | Active |
| [boards.md](./boards.md) | Kanban boards, columns, card placement, spatial ordering, cross-context coordination | Active |
| [administration.md](./administration.md) | Audit logs, user management, system health, protected data handling | Active |
| [onboarding.md](./onboarding.md) | User journey tracking, guided tours, progress tracking | Active |
| [analytics.md](./analytics.md) | Event collection, funnel tracking, metrics aggregation | Active |
| [notifications.md](./notifications.md) | Email sending, in-app notifications, push notifications | Active |
| [projects.md](./projects.md) | Project management context (v2) | Draft (v2) |
| [comms.md](./comms.md) | Communications context (v2) | Draft (v2) |
| [people.md](./people.md) | People and companies context (v2) | Draft (v2) |
| [agents.md](./agents.md) | Agent sessions context (v2) | Draft (v2) |

---

## Summary

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

### Context Types

| Context | Type | Responsibility |
|---------|------|----------------|
| **Identity** | Core | User registration, authentication, authorization, roles |
| **Task** | Core | Task lifecycle, status management, metadata (title, description, priority, tags, due date) |
| **Board** | Core | Kanban boards, columns, card placement, spatial ordering of tasks |
| **Administration** | Supporting | Audit logs, user management, system health, protected data handling |
| **Onboarding** | Supporting | User journey tracking, guided tours, progress tracking |
| **Analytics** | Generic | Event collection, funnel tracking, metrics aggregation |
| **Notification** | Generic | Email sending, in-app notifications, push notifications |
| **Projects** | Core (v2) | Project management — to be designed in Phase 2 |
| **Communications** | Core (v2) | Communication management — to be designed in Phase 2 |
| **People** | Supporting (v2) | People and companies — to be designed in Phase 2 |
| **Agents** | Generic (v2) | Agent sessions — to be designed in Phase 2 |
