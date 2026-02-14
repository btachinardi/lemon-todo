# LemonDo - Project Task Tracker

> This document tracks the complete planning, design, and development lifecycle of the LemonDo project.
> The project follows a **checkpoint-based delivery model**: each checkpoint produces a complete,
> runnable application. If development stops at any checkpoint, the result is still a presentable,
> functional product that demonstrates our architecture and thought process.

---

## Evaluation Criteria Alignment

| Criteria | Primary Checkpoint | How We Address It |
|---|---|---|
| Backend API design | CP1 | DDD layers, minimal API endpoints, Result pattern |
| Data structure design | CP1 | EF Core + SQLite, proper entity configs, value objects |
| Frontend component design | CP1 | Architecture Tiers + Component Taxonomy, Shadcn/ui |
| F/E ↔ B/E communication | CP1 | TanStack Query, typed API client, optimistic updates |
| Clean code & architecture | CP1 | DDD, SOLID, strict separation of concerns, TDD |
| Trade-offs & assumptions | CP1 | Documented in README development journal + Decision Log |
| Production MVP features | CP2+ | Auth, observability, PII handling, audit trail, i18n |

---

## Phase 0: Project Initialization

| # | Task | Status | Notes |
|---|------|--------|-------|
| 0.1 | Create TASKS.md tracking document | DONE | This file |
| 0.2 | Initialize git repository with gitflow | DONE | main + develop branches |
| 0.3 | Create initial project structure | PENDING | Awaiting bootstrap |

## Phase 1: Product Requirements

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.1 | Write initial PRD (docs/PRD.draft.md) | DONE | 10 FR groups, 10 NFR groups, success metrics, risks |
| 1.2 | Technology research (docs/RESEARCH.md) | DONE | .NET 10, Aspire 13, Vite 7, React 19, all versions locked |
| 1.3 | User scenarios and storyboards (docs/SCENARIOS.md) | DONE | 3 personas, 10 scenarios, analytics events, north star metric |
| 1.4 | Revised PRD (docs/PRD.md) | DONE | Quick-add P0, offline CRUD, PII default-redacted, micro-interactions |

## Phase 2: Domain Design

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.1 | Domain modeling (docs/DOMAIN.md) | DONE | 6 bounded contexts, full entity/VO/event design, API endpoints |
| 2.2 | Development guidelines (GUIDELINES.md) | DONE | TDD, Architecture Tiers, Component Taxonomy, SOLID, gitflow |
| 2.3 | Professional README.md | DONE | Development journal format, full tech stack, project structure |
| 2.4 | LICENSE file | DONE | MIT |

## Phase 3: Codebase Bootstrap

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.1 | Initialize .NET Aspire solution | PENDING | AppHost, ServiceDefaults, API, Domain, Application, Infrastructure |
| 3.2 | Initialize Vite + React frontend | PENDING | TypeScript, Tailwind 4, Shadcn/ui, React Router |
| 3.3 | Configure test infrastructure | PENDING | xUnit, FsCheck, Vitest, fast-check |
| 3.4 | Wire up Aspire ↔ React (AddJavaScriptApp) | PENDING | Dev proxy, CORS |
| 3.5 | Health check endpoint + Scalar API docs | PENDING | /health, /scalar/v1 |
| 3.6 | Verify full stack starts and serves | PENDING | `dotnet run` → API + React both reachable |

---

## Checkpoint 1: Core Task Management

> **Goal**: A working full-stack todo app with clean architecture and tests.
> **Demonstrates**: API design, data structures, component design, F/E↔B/E communication, clean code.
> **Trade-off**: Single-user mode (no auth). Demonstrates architecture without auth complexity.
> Auth adds in CP2 — the repository pattern means adding user-scoped queries is a one-line change.

| # | Task | Status | Notes |
|---|------|--------|-------|
| | **Backend** | | |
| CP1.1 | TaskItem entity + value objects (TDD) | PENDING | TaskTitle, Priority, TaskStatus, Tag, DueDate |
| CP1.2 | Board + Column entities (TDD) | PENDING | Default board with Todo/InProgress/Done columns |
| CP1.3 | Task use cases (TDD) | PENDING | Create, Update, Complete, Delete, Move, List, GetById |
| CP1.4 | EF Core configuration + SQLite | PENDING | Entity configs, migrations, seed data |
| CP1.5 | Task API endpoints | PENDING | CRUD + move + complete, Scalar docs |
| CP1.6 | API integration tests | PENDING | All endpoints, happy + error paths |
| | **Frontend** | | |
| CP1.7 | Design System setup (Shadcn/ui) | PENDING | Button, Card, Badge, Input, Dialog, Toast, layout primitives |
| CP1.8 | Layouts + Pages | PENDING | DashboardLayout, TaskBoardPage, TaskListPage |
| CP1.9 | Domain Atoms | PENDING | PriorityBadge, TaskStatusChip, DueDateLabel, TagList |
| CP1.10 | Domain Widgets | PENDING | TaskCard, KanbanColumn, QuickAddForm |
| CP1.11 | Domain Views | PENDING | KanbanBoard, TaskListView |
| CP1.12 | State: TanStack Query hooks | PENDING | useTasksQuery, useBoardQuery, mutations |
| CP1.13 | State: Zustand stores | PENDING | useTaskViewStore (kanban/list toggle, filters) |
| CP1.14 | Routing setup (React Router) | PENDING | Board route, list route, 404 |
| CP1.15 | Frontend component tests | PENDING | Vitest + Testing Library |
| | **Deliverable** | | `dotnet run --project src/LemonDo.AppHost` → full working app |

---

## Checkpoint 2: Authentication & Authorization

> **Goal**: Secure the app with user accounts and role-based access.
> **Demonstrates**: Security thinking, production-readiness, proper auth patterns.
> **Trade-off**: Two roles (User, Admin) not three. SystemAdmin deferred to CP4.

| # | Task | Status | Notes |
|---|------|--------|-------|
| | **Backend** | | |
| CP2.1 | User entity + Identity setup (TDD) | PENDING | ASP.NET Core Identity, Email VO, DisplayName VO |
| CP2.2 | Auth endpoints | PENDING | Register, Login, Logout, GetCurrentUser |
| CP2.3 | JWT token generation + refresh | PENDING | Access + refresh tokens |
| CP2.4 | Protect task endpoints (user-scoped) | PENDING | [Authorize], filter tasks by authenticated user |
| CP2.5 | Role seeding (User, Admin) | PENDING | Default roles on startup |
| CP2.6 | Auth integration tests | PENDING | Register/login flow, protected endpoints |
| | **Frontend** | | |
| CP2.7 | Auth pages (Login, Register) | PENDING | AuthLayout + LoginForm + RegisterForm |
| CP2.8 | Auth state management | PENDING | Zustand auth store + TanStack Query for user profile |
| CP2.9 | Route guards + redirects | PENDING | Unauthenticated → /login, post-login redirect back |
| CP2.10 | JWT handling (attach, refresh, expire) | PENDING | API client interceptor |
| CP2.11 | User menu + logout | PENDING | Header component with user info and sign out |
| | **Deliverable** | | Multi-user app with secure authentication |

---

## Checkpoint 3: Rich UX & Polish

> **Goal**: Elevate from functional to delightful. Production-quality UX.
> **Demonstrates**: Frontend depth, UX thinking, attention to detail.
> **Trade-off**: Quick-add prioritized over advanced task editing. Theme before i18n.

| # | Task | Status | Notes |
|---|------|--------|-------|
| CP3.1 | Kanban drag-and-drop | PENDING | Move tasks between columns via drag |
| CP3.2 | Quick-add (title-only creation) | PENDING | One-tap task creation — our P0 feature |
| CP3.3 | Task detail modal/sheet | PENDING | Edit title, description, priority, due date, tags |
| CP3.4 | Filters and search | PENDING | By priority, status, tag, text search |
| CP3.5 | Dark/light theme toggle | PENDING | System-aware + manual toggle, Zustand persisted |
| CP3.6 | Responsive design | PENDING | Mobile-first, sidebar collapse, touch-friendly |
| CP3.7 | Loading states + skeletons | PENDING | Skeleton components for every view |
| CP3.8 | Empty states | PENDING | Friendly empty board, no results, first-task prompt |
| CP3.9 | Toast notifications | PENDING | Success/error feedback on all actions |
| CP3.10 | Error boundaries | PENDING | Graceful error recovery per route |
| | **Deliverable** | | Polished, responsive, delightful task management app |

---

## Checkpoint 4: Production Hardening

> **Goal**: Enterprise-grade observability, security, and compliance readiness.
> **Demonstrates**: Production thinking, scalability awareness, security depth.
> **Trade-off**: "HIPAA-Ready" infrastructure, not certified HIPAA compliance.
> Full certification requires legal/BAA framework beyond code scope.

| # | Task | Status | Notes |
|---|------|--------|-------|
| CP4.1 | Backend OpenTelemetry traces + metrics | PENDING | Aspire Dashboard integration |
| CP4.1b | Frontend OpenTelemetry (browser SDK) | PENDING | OTel Browser SDK → OTLP HTTP → Aspire Dashboard, distributed tracing |
| CP4.2 | Structured logging (Serilog) | PENDING | Correlation IDs, request context, PII-safe |
| CP4.3 | PII redaction in admin views | PENDING | Default-masked, reveal with audit log entry |
| CP4.4 | Audit trail | PENDING | Log all data access and mutations |
| CP4.5 | Admin panel (user management) | PENDING | AdminLayout, user list, role assignment |
| CP4.6 | Admin panel (audit log viewer) | PENDING | Filterable, searchable audit log table |
| CP4.7 | SystemAdmin role | PENDING | Third role with elevated privileges |
| CP4.8 | i18n setup (en + pt-BR) | PENDING | react-i18next, all user-facing strings |
| CP4.9 | Rate limiting on auth endpoints | PENDING | Configurable per-IP limits |
| CP4.10 | Data encryption at rest (PII fields) | PENDING | AES-256 for sensitive fields |
| | **Deliverable** | | Production-hardened app with observability and compliance readiness |

---

## Checkpoint 5: Advanced & Delight

> **Goal**: Above-and-beyond features that showcase full-stack depth.
> **Demonstrates**: Offline-first thinking, user empathy, end-to-end quality.
> **Trade-off**: Onboarding + analytics are lightweight implementations proving the
> architecture supports them, not full-blown product analytics suites.

| # | Task | Status | Notes |
|---|------|--------|-------|
| CP5.1 | PWA configuration | PENDING | Service worker, manifest, install prompt |
| CP5.2 | Offline read support | PENDING | Cached task data viewable offline |
| CP5.3 | Onboarding flow | PENDING | Guided first task creation + celebration |
| CP5.4 | Analytics event tracking | PENDING | Privacy-first, hashed IDs, funnel events |
| CP5.5 | Notification system (in-app) | PENDING | Task reminders, due date alerts |
| CP5.6 | E2E tests (Playwright) | PENDING | Chromium + Firefox + WebKit, device emulation (iPhone, iPad, Pixel) |
| CP5.6b | Visual regression baselines | PENDING | Playwright `toHaveScreenshot()` for all key views |
| CP5.7 | Spanish language support | PENDING | Third language option |
| CP5.8 | Offline mutation queue | PENDING | Create/complete tasks offline, sync when online |
| | **Deliverable** | | Feature-complete platform showcasing full production ambition |

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-13 | .NET 10 LTS + Aspire 13 | Latest LTS with best cloud-native support |
| 2026-02-13 | Vite 7 + React 19 | Latest stable, best DX and performance |
| 2026-02-13 | Scalar over Swagger | Modern API docs UI, .NET 9+ default |
| 2026-02-13 | SQLite for MVP | Simple, zero-config, easy to swap later via repository pattern |
| 2026-02-13 | Terraform over Bicep | Multi-cloud portability, team familiarity |
| 2026-02-13 | Azure Container Apps | Aspire-native deployment target |
| 2026-02-13 | WATC as North Star Metric | Weekly Active Task Completers — measures value delivery |
| 2026-02-13 | Quick-add as P0 feature | Scenarios showed fast capture is the killer feature |
| 2026-02-13 | PII default-redacted | Admin views mask PII by default, reveal is explicit + audited |
| 2026-02-13 | TaskItem (not Task) | Avoids conflict with System.Threading.Tasks.Task |
| 2026-02-13 | Result<T,E> over exceptions | Business logic errors are expected, not exceptional |
| 2026-02-13 | Zustand 5 for client state | No provider needed, tiny bundle, persists to localStorage |
| 2026-02-13 | TanStack Query 5 for server state | Cache, dedup, offline mutations, replaces fetch+useState |
| 2026-02-14 | Architecture Tiers + Component Taxonomy | Two orthogonal systems replacing the conflated L1/L2/L3 model |
| 2026-02-14 | Checkpoint-based delivery | Each checkpoint is a complete, submittable application |
| 2026-02-14 | Tasks before Auth (CP1 single-user) | Demonstrates core architecture first; auth adds in CP2 |
| 2026-02-14 | React Router for routing | Well-established, good TypeScript support, evaluator-friendly |
| 2026-02-14 | HIPAA → "HIPAA-Ready" | Technical controls as P0, certification deferred (requires legal/BAA) |
| 2026-02-14 | Deferred: Analytics, Notification, Onboarding | Bounded contexts kept in design, implemented in CP5 |
| 2026-02-14 | Deferred: i18n beyond English | English-only through CP3, add pt-BR in CP4, es in CP5 |
| 2026-02-14 | Deferred: Offline CRUD | Read-only offline in CP5.2, full mutation queue in CP5.8 |
| 2026-02-14 | CI/CD + Docker + Terraform deferred | Infrastructure as code planned but not blocking checkpoints |
| 2026-02-14 | OTel Browser SDK over Sentry for frontend telemetry | Unified OTel pipeline (frontend + backend), Aspire-native, free; Sentry as future production add-on |
| 2026-02-14 | Playwright 3-engine matrix for cross-browser testing | Chromium + Firefox + WebKit covers ~95% of browsers; BrowserStack for real devices in production |
| 2026-02-14 | Playwright screenshots for visual regression baseline | Built-in `toHaveScreenshot()` in CP5; Percy/Chromatic for production cross-browser visual diffs |

---

## Progress Summary

- **Planning**: DONE (Phase 0 + 1 + 2 complete)
- **Bootstrap**: NOT STARTED (Phase 3)
- **Checkpoint 1**: NOT STARTED (Core Task Management)
- **Checkpoint 2**: NOT STARTED (Auth & Authorization)
- **Checkpoint 3**: NOT STARTED (Rich UX & Polish)
- **Checkpoint 4**: NOT STARTED (Production Hardening)
- **Checkpoint 5**: NOT STARTED (Advanced & Delight)

---

## Commit History

| Hash | Message | Phase |
|------|---------|-------|
| 8e61831 | docs: initial project planning documents | Phase 1 |
| f6b7af2 | docs: domain design, guidelines, readme, license | Phase 2 |
| 7b14651 | docs: add Zustand/TanStack Query and revamp frontend architecture | Phase 2 |
| 27e04b6 | docs(guidelines): fix L1 layer import rules consistency | Phase 2 |
| 36b53fa | docs(guidelines): add Pages/Layouts layer and L2 granularity levels | Phase 2 |
| 3790514 | docs(guidelines): replace L1/L2/L3 with Architecture Tiers and Component Taxonomy | Phase 2 |
| *next* | docs: checkpoint-based delivery plan and evaluation alignment | Phase 2 |
