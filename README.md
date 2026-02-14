# LemonDo

> A task management platform that combines consumer-grade UX with enterprise-grade compliance.

[![Build Status](https://img.shields.io/badge/build-planning-yellow)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)]()
[![React](https://img.shields.io/badge/React-19-blue)]()
[![Aspire](https://img.shields.io/badge/Aspire-13-orange)]()

---

## What is LemonDo?

LemonDo is a full-stack task management platform built with .NET Aspire and React. It features a Kanban board, list view, HIPAA-compliant data handling, role-based access control, and a delightful onboarding experience. It's designed for individuals and small teams in regulated industries who need simplicity without sacrificing compliance.

### Key Features

- **Kanban Board & List View** - Visualize work your way
- **HIPAA Compliance** - PII encryption, redaction, and audit trails
- **Role-Based Access Control** - User, Admin, SystemAdmin roles
- **Beautiful Onboarding** - Guided tour from signup to first completed task
- **Mobile-First PWA** - Works offline, installable, responsive
- **Dark & Light Themes** - System-aware with manual toggle
- **Multi-Language** - English, Portuguese, Spanish
- **Full Observability** - OpenTelemetry traces, metrics, and structured logs

---

## How We Built This: A Development Journal

This README documents our complete thought process, from inception to production. Every decision, every phase, every lesson learned.

### The Starting Point

**Date: February 13, 2026**

We started with an empty folder and a clear vision: build a production-grade task management platform that proves you can have great UX AND compliance. Not one or the other.

Our constraints:
- .NET Aspire for cloud-native orchestration
- React + Shadcn/ui for a premium frontend
- Strict TDD methodology
- DDD architecture throughout
- HIPAA-level data protection

### Phase 1: Planning Before Code

We believe planning is not wasted time. It's compressed debugging. We created five foundational documents before writing a single line of code:

#### 1.1 Product Requirements Document (PRD)

Our first document ([docs/PRD.md](./docs/PRD.md)) captured everything we knew we needed:
- 10 functional requirement groups (FR-001 through FR-010)
- 10 non-functional requirement groups (NFR-001 through NFR-010)
- Success metrics with concrete targets
- Risk assessment with mitigations
- Clear "out of scope" boundaries to prevent scope creep

**Decision**: We chose Scalar over Swagger for API documentation. Starting with .NET 9, Scalar became the modern default. It loads faster, has better search, and its dark mode matches our premium UI goals.

**Decision**: SQLite for MVP. Some might call this controversial for a "HIPAA-compliant" app. Our reasoning: SQLite is more than capable for our MVP scale, the repository pattern makes swapping to PostgreSQL a one-file change, and it eliminates infrastructure complexity during development.

#### 1.2 Technology Research

We researched every technology we'd use ([docs/RESEARCH.md](./docs/RESEARCH.md)), verifying:
- Latest stable versions (not bleeding edge, not outdated)
- Compatibility between all pieces of the stack
- Features relevant to our requirements

Key findings:
- **.NET 10** is the current LTS (3-year support). We're using 10.0.103.
- **Aspire 13** dropped the ".NET" prefix and added `AddJavaScriptApp` which auto-generates Dockerfiles for our React frontend.
- **Vite 7** is the latest major version (7.3.1). Vite 6 is now in maintenance.
- **React 19.2** brought the React Compiler for automatic memoization.
- **Shadcn/ui** added component styles (Vega, Nova, etc.) and Base UI support in February 2026.

#### 1.3 User Scenarios

This is where our planning leveled up. Instead of jumping to domain design, we wrote detailed storyboards ([docs/SCENARIOS.md](./docs/SCENARIOS.md)) from the USER's perspective:

We created three personas:
- **Sarah** (Freelancer): Needs quick task capture on mobile
- **Marcus** (Team Lead): Needs Kanban with compliance
- **Diana** (System Admin): Needs audit trails and PII management

Then we walked through 10 scenarios step-by-step, documenting:
- What the user sees at each step
- What they expect to happen
- What emotions they should feel
- What analytics events we should track

**Insight**: This exercise revealed that quick-add (title-only, one tap) is THE killer feature. Our PRD originally required title + description for task creation. The scenario analysis showed that Sarah creates tasks in 2-second bursts while walking between meetings. We changed the minimum to title-only.

**Insight**: Offline support isn't a nice-to-have. Sarah's airplane scenario proved that creating and completing tasks offline is essential, not just viewing.

**Our North Star Metric**: Weekly Active Task Completers (WATC). A user who completes at least one task per week. This measures actual value delivery, correlates with retention, and is not gameable.

#### 1.4 Revised PRD

After scenarios, we created [docs/PRD.reviewed.md](./docs/PRD.reviewed.md) - a revised PRD that incorporated everything we learned. Key changes:
- Quick-add promoted to P0
- Onboarding celebrations upgraded from P1 to P0
- Offline CRUD (not just viewing) became a requirement
- PII default-redacted in admin views (not opt-in redaction, but opt-in reveal)
- New NFR section for micro-interactions and UX polish

We kept the original PRD intact to show our evolution.

#### 1.5 Domain Design

With requirements solid, we designed our domain ([docs/DOMAIN.md](./docs/DOMAIN.md)):

**6 Bounded Contexts**:
1. **Identity** - Users, roles, authentication
2. **Task Management** - Tasks, boards, columns (core domain)
3. **Administration** - Audit logs, PII handling, system health
4. **Onboarding** - User journey tracking
5. **Analytics** - Event collection (privacy-first)
6. **Notification** - Emails, in-app alerts

**Key Domain Decisions**:
- `TaskItem` (not `Task`) to avoid conflict with `System.Threading.Tasks.Task`
- Value objects for ALL identifiers (`UserId`, `TaskItemId`, etc.) - type safety over primitive obsession
- Domain events on every mutation - enables audit trail and analytics without coupling
- `Result<T, E>` pattern instead of exceptions for business logic
- `RedactedString` value object that holds encrypted original + masked display

### Phase 2: Development Guidelines

Before touching code, we established our rules of engagement ([GUIDELINES.md](./GUIDELINES.md)):

- **Strict TDD**: RED-GREEN-VALIDATE. No production code without a failing test.
- **Frontend Architecture**: Two orthogonal systems — Architecture Tiers (Routing -> Pages & Layouts -> State Management -> Components) for separation of concerns, and Component Taxonomy (Design System -> Domain Atoms -> Domain Widgets -> Domain Views) for composition granularity.
- **Gitflow**: main + develop + feature branches. Conventional commits. Atomic commits.
- **Security**: PII redaction in logs, OWASP Top 10 compliance, rate limiting.
- **Accessibility**: WCAG 2.1 AA minimum, Radix primitives for built-in a11y.

### Interlude: The State Management Gap

During our checkpoint review of GUIDELINES.md, we realized we had a significant blind spot: **no explicit state management strategy for the frontend**. Our original component architecture described what components render, but not how they get their data.

We added two critical libraries to the stack:

**TanStack Query 5** for server state (data from the API):
- Replaces the `useState` + `useEffect` + `fetch` anti-pattern
- Automatic caching, deduplication, and background refetching
- Offline mutation queue - critical for our PWA scenario (Sarah on a plane)
- Optimistic updates for that instant-feeling UI

**Zustand 5** for client state (UI preferences, form drafts, offline queue):
- No provider wrapper needed (unlike Redux or Context)
- Built-in `persist` middleware for localStorage/IndexedDB
- Tiny (~1KB) - important for our mobile-first PWA

The key rule: **TanStack Query owns all server data, Zustand owns all client state, React Context is only for low-frequency cross-cutting providers.** Components never mix `fetch` calls with rendering.

### Interlude: Untangling "Layers"

We realized we were conflating two orthogonal concepts under the same "layer" word. What we actually have are two independent organizational systems:

**Architecture Tiers** answer *"what is this code responsible for?"* — separation of concerns:
```
Routing → Pages & Layouts → State Management → Components
```

**Component Taxonomy** answers *"how big and domain-aware is this UI piece?"* — composition granularity:
```
Design System → Domain Atoms → Domain Widgets → Domain Views
```

The old L1/L2/L3 labels tried to do both jobs at once and created confusion. The new model is cleaner: Architecture Tiers flow data top-down (from URL to pixels), while the Component Taxonomy flows bottom-up (small primitives compose into bigger domain-aware pieces). See [GUIDELINES.md](./GUIDELINES.md) for the full specification with examples and import rules.

### Interlude: Rethinking Delivery Strategy

After running review agents against our documentation, we faced a hard truth: we were planning to build everything at once. Six bounded contexts, HIPAA compliance, three languages, offline CRUD, analytics — all marked as P0. The reviewers flagged this as over-engineering, and they were right.

But the ambition isn't wrong — the delivery order is. We restructured into **checkpoint-based delivery**: five incremental checkpoints where each one produces a complete, runnable application. If we stop at any checkpoint, we have something presentable that demonstrates real architecture and thought process.

**The key insight**: Checkpoint 1 addresses *every* core evaluation criteria (API design, data structures, component design, F/E↔B/E communication, clean code). Later checkpoints layer on production features progressively.

| Checkpoint | Focus | Key Trade-off |
|---|---|---|
| **CP1: Core Tasks** | Working full-stack CRUD | Single-user mode (no auth yet) |
| **CP2: Auth** | User accounts, JWT, RBAC | Two roles (User, Admin), not three |
| **CP3: Rich UX** | Drag-drop, theme, quick-add, polish | Theme before i18n |
| **CP4: Production** | Observability, PII, audit, i18n | "HIPAA-Ready" not certified |
| **CP5: Advanced** | PWA, onboarding, analytics, E2E | Lightweight implementations proving architecture |

**Decision**: Tasks before Auth. A bold but deliberate choice. CP1 runs in single-user mode so we can demonstrate clean architecture end-to-end without auth complexity. The repository pattern means adding user-scoped queries in CP2 is a one-line change — proving the architecture's extensibility.

**Decision**: HIPAA downgraded from P0 to "HIPAA-Ready infrastructure." Full HIPAA compliance requires BAAs, legal review, workforce training, and incident response procedures — that's a business framework, not a codebase feature. We implement the *technical controls* (encryption, audit trails, PII redaction) and document what's needed for full certification.

See [TASKS.md](./TASKS.md) for the complete checkpoint breakdown with every task.

### Phase 3: Codebase Bootstrap

*Coming next: Initialize the .NET Aspire solution with DDD project structure, scaffold the React frontend with Tailwind + Shadcn/ui, wire up test infrastructure (xUnit, FsCheck, Vitest, fast-check), connect Aspire ↔ React via AddJavaScriptApp, and verify health checks + Scalar API docs are serving.*

### Checkpoint 1: Core Task Management

*Planned: Full-stack task CRUD in single-user mode. Backend: TaskItem/Board/Column entities with TDD, EF Core + SQLite, minimal API endpoints. Frontend: Design System setup, Domain Components (Atoms/Widgets/Views), TanStack Query hooks, Zustand stores, React Router. Integration tests for all endpoints. Working kanban board and list view.*

### Checkpoint 2: Authentication & Authorization

*Planned: ASP.NET Core Identity with JWT tokens, register/login/logout endpoints, user-scoped task queries, React auth pages with route guards and redirects, token refresh handling.*

### Checkpoint 3: Rich UX & Polish

*Planned: Kanban drag-and-drop, quick-add, task detail editing, filters/search, dark/light theme, responsive design, loading skeletons, empty states, toast notifications, error boundaries.*

### Checkpoint 4: Production Hardening

*Planned: OpenTelemetry + Serilog observability, PII redaction in admin views, audit trail, admin panel, SystemAdmin role, i18n (en + pt-BR), rate limiting, data encryption at rest.*

### Checkpoint 5: Advanced & Delight

*Planned: PWA with offline support, onboarding flow, analytics event tracking, in-app notifications, Playwright E2E tests, Spanish language, offline mutation queue.*

---

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Backend** | .NET 10 LTS | 10.0 |
| **Orchestration** | Aspire 13 | 13.x |
| **ORM** | Entity Framework Core 10 | 10.x |
| **Database** | SQLite | 3.x |
| **Auth** | ASP.NET Core Identity | 10.x |
| **API Docs** | Scalar | Latest |
| **Frontend** | React 19 + TypeScript | 19.x |
| **Build** | Vite 7 | 7.x |
| **UI** | Shadcn/ui + Radix | Latest |
| **Styling** | Tailwind CSS | 4.x |
| **Routing** | React Router | 7.x |
| **Server State** | TanStack Query 5 | 5.x |
| **Client State** | Zustand 5 | 5.x |
| **i18n** | react-i18next | 16.x |
| **PWA** | vite-plugin-pwa | Latest |
| **Backend Tests** | xUnit + FsCheck | Latest |
| **Frontend Tests** | Vitest + fast-check | 3.x |
| **E2E Tests** | Playwright | 1.58+ |
| **Observability** | OpenTelemetry + Serilog | Latest |
| **CI/CD** | GitHub Actions | Latest |
| **IaC** | Terraform + Azure | Latest |
| **Containers** | Docker | Latest |

---

## Project Structure

```
lemon-todo/
├── docs/                          # Design documents
│   ├── PRD.md                     # Initial product requirements
│   ├── PRD.reviewed.md            # Revised requirements post-scenarios
│   ├── RESEARCH.md                # Technology research
│   ├── SCENARIOS.md               # User storyboards and analytics
│   └── DOMAIN.md                  # DDD domain design
├── src/                           # Source code
│   ├── LemonDo.AppHost/           # Aspire orchestrator
│   ├── LemonDo.ServiceDefaults/   # Shared Aspire configuration
│   ├── LemonDo.Api/               # ASP.NET Core API
│   ├── LemonDo.Application/       # Use cases (commands + queries)
│   ├── LemonDo.Domain/            # Pure domain (entities, VOs, events)
│   ├── LemonDo.Infrastructure/    # EF Core, external services
│   └── client/                    # Vite + React frontend
├── tests/                         # Test projects
│   ├── LemonDo.Domain.Tests/      # Domain unit + property tests
│   ├── LemonDo.Application.Tests/ # Use case tests
│   ├── LemonDo.Api.Tests/         # API integration tests
│   └── LemonDo.E2E.Tests/         # Playwright E2E tests
├── infra/                         # Infrastructure
│   ├── terraform/                 # Azure IaC
│   └── docker/                    # Dockerfiles
├── .github/                       # GitHub Actions workflows
├── TASKS.md                       # Project task tracker
├── GUIDELINES.md                  # Development guidelines
├── README.md                      # This file
└── LICENSE                        # MIT License
```

---

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 23+](https://nodejs.org/)
- [pnpm](https://pnpm.io/)
- [Docker](https://www.docker.com/)
- [Terraform](https://www.terraform.io/) (for deployment)

### Quick Start

```bash
# Clone the repository
git clone https://github.com/your-org/lemon-todo.git
cd lemon-todo

# Run with Aspire (starts all services)
dotnet run --project src/LemonDo.AppHost

# Or run individually:
# Backend
dotnet run --project src/LemonDo.Api

# Frontend
cd src/client && pnpm install && pnpm dev
```

### Running Tests

```bash
# All backend tests
dotnet test

# Frontend tests
cd src/client && pnpm test

# E2E tests
cd tests/LemonDo.E2E.Tests && dotnet test

# With coverage
dotnet test --collect:"XPlat Code Coverage"
cd src/client && pnpm test:coverage
```

---

## API Documentation

When the API is running, visit:
- **Scalar UI**: `http://localhost:5000/scalar/v1`
- **OpenAPI JSON**: `http://localhost:5000/openapi/v1.json`
- **Health Check**: `http://localhost:5000/health`

---

## Architecture

See [docs/DOMAIN.md](./docs/DOMAIN.md) for the complete domain design including:
- Bounded context map
- Entity definitions with invariants
- Value objects
- Domain events
- Use cases (commands and queries)
- API endpoint design

See [GUIDELINES.md](./GUIDELINES.md) for:
- TDD methodology
- Code quality standards
- Git workflow
- Security and accessibility guidelines

---

## Trade-offs and Assumptions

### Assumptions

- **Single developer, time-boxed**: The project is designed for incremental delivery with meaningful checkpoints, not waterfall completion.
- **SQLite is sufficient for MVP**: Our data model is simple (tasks, boards, users). The repository pattern makes swapping to PostgreSQL a one-file change when scaling requires it.
- **Evaluators have .NET 10 SDK + Node.js 23+**: We target the latest LTS runtime. Aspire handles service orchestration so `dotnet run` starts everything.
- **Browser-first, not native**: We chose PWA over native apps. Service workers provide offline support without app store distribution.

### Key Trade-offs

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Auth timing** | Tasks first (CP1), auth second (CP2) | Auth-gated MVP from day one | Demonstrates architecture faster; adding user-scoping is a one-line repository change |
| **Database** | SQLite | PostgreSQL/SQL Server | Zero-config for evaluators; repository pattern means swap is trivial |
| **HIPAA** | Technical controls ("HIPAA-Ready") | Full certification | Certification requires legal/BAA framework beyond code scope |
| **State management** | Zustand + TanStack Query | Redux, React Context | Smaller bundle, no providers needed, natural server/client split |
| **API docs** | Scalar | Swagger UI | Faster, better search, dark mode, .NET 9+ default |
| **Offline strategy** | Read-only first, CRUD later | Full offline from day one | Incremental complexity; read-only covers 80% of offline scenarios |
| **i18n** | English-first, add languages later | Multi-language from start | i18next is cheap to retrofit; translation files are additive |
| **Bounded contexts** | All 6 designed, 2-4 implemented per checkpoint | Implement all at once | Incremental delivery proves extensibility without over-building |

### Scalability Considerations

**What scales today**:
- Repository pattern decouples data access from domain logic — swap SQLite to PostgreSQL with no domain changes
- TanStack Query handles caching, deduplication, and background refetch — adding pagination is configuration, not rewrite
- Aspire orchestration generates Kubernetes manifests — deployment scales via `aspire do`
- Domain events decouple mutations from side effects — adding audit logging, notifications, or analytics is event subscription, not code modification

**What we'd add for production scale**:
- **Database migration**: PostgreSQL with connection pooling (PgBouncer) behind the repository interface
- **Caching layer**: Redis for session storage and query caching (Aspire has built-in Redis integration)
- **CDN**: Static assets served via Azure CDN or CloudFront
- **Search**: Elasticsearch for full-text task search (replace in-memory filtering)
- **Queue**: Azure Service Bus for async domain event processing (replace in-process event dispatch)
- **Monitoring**: Grafana dashboards consuming OpenTelemetry data, PagerDuty alerting
- **Multi-tenancy**: Organization-scoped data isolation (the Board aggregate root naturally supports this)

### What We Would Implement Next

Beyond CP5, the roadmap is organized into capability tiers:

#### Tier 1: AI & Agent Ecosystem

- **AI Assistant** — Natural language chat interface for managing tasks ("create a high-priority task to review the Q1 report by Friday", "what's overdue this week?", "summarize my board status"). Built as a new bounded context with a clean adapter pattern — swap between Azure OpenAI, Anthropic, or local models without domain changes.
- **MCP Server** — Expose LemonDo as a [Model Context Protocol](https://modelcontextprotocol.io) server. AI agents (Claude, Copilot, custom) can create/complete/move tasks, query board status, and manage projects through standardized tool definitions. Our use case layer maps naturally to MCP tools — each command/query becomes an MCP tool with typed parameters and responses. This is the critical bridge to a future where agents orchestrate work across systems.
- **MCP Client** — LemonDo as an MCP client, connecting to external MCP servers for calendar, email, CRM, and code repository access. The AI assistant can pull context from a user's entire toolchain without custom integrations for each service.
- **Smart Categorization** — Auto-suggest priority, tags, and columns based on task title content and historical patterns.
- **Daily Digest** — AI-generated summary of what was accomplished, what's in progress, and what needs attention. Delivered via email or in-app notification.
- **Natural Language Filters** — "Show me tasks Marcus created last week that are still in progress" → query builder.

#### Tier 2: Third-Party Integrations

- **Calendar Sync** — Two-way sync with Google Calendar and Outlook. Tasks with due dates appear as calendar events; calendar events can spawn tasks. OAuth2 integration via adapter pattern.
- **Messaging Notifications** — Slack, Microsoft Teams, WhatsApp, and SMS reminders for due dates, mentions, and status changes. Each channel is a Notification bounded context adapter — add new channels without changing domain logic.
- **Push Notifications** — Web Push API (building on our PWA service worker) for real-time browser notifications even when the app isn't open.
- **Email-to-Task** — Forward emails to a dedicated address to create tasks. Azure Functions + SendGrid inbound parse.
- **Zapier/Make Webhooks** — Outbound webhooks on domain events, enabling no-code integrations with 5000+ external services.

#### Tier 3: Collaboration & Real-Time

- **Multi-Tenancy** — Organization-scoped data isolation using EF Core global query filters. Our Board aggregate root naturally supports this — add an `OrganizationId` field and the filter does the rest.
- **Teams & Projects** — Group boards under projects, assign team members, track per-team velocity.
- **Real-Time Board Updates** — SignalR (or SSE for simpler clients) for live Kanban board sync across team members. See a task move the moment a colleague drags it.
- **Idempotency & Conflict Resolution** — Idempotency keys on mutations, optimistic concurrency with ETag headers, last-write-wins with conflict notification for simultaneous edits.
- **Message Queue / DLQ** — Azure Service Bus for reliable async event processing. Dead-letter queue for failed event handlers with retry policies and manual inspection.
- **Activity Feed** — Per-task and per-board activity streams showing who did what and when. Built on top of our domain events + audit trail.
- **Comments & Mentions** — Threaded comments on tasks with @mentions that trigger notifications.

#### Tier 4: Advanced Task Modeling

- **Task Dependencies** — "Blocked by" relationships with visual dependency graphs. Automatic status propagation (parent blocked until all blockers resolved).
- **Subtasks & Checklists** — Nested task hierarchies with progress tracking.
- **Recurring Tasks** — Cron-based task generation via Hangfire or Azure Functions. "Every Monday at 9am, create a 'Weekly standup prep' task."
- **Time Tracking** — Start/stop timer on tasks, time estimates vs actuals, timesheet export.
- **Custom Fields** — Extensible task schema per workspace (dropdown, date, number, text fields). JSON column in SQLite/PostgreSQL with indexed extraction.
- **Templates** — Board templates ("Sprint Board", "Personal GTD") and task templates ("Bug Report", "Feature Request") with pre-filled fields.

#### Tier 5: Reporting & Developer Experience

- **Dashboards** — Burndown/burnup charts, velocity tracking, workload heatmaps, time-to-completion trends. Custom dashboard builder with drag-and-drop widgets.
- **Public API + SDK** — Versioned REST API (/api/v1/, /api/v2/) with auto-generated TypeScript and C# SDKs via Scalar's code generation.
- **CLI Tool** — `lemondo tasks list --status=todo --priority=high` for power users and CI/CD integration.
- **GitHub/GitLab Integration** — Link commits and PRs to tasks, auto-transition task status on merge.
- **Browser Extension** — Quick-capture from any webpage ("clip this page as a task" with URL, title, and screenshot).

#### Tier 6: Platform & Compliance

- **Desktop App** — Tauri (Rust shell wrapping our React frontend) for native desktop experience with system tray, global shortcuts, and offline-first storage.
- **Mobile Native** — React Native sharing domain types from `packages/shared-types` for iOS and Android.
- **SSO** — SAML 2.0 and OIDC for enterprise single sign-on (Okta, Azure AD, Auth0).
- **Full HIPAA Certification** — BAA templates, annual security risk assessment, workforce training program, breach notification procedures, subcontractor BAA verification.
- **GDPR Compliance** — Right to erasure, data portability (full JSON export), consent management, Data Protection Officer workflow.
- **SOC 2 Type II** — Our audit trail and encryption foundations make this achievable. Add formal policies, evidence collection, and annual audit.
- **Data Residency** — Region-specific database deployments for organizations with data sovereignty requirements.

#### Tier 7: Product & Growth

This tier shifts focus from *what we build* to *how we grow*. Features here are about the business, not just the code.

**Monetization & Conversion**:
- **Freemium Model** — Free tier (1 user, 3 boards, 100 tasks), Pro tier (unlimited, integrations, themes), Team tier (collaboration, real-time, admin), Enterprise tier (HIPAA, SSO, data residency, custom fields).
- **Conversion Journey** — Land on page → interactive demo → sign up free (no credit card) → guided onboarding → habit formation → hit a limit → contextual upgrade prompt → Pro. No friction walls — users feel the value before seeing the price.
- **Upgrade Prompts at Natural Friction Points** — "You've used 3 of 3 boards. Upgrade to Pro for unlimited boards." Shown at the moment of need, not in a settings page.
- **Self-Serve Billing Portal** — Stripe integration for subscription management, invoices, plan changes. No sales calls required for Pro/Team tiers.
- **Revenue Metrics** — MRR, ARPU, conversion rate, LTV, churn rate. Dashboard for internal business health monitoring.

**Landing Page & Marketing**:
- **Landing Page** — Hero with clear value proposition, interactive live demo (try before signup), social proof (testimonials, company logos), comparison tables (vs Trello, Asana, Jira), pricing tiers with clear differentiators.
- **Use Case Pages** — SEO-optimized pages: "LemonDo for Healthcare Teams", "LemonDo for Freelancers", "LemonDo for Software Teams." Show the user what the platform can do *for them*, not what they can do *with the platform*.
- **Template Gallery** — Pre-built boards for specific workflows (Sprint Planning, Personal GTD, Client Onboarding, Content Calendar). Users start with a proven structure, not a blank board. Community-contributed templates with ratings.
- **Success Stories** — Case studies with real metrics: "How Team X reduced task completion time by 40%." Video testimonials, before/after screenshots.
- **Content Marketing** — Blog with productivity tips, workflow guides, and thought leadership. SEO funnel: search → blog → signup → onboarding.

**Retention & Customer Success**:
- **Onboarding Optimization** — Track drop-off at every onboarding step. A/B test variations. Measure time-to-first-completed-task (our activation metric).
- **Churn Prevention** — Identify at-risk users via usage signals (7+ days inactive, declining task creation, never completed a task). Automated re-engagement: email sequences, in-app prompts, "We miss you" notifications.
- **Automated Customer Success** — Health score per user/team based on WATC (Weekly Active Task Completers), feature adoption depth, and engagement trends. Surface at-risk accounts to customer success team before they churn.
- **NPS / CSAT Surveys** — In-app micro-surveys at strategic moments (after completing 10th task, after first week, monthly). Track satisfaction trends, route detractors to support.
- **Lifecycle Emails** — Welcome sequence, feature discovery drips ("Did you know you can..."), milestone celebrations ("You completed 100 tasks!"), dormancy re-engagement.

#### Tier 8: UX Excellence

Features focused on *how it feels* to use LemonDo, not just what it does.

- **Command Palette** — Cmd+K to search everything: tasks, boards, actions, settings. Power users never touch the mouse. Fuzzy search with recent items and contextual suggestions.
- **Keyboard Shortcuts** — Full keyboard navigation for every action. `N` for new task, `E` to edit, `D` to mark done, arrow keys to navigate board. Discoverable via `?` overlay.
- **Undo Everywhere** — Every destructive action gets a 5-second undo toast, not a confirmation dialog. Delete a task? Undo. Move a column? Undo. Archive a board? Undo. Faster and less disruptive than "Are you sure?" modals.
- **Batch Operations** — Multi-select tasks (Shift+click, Cmd+click), bulk move between columns, bulk tag, bulk delete, bulk change priority. Essential for power users managing dozens of tasks.
- **Progressive Disclosure** — New users see simplified UI (single board, basic task cards). Power features (filters, custom fields, keyboard shortcuts) reveal progressively as users demonstrate readiness. No overwhelming first-day experience.
- **Micro-Interactions** — Confetti on task completion (our P0 celebration feature), smooth drag physics with haptic-like feedback, satisfying checkbox animation, subtle board column count badges, progress ring on boards showing completion percentage.
- **Smart Defaults** — Pre-fill priority based on keywords ("urgent" → Critical, "meeting" → adds due date), auto-suggest tags from recent usage, remember last-used column for quick-add.
- **Contextual Empty States** — Not just "No tasks yet" but specific, actionable prompts: "Create your first task" with a single CTA, "No results for this filter" with a "Clear filters" link, "This column is empty — drag tasks here or create one" with inline quick-add.
- **Session Analytics** — PostHog for heatmaps, session replays, and funnel analysis. Understand how users *actually* use the product, not how we *assume* they do. Feed insights back into UX improvements.

---

## Contributing

1. Read [GUIDELINES.md](./GUIDELINES.md) first
2. Create a feature branch from `develop`: `git checkout -b feature/your-feature`
3. Follow TDD: write failing test first
4. Use conventional commits
5. Open a PR to `develop`

---

## License

[MIT](./LICENSE)
