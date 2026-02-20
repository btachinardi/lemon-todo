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
| 0.3 | Create initial project structure | DONE | .NET Aspire solution + React frontend bootstrapped in Phase 3 |

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
| 3.1 | Initialize .NET Aspire solution | DONE | AppHost, ServiceDefaults, API, Domain, Application, Infrastructure. Uses `.slnx` (new .NET 10 XML format). Aspire 13.1.1 templates via NuGet (workload deprecated). |
| 3.2 | Initialize Vite + React frontend | DONE | Vite 7.3.1, React 19.2, TypeScript 5.9, Tailwind 4.1.18, Shadcn/ui, React Router 7.13, TanStack Query 5.90, Zustand 5.0, i18next |
| 3.3 | Configure test infrastructure | DONE | MSTest 4.0.1 + MTP (switched from xUnit v3 due to .NET 10 incompatibility), FsCheck 3.3.2, Vitest 4.0.18, fast-check 4.5.3. All tests pass. |
| 3.4 | Wire up Aspire ↔ React (AddJavaScriptApp) | DONE | AppHost orchestrates API + client, Vite dev proxy for /api, PORT env var |
| 3.5 | Health check endpoint + Scalar API docs | DONE | /health, /alive (ServiceDefaults), /scalar/v1, /openapi/v1.json |
| 3.6 | Verify full stack builds and tests pass | DONE | 9/9 projects build (0 warnings), 3 backend + 1 frontend smoke tests pass |

---

## Checkpoint 1: Core Task Management

> **Goal**: A working full-stack todo app with clean architecture and tests.
> **Demonstrates**: API design, data structures, component design, F/E↔B/E communication, clean code.
> **Trade-off**: Single-user mode (no auth). Demonstrates architecture without auth complexity.
> Auth adds in CP2 - the repository pattern means adding user-scoped queries is a one-line change.

| # | Task | Status | Notes |
|---|------|--------|-------|
| | **Backend** | | |
| CP1.1 | TaskItem entity + value objects (TDD) | DONE | TaskTitle, Priority, TaskStatus, Tag, DueDate. 80+ unit + property tests. |
| CP1.2 | Board + Column entities (TDD) | DONE | Default board with Todo/InProgress/Done columns. OwnsMany for columns. |
| CP1.3 | Task use cases (TDD) | DONE | 10 commands + 4 queries. NSubstitute mocks. 130 tests total. |
| CP1.4 | EF Core configuration + SQLite | DONE | Entity configs, OwnsMany tags, DateTimeOffset→string convention, seed data. DesignTimeDbContextFactory for migrations. |
| CP1.5 | Task API endpoints | DONE | 12 task + 6 board routes. ResultExtensions for error mapping (400/404/422). ErrorHandlingMiddleware. |
| CP1.6 | API integration tests | DONE | 13 task + 6 board tests. In-memory SQLite via WebApplicationFactory. ClassLevel parallelism. |
| | **Frontend** | | |
| CP1.7 | Design System setup (Shadcn/ui) | DONE | 12 components: button, card, badge, input, textarea, select, dialog, sonner, scroll-area, separator, skeleton, dropdown-menu. ESLint override for ui/ dir. |
| CP1.8 | Layouts + Pages | DONE | DashboardLayout, TaskBoardPage, TaskListPage, NotFoundPage |
| CP1.9 | Domain Atoms | DONE | PriorityBadge, TaskStatusChip, DueDateLabel, TagList |
| CP1.10 | Domain Widgets | DONE | TaskCard, KanbanColumn, QuickAddForm |
| CP1.11 | Domain Views | DONE | KanbanBoard, TaskListView |
| CP1.12 | State: TanStack Query hooks | DONE | useTasksQuery, useBoardQuery, useTaskQuery + 8 mutation hooks |
| CP1.13 | State: Zustand stores | DONE | useTaskViewStore (kanban/list toggle, filters, persisted) |
| CP1.14 | Routing setup (React Router) | DONE | Board route (/), list route (/list), 404 |
| CP1.15 | Frontend component tests | DONE | 49 tests: 4 atom + 3 widget + 2 view test suites. fast-check property tests for PriorityBadge and TagList. |
| | **Deliverable** | | `dotnet run --project src/LemonDo.AppHost` → full working app |

---

## Checkpoint 2: Authentication & Authorization

> **Goal**: Secure the app with user accounts and role-based access.
> **Demonstrates**: Security thinking, production-readiness, proper auth patterns.
> **Trade-off**: Two roles (User, Admin) not three. SystemAdmin deferred to CP4.

| # | Task | Status | Notes |
|---|------|--------|-------|
| | **Backend** | | |
| CP2.1 | User entity + Identity setup (TDD) | DONE | ASP.NET Core Identity, Email VO, DisplayName VO, ApplicationUser, IdentityDbContext |
| CP2.2 | Auth endpoints | DONE | Register, Login, Logout, Refresh, GetCurrentUser (5 endpoints) |
| CP2.3 | JWT token generation + refresh | DONE | Access + refresh tokens, JwtTokenService, jti claim for uniqueness |
| CP2.4 | Protect task endpoints (user-scoped) | DONE | ICurrentUserService, RequireAuthorization(), replaced ~10 UserId.Default |
| CP2.5 | Role seeding (User, Admin) | DONE | Default roles on startup, auto-assign "User" on register |
| CP2.6 | Auth integration tests | DONE | 46 API tests (26 auth + 20 existing adapted), deferred JWT options pattern |
| | **Frontend** | | |
| CP2.7 | Auth pages (Login, Register) | DONE | AuthLayout, LoginForm, RegisterForm, LoginPage, RegisterPage |
| CP2.8 | Auth state management | DONE | Zustand auth store (skipHydration + AuthHydrationProvider), auth mutations |
| CP2.9 | Route guards + redirects | DONE | ProtectedRoute, LoginRoute, RegisterRoute, unauthenticated → /login |
| CP2.10 | JWT handling (attach, refresh, expire) | DONE | Bearer token in api-client.ts, 401 handling clears auth + redirects |
| CP2.11 | User menu + logout | DONE | UserMenu dropdown in DashboardLayout header, sign out |
| | **E2E Tests** | | |
| CP2.E2E | Auth E2E tests + update existing | DONE | 5 new auth tests, 37 existing adapted. 42 total, 100% stable (unique users + serial execution). |
| | **Deliverable** | | Multi-user app with secure authentication |

---

## Checkpoint 3: Rich UX & Polish

> **Goal**: Elevate from functional to delightful. Production-quality UX.
> **Demonstrates**: Frontend depth, UX thinking, attention to detail.
> **Trade-off**: Quick-add prioritized over advanced task editing. Theme before i18n.

| # | Task | Status | Notes |
|---|------|--------|-------|
| CP3.1 | Kanban drag-and-drop | DONE | @dnd-kit integration, cross-column + within-column reorder with sparse rank ordering |
| CP3.2 | Quick-add (title-only creation) | DONE | QuickAddForm wired in both board and list views |
| CP3.3 | Task detail modal/sheet | DONE | TaskDetailSheet (slide-over) + TaskDetailSheetProvider context. Inline edit title, description, due date, priority, tags. 12 tests. |
| CP3.4 | Filters and search | DONE | FilterBar widget + filter-tasks.ts utility (12 tests). Backend query params for status, priority, search, tags, archive. |
| CP3.5 | Dark/light theme toggle | DONE | ThemeProvider + ThemeToggle + Zustand persisted store (8 tests). System-aware default. |
| CP3.6 | Responsive design | DONE | use-media-query hook, snap-scroll mobile columns, adaptive layouts |
| CP3.7 | Loading states + skeletons | DONE | BoardSkeleton + ListSkeleton with tests |
| CP3.8 | Empty states | DONE | EmptyBoard (CTA to create first task) + EmptySearchResults (clear filters). Tests for both. |
| CP3.9 | Toast notifications | DONE | All CRUD mutations + error toasts via sonner |
| CP3.10 | Error boundaries | DONE | RouteErrorBoundary per route with retry/home recovery UI (4 tests) |
| CP3.E2E | E2E tests for CP3 features | DONE | 13 new tests: task detail sheet (5), filter/search (5), theme toggle (3). Fixed 5 pre-existing E2E failures caused by EmptyBoard change. |
| | **Deliverable** | | Polished, responsive, delightful task management app |

---

## Checkpoint 4: Production Hardening

> **Goal**: Enterprise-grade observability, security, and compliance readiness.
> **Demonstrates**: Production thinking, scalability awareness, security depth.
> **Trade-off**: "HIPAA-Ready" infrastructure, not certified HIPAA compliance.
> Full certification requires legal/BAA framework beyond code scope.

| # | Task | Status | Notes |
|---|------|--------|-------|
| CP4.1 | Backend OpenTelemetry traces + metrics | DONE | Aspire Dashboard integration (done in CP1 observability commit) |
| CP4.1b | Frontend OpenTelemetry (traceparent) | DONE | W3C traceparent propagation from frontend to backend, zero new deps |
| CP4.2 | Structured logging (Serilog) | DONE | Serilog with PII masking, correlation ID enrichment, console + OTel sinks |
| CP4.3 | PII redaction in admin views | DONE | Default-masked via PiiRedactor, SystemAdmin reveal with audit trail + 30s auto-hide |
| CP4.4 | Audit trail | DONE | Administration bounded context, AuditEntry entity, domain event handlers, paginated search |
| CP4.5 | Admin panel (user management) | DONE | AdminLayout, paginated user list, role assignment, deactivate/reactivate, AdminRoute guard |
| CP4.6 | Admin panel (audit log viewer) | DONE | Filterable by date/action/resource, paginated table with color-coded action badges |
| CP4.7 | SystemAdmin role | DONE | Third role with RequireAdminOrAbove + RequireSystemAdmin authorization policies |
| CP4.8 | i18n setup (en + pt-BR) | DONE | i18next + i18next-browser-languagedetector, 158 keys in en.json + pt-BR.json, LanguageSwitcher |
| CP4.9 | Rate limiting on auth endpoints | DONE | Configurable per-IP limits (done in CP2 security hardening) |
| CP4.10 | Data encryption at rest (PII fields) | DONE | AES-256-GCM field encryption, EncryptedEmail + EncryptedDisplayName columns |
| CP4.11 | Dual-provider SQL Server support | DONE | DatabaseProvider config, SqlServerTestCleanup, EnsureCreated for SQL Server, 370 tests pass on both |
| CP4.12 | Dual EF Core migration assemblies | DONE | Migrations.Sqlite + Migrations.SqlServer projects, unconditional MigrateAsync for both providers |
| CP4.13 | Terraform Azure infrastructure | DONE | Bootstrap + 3 stages, 10 reusable modules (added container-app), Container Apps replaces App Service (VM quota blocked) |
| CP4.14 | CI/CD pipeline (GitHub Actions) | DONE | 4-job test + 1-job deploy workflow: Docker push to ACR + `az containerapp update`, deploy only on main |
| CP4.15 | Dockerfile + containerization | DONE | Multi-stage build, non-root user, curl healthcheck, migration assemblies, .dockerignore |
| CP4.16 | Developer CLI (`./dev`) | DONE | Unified bash script: build, test, lint, start, migrate, docker, verify, infra (bootstrap/init/plan/apply/destroy/output/status/unlock) |
| CP4.17 | Azure deployment verification | DONE | 15 resources deployed, API healthy, Key Vault secrets configured, GitHub CI/CD secrets + variables set |
| | **Deliverable** | | Production-hardened app with observability, compliance, and cloud deployment |

---

## Checkpoint 5: Advanced & Delight

> **Goal**: Above-and-beyond features that showcase full-stack depth.
> **Demonstrates**: Offline-first thinking, user empathy, end-to-end quality.
> **Trade-off**: Onboarding + analytics are lightweight implementations proving the
> architecture supports them, not full-blown product analytics suites.

| # | Task | Status | Notes |
|---|------|--------|-------|
| CP5.1 | PWA configuration | DONE | Service worker, manifest, install and update prompts |
| CP5.2 | Offline read support | DONE | Workbox runtime caching, offlineFirst query mode, OfflineBanner variants |
| CP5.3 | Onboarding flow | DONE | OnboardingCompletedAt entity, 3-step tooltip tour, celebration animation, skip button |
| CP5.4 | Analytics event tracking | DONE | Backend analytics port/adapter + frontend batched tracking with device context |
| CP5.5 | Notification system (in-app) | DONE | Notification entity, due date reminders, bell + dropdown, Web Push (VAPID) |
| CP5.6 | E2E tests (Playwright) | DONE | Multi-browser (Chromium + Firefox + WebKit), device emulation, 41 new specs |
| CP5.6b | Visual regression baselines | DONE | Playwright `toHaveScreenshot()` for board/list/auth/landing in light + dark |
| CP5.7 | Spanish language support | DONE | Third language option (es.json) |
| CP5.8 | Offline mutation queue | DONE | IndexedDB queue, FIFO drain, 409 conflict toast, SyncIndicator |
| CP5.9 | Mobile responsiveness + branding | DONE | Lemon.DO visual identity, responsive layouts |
| CP5.10 | Landing page | DONE | Hero, features, security, open-source sections with scroll animations |
| CP5.11 | Password strength meter | DONE | Animated strength bar, requirement checklist, show/hide toggle, 24 tests |
| CP5.12 | Description auto-save | DONE | Debounced save with flush on unmount, save indicator |
| CP5.13 | Story page | DONE | "How I Built This" interactive story at /story |
| CP5.14 | Auth UX enhancements | DONE | Dev password auto-fill, self-reveal for user's own redacted profile data |
| CP5.15 | Custom domains | DONE | api.lemondo.btas.dev + lemondo.btas.dev via Terraform + managed certs |
| | **Deliverable** | | Feature-complete platform showcasing full production ambition |

---

## Post-Release Quality

| # | Task | Status | Notes |
|---|------|--------|-------|
| PR.1 | Offline queue startup drain + cache invalidation | DONE | `initOfflineQueue()` drains on startup if online with pending mutations; `offline-queue-drained` event for QueryProvider cache invalidation |
| PR.2 | Admin E2E test coverage | DONE | 20 tests across 5 specs (users, audit, roles, route guards, PII reveal). Shared `admin.helpers.ts` module. |
| PR.3 | Fix AuthHydrationProvider StrictMode race | DONE | `useRef` shared promise prevents double-fire of non-idempotent refresh token rotation |
| PR.4 | Security hardening: domain validation + middleware + auth race fix | DONE | Invisible Unicode rejection, null byte guard, GUID claim validation, correlation ID sanitization, concurrency error handling, atomic refresh rotation |
| PR.5 | Security test infrastructure + parameterized baselines | DONE | EndpointRegistry + DynamicData baselines, shared types, advanced/concurrency/response tests. Rename *HardeningTests → *SecurityTests. |
| PR.6 | Dev CLI test-results infrastructure | DONE | Per-project TRX output, 24h cleanup, `./dev test-results` list/failures/clean |

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
| 2026-02-13 | WATC as North Star Metric | Weekly Active Task Completers - measures value delivery |
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
| 2026-02-14 | MSTest 4 + MTP over xUnit v3 | xUnit v3 templates default to net8.0 (don't auto-detect SDK), active .NET 10 bug (#3413 "catastrophic failure"). MSTest is first-party, auto-targets net10.0, guaranteed compatibility. |
| 2026-02-14 | FsCheck core API (no runner package) | FsCheck 3.3.2 core package works standalone in any test framework via `Prop.ForAll`/`Check.Quick`. No need for framework-specific adapter. |
| 2026-02-14 | .NET 10 uses .slnx format | `dotnet new sln` creates `.slnx` (XML-based) by default, not `.sln`. Lighter, cleaner format. |
| 2026-02-14 | Aspire workload deprecated | Aspire is now distributed as NuGet packages + templates. `dotnet new install Aspire.ProjectTemplates` replaces `dotnet workload install aspire`. |
| 2026-02-14 | `dotnet test --solution` syntax | .NET 10 changed `dotnet test` to require `--solution` flag for solution paths (no positional argument). |
| 2026-02-14 | OwnsMany for Tags (not JSON column) | Tags mapped to separate TaskItemTags table via EF Core OwnsMany for queryability. |
| 2026-02-14 | DateTimeOffset→string for SQLite | SQLite doesn't support DateTimeOffset in ORDER BY. Used ConfigureConventions to convert all DateTimeOffset to string globally. |
| 2026-02-14 | ClassLevel test parallelism for integration tests | MethodLevel caused race conditions on shared in-memory SQLite DB. ClassLevel isolates test classes. |
| 2026-02-14 | DesignTimeDbContextFactory for EF migrations | EF tools can't resolve DbContext without DI. Factory provides standalone context creation. |
| 2026-02-14 | Sonner directly over Shadcn Toaster wrapper | Shadcn Toaster requires next-themes ThemeProvider. Direct sonner import avoids unnecessary provider for CP1. |
| 2026-02-14 | erasableSyntaxOnly in tsconfig | Vite + TypeScript 5.9 enables erasableSyntaxOnly which disallows class parameter properties with `readonly`. Use explicit field declarations instead. |
| 2026-02-14 | Split Task Management into Task + Board bounded contexts | DDD review found tight coupling: Tasks stored ColumnId/Position (board concerns), status changes required Board aggregate. Separate contexts with conformist relationship gives clearer boundaries. |
| 2026-02-14 | BoardTask → Task rename | Tasks exist independently of boards. Use qualified names (`using TaskEntity = ...`) for System.Threading.Tasks.Task collisions. |
| 2026-02-14 | TaskCard value object on Board aggregate | Board owns spatial placement (TaskId + ColumnId + Position). Tasks don't need to know about columns. |
| 2026-02-14 | Application-layer cross-context coordination | CreateTask/MoveCard/Complete/Uncomplete handlers coordinate between Task and Board aggregates. No domain-level coupling. |
| 2026-02-15 | Sparse decimal ranks over dense integer positions | Dense integers caused position collisions on reorder (no reindexing). Sparse ranks (1000, 2000...) with midpoint insertion update only the moved card. Decimal avoids float precision drift. |
| 2026-02-15 | Neighbor-based move API over index-based | Frontend sends previousTaskId/nextTaskId instead of position index. Backend computes rank from neighbors (two O(1) lookups). Intent-based contract survives backend strategy changes. Frontend stays dumb. |
| 2026-02-15 | Board.RemoveCard on Delete only, not Archive | Delete is destructive — remove the card. Archive is reversible — preserve the card's column/rank so unarchive restores placement. Filter archived/deleted cards at the query level (board query handlers cross-reference active task IDs). |
| 2026-02-15 | Column.NextRank per-column monotonic counter | Ranks are column-scoped. Each column tracks its own NextRank (starts 1000, +1000 per placement). Avoids scanning cards for max rank. MoveCard bumps target column's NextRank when computed rank exceeds it. |
| 2026-02-15 | Archive decoupled from task status | Archive is a visibility flag orthogonal to lifecycle. Any task (Todo, InProgress, Done) can be archived. Status changes never affect IsArchived. Only explicit Unarchive() clears it. |
| 2026-02-15 | v0.1.0 release via gitflow | Pre-1.0 SemVer for initial development. Centralized .NET versioning via `src/Directory.Build.props`. Annotated tag for GitHub release recognition. CHANGELOG.md in Keep a Changelog format. |
| 2026-02-15 | `ValueObject<T>` base class + `IReconstructable` interface | Eliminates ~5 lines of boilerplate per VO (11 files). `ValueObject<T>` provides `Value`, equality, `ToString()`. `IReconstructable<TSelf, TValue>` standardizes persistence reconstruction. EF extensions (`IsValueObject()`, `IsNullableValueObject()`) replace verbose `HasConversion` calls. |
| 2026-02-15 | No static interface for `Create()` pattern | `Create()` methods are inherently non-uniform (different params, normalization, validation). `Reconstruct` works because it's always the same shape: trusted value in, VO out, no validation. A shared `ICreatable` would be too generic or too restrictive. |
| 2026-02-15 | CS8927 workaround: delegate capture for static abstract in expression trees | EF Core `HasConversion` uses expression trees, which can't call static abstract interface members. Fix: capture `TVO.Reconstruct` as `Func<>` delegate, then use delegate in lambda. |
| 2026-02-15 | Custom auth endpoints over `MapIdentityApi` | Full control over JWT response shape, refresh token flow, and error handling. `MapIdentityApi` scaffolds endpoints we don't need. |
| 2026-02-15 | Domain User entity separate from ApplicationUser | Domain layer stays Identity-free. `ApplicationUser : IdentityUser<Guid>` lives in Infrastructure. Domain `User` has pure VOs (Email, DisplayName). |
| 2026-02-15 | Deferred JWT bearer options configuration | Eager config read in Program.cs runs before test factory overrides. `AddOptions<JwtBearerOptions>().Configure<IOptions<JwtSettings>>()` defers to runtime, fixing test 401s. |
| 2026-02-15 | Zustand persist `skipHydration` for React 19 | Auto-hydration changes store mid-render, crashing React 19's stricter `useSyncExternalStore`. Manual `await rehydrate()` in `AuthHydrationProvider` avoids the race. |
| 2026-02-15 | `loginViaStorage` for E2E auth | Injects Zustand auth state into localStorage (avoid slow form login per test). Register once, cache token, inject for all UI tests. |
| 2026-02-15 | HttpOnly cookie refresh tokens over localStorage | Access token in memory (Zustand, no persist), refresh token in HttpOnly cookie (`SameSite=Strict`, `Path=/api/auth`). Eliminates XSS risk, no CSRF tokens needed. Silent refresh via `AuthHydrationProvider` on page load. |
| 2026-02-15 | Cookie-based E2E auth over localStorage injection | Playwright E2E uses `context.addCookies()` + silent refresh (not localStorage). Matches production auth flow exactly. Verifies `POST /api/auth/refresh` succeeds before proceeding. |
| 2026-02-15 | Unique user per describe block for E2E isolation | Each `test.describe.serial` creates a fresh user (timestamp + counter email). No cleanup needed — users never see each other's data. Eliminates flaky token rotation conflicts from shared state. |
| 2026-02-15 | Serial execution for E2E UI tests | `test.describe.serial` with shared page/context. Login once in `beforeAll`, tests accumulate state within block. 42 logins → 8 logins, 60-90s → 20s, 100% stability (3/3 green runs). |
| 2026-02-15 | v0.2.0 release via gitflow | CP2 (Auth & Authorization) release. 388 tests (262 backend + 84 frontend + 42 E2E). Cookie-based auth, security hardening, multi-user support. |
| 2026-02-15 | @dnd-kit for drag-and-drop (not react-beautiful-dnd) | react-beautiful-dnd is unmaintained; @dnd-kit is modular, actively maintained, supports touch/keyboard, and has first-class React 19 support |
| 2026-02-15 | Slide-over Sheet for task details (not modal Dialog) | Sheet keeps board/list visible in the background, feels lighter than a modal, supports mobile swipe-to-dismiss |
| 2026-02-15 | Client-side filtering with backend query param support | Filters applied client-side for instant UX; backend params added for future server-side filtering at scale |
| 2026-02-15 | Zustand persisted theme store (separate from auth store) | Theme preference is non-sensitive client state that should survive page refresh; auth store deliberately avoids persistence |
| 2026-02-15 | Per-route error boundaries (not global) | Route-level granularity lets users retry or navigate home without losing state in other routes |
| 2026-02-15 | date-fns over dayjs/moment for date formatting | Tree-shakeable, functional API, no global mutation; only imports what we use |
| 2026-02-15 | react-day-picker for calendar (Shadcn/ui default) | Shadcn Calendar component is built on react-day-picker; using the standard primitive |
| 2026-02-15 | CSS animate-fade-in on kanban cards (NFR-011.1) | New DOM elements (new tasks) get fade-in animation via CSS animation-fill-mode:both. React reconciliation preserves existing elements, so only new cards animate. TaskListView already had animate-fade-in-up. |
| 2026-02-15 | v0.3.0 release via gitflow | CP3 (Rich UX & Polish) release. 478 tests (262 backend + 161 frontend + 55 E2E). Dark mode, filter bar, task detail sheet, loading skeletons, empty states, error boundaries, toasts, micro-animations. |
| 2026-02-16 | Serilog over built-in logging | Structured JSON logging, PII destructuring policy, correlation ID enrichment via LogContext |
| 2026-02-16 | SystemAdmin as third role | Separate from Admin for elevated ops (role assignment, deactivation, PII reveal). Two policies: RequireAdminOrAbove, RequireSystemAdmin |
| 2026-02-16 | Administration bounded context for audit | AuditEntry entity with action enum, domain event handlers create entries on key actions (login, register, task CRUD, role changes) |
| 2026-02-16 | AES-256-GCM for PII encryption at rest | Random 12-byte IV prepended to ciphertext, separate EncryptedEmail/EncryptedDisplayName columns alongside Identity columns (Identity uses NormalizedEmail for lookups) |
| 2026-02-16 | PII redacted by default in admin views | PiiRedactor masks emails/names, SystemAdmin reveal creates PiiRevealed audit entry, 30-second auto-hide in UI |
| 2026-02-16 | i18next over react-intl for i18n | Simpler API, namespace support, browser language detection, localStorage persistence |
| 2026-02-16 | Manual traceparent over OTel Browser SDK | Zero new npm deps, W3C Trace Context format, sufficient for distributed tracing correlation |
| 2026-02-16 | Dual-provider database support (SQLite + SQL Server) | DatabaseProvider config key, conditional UseSqlite/UseSqlServer, per-instance unique test DBs for SQL Server |
| 2026-02-16 | Separate migration assemblies per provider | EF Core requires one ModelSnapshot per DbContext per assembly. SQLite and SQL Server produce different column types. Migrations.Sqlite + Migrations.SqlServer assemblies, each with IDesignTimeDbContextFactory. DatabaseProvider env var needed for SQL Server migration generation (dotnet ef finds both factories via Api's references). |
| 2026-02-16 | `./dev` CLI over Makefile or npm scripts | Bash script is portable (Git Bash on Windows), has colored output, auto-manages env vars for SQL Server, and wraps dual-provider migrations in a single command. Colon-separated subcommands (`test backend:sql`) read naturally without flag parsing. |
| 2026-02-16 | Container Apps over App Service | Azure Free Trial and Pay-As-You-Go both had 0 VM quota for App Service (all tiers, all regions). Container Apps use consumption model — no VM quota needed, built-in auto-scaling, cheaper for MVP. |
| 2026-02-16 | Docker push + `az containerapp update` over ZIP deploy | Container Apps don't support ZIP deploy. CI/CD builds Docker image, pushes to ACR with commit SHA tag, then updates Container App image reference. |
| 2026-02-16 | `./dev infra` CLI commands | Terraform operations need Azure CLI in PATH, `MSYS_NO_PATHCONV=1` for Git Bash, and stage selection. CLI wraps all this, making `./dev infra plan stage1-mvp` as simple as `./dev test`. |
| 2026-02-16 | v0.4.0 release via gitflow | CP4 (Production Hardening) release. 668 tests (370 backend + 243 frontend + 55 E2E). Observability, security, admin, audit, i18n, encryption, Azure deployment. |
| 2026-02-16 | Client-side password strength over backend-only validation | Instant feedback as user types, mirrors ASP.NET Identity rules exactly (8+ chars, uppercase, lowercase, digit). Backend remains the authority; frontend is advisory only. |
| 2026-02-16 | No external password strength library | `evaluatePasswordStrength()` is a pure function (~30 lines) checking 6 regex patterns. Libraries like zxcvbn add 400KB+ for dictionary-based scoring we don't need. |
| 2026-02-16 | Disable submit until requirements met | Prevents frustrating server-side 400 errors. Button enables as soon as all 4 required checks pass. Bonus checks (special char, 12+ length) improve the score but don't block submission. |
| 2026-02-16 | Native overflow over Radix ScrollArea for kanban | Radix ScrollArea intercepts touch events, preventing native swipe between columns. Native `overflow-x-auto` + CSS `snap-x snap-mandatory` gives smooth one-column-at-a-time swiping. |
| 2026-02-16 | Bottom-anchored task input on mobile | Matches native mobile app conventions (thumb-reachable zone). `env(safe-area-inset-bottom)` handles notched devices. Desktop keeps top placement. |
| 2026-02-16 | Table-to-card layout for mobile admin views | Tables with 5+ columns are unusable on 375px viewports. `hidden sm:block` / `sm:hidden` pattern swaps between table (desktop) and cards (mobile) at the `sm` breakpoint. |
| 2026-02-16 | Nunito for brand typography | Identified from logo letterforms (rounded terminals, thick strokes). Google Fonts CDN at weights 700/800/900. CSS `--font-brand` variable as single source of truth. |
| 2026-02-16 | vite-plugin-pwa for service worker | Bundles Workbox, generates manifest, handles SW registration. Checked Vite 7 compatibility before install. |
| 2026-02-16 | Workbox NetworkFirst for API, CacheFirst for assets | API data needs freshness; fonts/images are immutable. Auth/analytics/push endpoints excluded from caching. |
| 2026-02-16 | Server-side onboarding state (OnboardingCompletedAt) | Survives device switches. Existing users get timestamp set in data migration to skip tour. |
| 2026-02-16 | 3-step tooltip tour with auto-advance | MutationObserver watches for `data-onboarding` DOM attributes. Steps advance automatically when user creates/completes tasks. |
| 2026-02-16 | Notification bounded context (not embedded in Tasks) | Notifications have their own lifecycle, persistence, and delivery. Keeps Task context focused on task lifecycle. |
| 2026-02-16 | Web Push via VAPID (not FCM) | No Google dependency. Standard Push API works across all modern browsers. Graceful degradation to in-app only. |
| 2026-02-16 | DueDateReminderService as BackgroundService | Checks tasks due within 24h every 6 hours. Short-lived DbContext scopes to avoid SQLite locking. |
| 2026-02-16 | IndexedDB mutation queue over localStorage | localStorage has 5MB limit and is synchronous. IndexedDB supports structured data, cursors, and larger storage. |
| 2026-02-16 | Last-write-wins for offline conflicts (409 → toast + discard) | Simple conflict resolution. 409 means server state diverged; toast informs user, mutation is discarded, cache invalidated. |
| 2026-02-16 | Debounced description auto-save (1s) with flush on unmount | Prevents data loss on quick exits. useRef tracks draft, cleanup flushes pending save. |
| 2026-02-16 | Multi-browser E2E locally, Chromium-only in CI | Firefox + WebKit add ~2x CI time for minimal additional coverage. Local multi-browser catches rendering differences. |
| 2026-02-16 | Visual regression with toHaveScreenshot | Built-in Playwright, no external service. Light + dark theme baselines at 1280x720 with reducedMotion. |
| 2026-02-16 | Release v1.0.0 (skip v0.5.0) | All 5 checkpoints complete. CP5 was planned as v0.5.0 but the app is feature-complete with 908 tests, cloud deployment, PWA, i18n, and admin tooling — warrants 1.0.0 stable designation. |
| 2026-02-16 | v1.0.1 patch release | 12 bug fixes: mobile responsiveness (touch drag, nav menus, column-snap scroll), accessibility (DialogTitle, sr-only keys, visible button labels), due date timezone fix, CSP for Scalar docs, feature flag for demo accounts. |
| 2026-02-17 | Startup drain for offline queue | `initOfflineQueue()` only listened for `online` event transitions. App reopening online with queued IndexedDB mutations would never drain. Fixed: await `refreshCount()` then drain if `navigator.onLine && pendingCount > 0`. |
| 2026-02-17 | Cache invalidation after drain via CustomEvent | `drain()` replayed mutations via raw `fetch()` but never invalidated TanStack Query caches — UI showed stale data until manual reload. Fixed: drain dispatches `offline-queue-drained` event, QueryProvider listens and calls `queryClient.invalidateQueries()`. |
| 2026-02-17 | QueryProvider tests | QueryProvider had zero tests. Added 3 tests for drain-complete cache invalidation, unrelated event immunity, and cleanup on unmount. |
| 2026-02-17 | useRef shared promise for AuthHydrationProvider | React StrictMode double-fires `useEffect`. `AbortController` discards the `Set-Cookie` from mount #1, causing mount #2 to send a revoked token. `useRef<Promise>` shares the refresh promise between mounts — fetch fires once, both cycles use the same result. |
| 2026-02-17 | Shared admin E2E helpers over inline auth code | 20 admin tests need identical login/seed patterns. `admin.helpers.ts` centralizes credentials, API calls, and browser helpers. Reduced ~70 lines per spec. |
| 2026-02-17 | Subagent parallelization for E2E test writing | Wrote first spec manually to establish patterns, then delegated 4 remaining specs to 3 parallel subagents. Shared helper module ensured consistency across all specs. |
| 2026-02-17 | Build-time OpenAPI spec generation over runtime export | `Microsoft.Extensions.ApiDescription.Server` generates spec during `dotnet build` via `GetDocument.Insider`. Restructured `Program.cs`: service registration unconditional (DI needed for parameter inference), only runtime behavior guarded behind `!isBuildTimeDocGen`. |
| 2026-02-17 | Document transformer for enum enrichment | Priority, TaskStatus, NotificationType use `.ToString()` in mappers → OpenAPI sees `string`. Transformer walks `components.schemas` and adds `enum` arrays. `AuditAction` auto-detected via `[JsonStringEnumConverter]`. |
| 2026-02-17 | Selective schema re-export over wholesale replacement | Generated types have `number \| string` (int32 quirk) and `optional` vs `required+nullable` differences. Derive enums from schema (high drift risk), keep interfaces hand-written (low risk, incompatible types). |
| 2026-02-17 | `satisfies` compile-time guards for enum const objects | `as const satisfies { [K in SchemaType]: K }` produces compile error if backend adds value not in const. Cleaner than unused type aliases — no warnings, no runtime code. |
| 2026-02-17 | Enum translation coverage guard test | Import `openapi.json` directly in Vitest, extract enum arrays, assert every value has matching i18n key in all 3 locales. Catches missing translations when backend adds new enum values. |
| 2026-02-17 | `.Produces<T>()` metadata on all endpoints | Minimal API `Results.Ok(dto)` doesn't carry type info. Without `.Produces<T>()`, OpenAPI spec only has request schemas, no response schemas. Added to all 7 endpoint files. |
| 2026-02-17 | v1.0.2 patch release | Consolidates OpenAPI type generation, protected data refactoring, offline queue fixes, admin E2E coverage, theme polish, and CI/CD improvements for release branches. |
| 2026-02-17 | CI/CD for release branches | Added `release/*` to workflow triggers (push + PR). Tests run but deploy is skipped (deploy only on `main` push). Ensures verification gate passes in CI before merging to main. |
| 2026-02-17 | `useVisualViewport` hook for keyboard-aware overlays | Mobile virtual keyboards resize the visual viewport but not the layout viewport. `useVisualViewport` tracks `window.visualViewport` offset/height changes and applies CSS transform to Dialog/Sheet overlays so they stay visible above the keyboard. |
| 2026-02-17 | v1.0.4 patch release | Mobile UX polish: keyboard-aware dialogs/sheets, scrollable evaluator modal, drag-scroll direction lock fix, auth loading screen, font size hierarchy bump, i18n copy tightening. |
| 2026-02-20 | Atomic SQL for refresh token rotation | Two-step read-then-revoke had race condition under concurrent requests. Single `UPDATE...WHERE RevokedAt IS NULL` ensures only one request wins. |
| 2026-02-20 | Parameterized security test baselines | Per-endpoint security tests had massive duplication (auth bypass, method enforcement, etc.). Extracted `EndpointRegistry` + `DynamicData` so adding a new endpoint auto-generates baseline coverage. |
| 2026-02-20 | Admin self-action guards | SystemAdmin self-deactivation or self-role-removal could leave system with no active administrator. Guards added at application layer. |
| 2026-02-20 | Correlation ID sanitization | Unsanitized X-Correlation-Id headers enabled log injection, XSS reflection, and log bloat. Now truncated to 128 chars, stripped to alphanumeric+hyphen+underscore. |
| 2026-02-20 | v1.0.8 patch release | Security hardening: domain validation, middleware hardening, atomic refresh rotation, admin self-action guards, parameterized security test infrastructure, dev CLI test-results. |

---

## Progress Summary

- **Planning**: DONE (Phase 0 + 1 + 2 complete)
- **Bootstrap**: DONE (Phase 3 - solution, frontend, tests, Aspire integration)
- **Checkpoint 1**: DONE — Released as **v0.1.0** (Core Task Management - 242+ tests, 0 warnings)
  - Domain Redesign: Bounded context split (Task + Board) complete
  - Bug Fix: Sparse rank ordering replaces dense integer positions
  - Domain Fix: Archive decoupled from status (any task can be archived)
  - Release: v0.1.0 tagged on main via gitflow
- **CP2 Prep**: `ValueObject<T>` base class + `IReconstructable` + EF extensions (boilerplate reduction)
- **Checkpoint 2**: DONE (Auth & Authorization - 388 tests total: 262 backend + 84 frontend + 42 E2E)
  - Backend: ASP.NET Core Identity + JWT, 5 auth endpoints, ICurrentUserService, role seeding
  - Frontend: Auth store (memory-only), login/register pages, route guards, user menu
  - E2E: 42 Playwright tests, 100% stable via unique users + serial execution
  - Security: HttpOnly cookie refresh, CORS, SecurityHeadersMiddleware, rate limiting, PII masking
  - Key lessons: (1) localStorage is not secure for tokens, (2) flaky E2E = test architecture problem
- **Checkpoint 3**: DONE (Rich UX & Polish - 262 backend + 161 frontend + 55 E2E = 478 tests)
  - Drag-and-drop: @dnd-kit with cross-column moves and within-column reorder
  - Task detail sheet: slide-over with inline editing (title, description, due date, priority, tags)
  - Filters & search: FilterBar + backend query params + client-side filter utility
  - Theme: dark/light with ThemeProvider, ThemeToggle, persisted Zustand store
  - Responsive: use-media-query hook, snap-scroll mobile, adaptive layouts
  - Loading: BoardSkeleton + ListSkeleton
  - Empty states: EmptyBoard + EmptySearchResults with CTA
  - Toasts: success/error feedback on all mutations
  - Error boundaries: RouteErrorBoundary per route with recovery UI
  - Micro-animations: fade-in for kanban cards, draw-check + bounce for completion checkbox
  - New Shadcn/ui primitives: Sheet, Calendar, Popover, Label
  - E2E: 13 new tests (detail sheet, filters, theme toggle) + 5 existing tests fixed for CP3 changes
- **Checkpoint 4**: DONE (Production Hardening - 370 backend + 243 frontend + 55 E2E = 668 tests)
  - Observability: Serilog structured logging, PII masking, W3C traceparent propagation
  - Security: AES-256-GCM field encryption for PII, SystemAdmin role with authorization policies
  - Audit: Administration bounded context with AuditEntry entity and domain event handlers
  - Admin panel: User management (list, search, roles, deactivate) + audit log viewer (filters, pagination)
  - PII redaction: Default-masked in admin views, SystemAdmin reveal with audit trail
  - i18n: i18next with en + pt-BR (158 translation keys), LanguageSwitcher component
  - Dual database: SQLite + SQL Server with separate migration assemblies, MigrateAsync for both
  - Infrastructure: Terraform Azure (bootstrap + 3 stages, 10 modules incl. container-app), GitHub Actions CI/CD, Docker
  - Azure deployment: 15 resources live (Container Apps, ACR, SQL, Key Vault, SWA, App Insights, Log Analytics)
  - Developer CLI: `./dev` script with build, test, lint, start, migrate, docker, verify, infra commands
- **Checkpoint 5**: DONE (Advanced & Delight — 406 backend + 406 frontend + 96 E2E = 908 tests)
  - PWA: Service worker via vite-plugin-pwa, install/update prompts, Workbox runtime caching
  - Offline: Read support (NetworkFirst caching, offlineFirst query mode) + mutation queue (IndexedDB, FIFO drain, 409 conflict handling)
  - Onboarding: 3-step tooltip tour with auto-advance, celebration animation, skip button, server-side state
  - Notifications: Notification bounded context, due date reminder BackgroundService, bell + dropdown, Web Push (VAPID)
  - Analytics: Backend port/adapter + frontend batched tracking with device context and SHA-256 user hashing
  - i18n: Spanish (es) third language — all 3 locales at ~180 keys
  - Password strength: Animated meter with requirement checklist, 5 strength levels, 24 tests
  - Landing page: Hero, features, security, open-source sections with scroll animations
  - Story page: "How I Built This" interactive engineering narrative at /story
  - Auth UX: Dev account password auto-fill, self-reveal for user's own redacted profile
  - Description auto-save: Debounced save with flush on unmount, save indicator
  - Custom domains: api.lemondo.btas.dev + lemondo.btas.dev via Terraform + managed certs
  - Mobile: Bottom-anchored input, native touch scrolling, admin card layouts, 44px touch targets
  - Branding: Lemon.DO identity with Nunito font, mascot icon, favicons
  - E2E: 41 new specs (language, onboarding, notifications, offline, PWA, visual regression)
  - Multi-browser: Chromium + Firefox + WebKit + device emulation (iPhone 14, iPad Mini, Pixel 7)
  - Visual regression: Light + dark theme baselines via Playwright toHaveScreenshot()
- **Release v1.0.0**: First stable release — all 5 checkpoints shipped
- **Release v1.0.1**: Patch — 12 bug fixes (mobile responsiveness, accessibility, timezone, CSP, config)
- **Post-release fix**: Offline queue startup drain + cache invalidation (462 frontend tests, +13 new)
- **Post-release quality**: Admin E2E coverage (20 tests across 5 specs) + AuthHydrationProvider StrictMode race fix
- **Post-release DX**: OpenAPI-based TypeScript type generation (471 frontend tests, +9 enum coverage guard tests)
- **Post-release polish**: Theme refinements — light primary switched to purple, brand token (always lime) for CTAs/logo, dark surfaces lightened, list view card surface, DevOps pipeline icons
- **Release v1.0.2**: Patch — OpenAPI type generation, protected data refactoring, offline queue fixes, admin E2E coverage (20 tests), theme polish, CI/CD for release branches
- **Release v1.0.3**: Patch — dev container, `./dev install` for zero-config onboarding, generated dev config files
- **Release v1.0.4**: Patch — mobile UX polish (keyboard-aware dialogs/sheets, scrollable modals, drag-scroll fix, auth loading screen, font size hierarchy, i18n copy tightening)
- **Release v1.0.5**: Patch — mobile UX polish (toast overlay, kanban drag-scroll edge zones, banner overflow, demo account switcher redesign)
- **Release v1.0.6**: Patch — smooth demo account switching (cache reset + transition overlay), kanban trailing scroll padding + custom scrollbar, test count docs update (1,086 total)
- **Release v1.0.7**: Patch — stale data fix on account switch (resetQueries replaces clear), loading screen ripple alignment with perspective ellipses, test count docs update (1,094 total)
- **Post-release security**: Domain input validation (invisible Unicode, null bytes), middleware hardening (GUID claim validation, correlation ID sanitization, concurrency error handling, Cache-Control), atomic refresh token rotation, admin self-action guards
- **Post-release DX**: Parameterized security test baselines (EndpointRegistry + DynamicData), security test infrastructure extraction, dev CLI test-results commands
- **Release v1.0.8**: Patch — security hardening across domain, middleware, auth, and admin layers; parameterized security test infrastructure; dev CLI test-results

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
| 38cd7bb | docs: text formatting and copy editing pass | Phase 2 |
| 3797bdf | chore: add global.json for MTP test runner and update .gitignore | Phase 3 |
| 5d88566 | feat: scaffold .NET Aspire solution with DDD project structure | Phase 3 |
| 56cb48b | feat: scaffold Vite + React frontend with Tailwind and Shadcn/ui | Phase 3 |
| a9ebce9 | test: add MSTest 4 + MTP test infrastructure with smoke tests | Phase 3 |
| 310782b | docs: update documentation for Phase 3 bootstrap completion | Phase 3 |
| 5f3b3c8 | merge: Phase 3 codebase bootstrap | Phase 3 |
| 3058446 | feat(domain): add common base types (Entity, ValueObject, Result, DomainEvent) | CP1 |
| 0ee06a4 | feat(tasks): add TaskItem entity with value objects and domain events | CP1 |
| 26b88bf | feat(tasks): add Board and Column entities with domain events | CP1 |
| 644bbf6 | feat(tasks): add task use cases (commands, queries, handlers) | CP1 |
| 42f629b | feat(infra): add EF Core configuration, SQLite, and initial migration | CP1 |
| 1200e66 | feat(api): add task and board minimal API endpoints | CP1 |
| 29dcd4f | test(api): add integration tests for task and board endpoints | CP1 |
| 87ddca2 | feat(ui): fix Shadcn alias and install design system components | CP1 |
| d9c3f84 | feat(tasks): add domain types, API client, and task components | CP1 |
| 5a556f9 | feat(tasks): add state management, routing, pages, and layouts | CP1 |
| 45644af | test(tasks): add frontend component tests | CP1 |
| b5e2dc8 | docs: update documentation for Checkpoint 1 completion | CP1 |
| 3713234 | fix(tasks): auto-assign new tasks to default board's first column | CP1 |
| cf78627 | refactor(domain): rename TaskItem to BoardTask | CP1 |
| e5903d8 | fix(domain): enforce column-status invariant with single source of truth | CP1 |
| 45386f3 | refactor(domain): split Task and Board into separate bounded contexts | CP1 |
| 1fe92ce | refactor(domain): make TaskCard immutable with remove+add pattern | CP1 |
| bfad540 | fix(cqrs): remove side effects from GetDefaultBoardQuery, seed board on startup | CP1 |
| a36c1d6 | feat(events): add domain event dispatch infrastructure in SaveChangesAsync | CP1 |
| ef42df8 | fix(ui): UX review fixes — nav, semantic colors, a11y, empty/error states | CP1 |
| 818c018 | docs(csharp): add XML documentation to public-facing domain, application, and infrastructure types | CP1 |
| 2ea3bfd | fix(kanban): persist card moves across columns on drag-and-drop | CP1 |
| abee14d | chore(deps): add @dnd-kit drag-and-drop packages | CP1 |
| d5ba47b | feat(ui): add TaskCheckbox atom and SortableTaskCard wrapper | CP1 |
| cffa65c | feat(kanban): integrate drag-and-drop into board columns and task cards | CP1 |
| eb5d758 | style(ui): polish branding, layout, and design system theme | CP1 |
| 3f84f80 | docs(typescript): add JSDoc to frontend types, API clients, hooks, and components | CP1 |
| 44c96b9 | fix(kanban): replace dense integer positions with sparse decimal ranks | CP1 |
| faabd82 | test(e2e): add card ordering and orphaned card E2E tests | CP1 |
| 4b9f7c6 | fix(domain): allow archiving tasks regardless of status | CP1 |
| f58fcc9 | docs: update domain model and journal for rank ordering and archive changes | CP1 |
| 75d140a | refactor(client): eliminate all unsafe type assertions and `any` usage | CP1 |
| abbc71b | docs(typescript): add missing JSDoc to frontend types, routes, and utilities | CP1 |
| 332cd7f | docs(csharp): add comprehensive XML documentation to all public APIs | CP1 |
| 4ed272d | docs(csharp): add XML documentation to Application and Infrastructure layers | CP1 |
| debb5ab | docs(csharp): add XML documentation to Domain layer | CP1 |
| 6e375ad | fix(migration): add data migration for Position-to-Rank card ordering | CP1 |
| b9ae69a | docs(csharp): fix all 146 CS1591 missing XML comment warnings | CP1 |
| f421b5b | fix(kanban): use drop target for cross-column card positioning | CP1 |
| a16880a | refactor(tests): replace manual assertions with MSTest 4 typed equivalents | CP1 |
| 5a6a7a6 | docs(tradeoffs): expand with domain design, bounded context, and ordering decisions | CP1 |
| 0d85a73 | chore: gitignore SQLite databases and untrack dev db files | CP1 |
| 7fab5e8 | feat(observability): add full-stack telemetry, error handling, and monitoring | CP1 |
| 6512e59 | style(theme): overhaul design tokens and fonts to match lemon.io | CP1 |
| de1becc | style(layout): polish header, navigation, and page containers | CP1 |
| 2210e36 | style(kanban): refine columns, cards, and quick-add form | CP1 |
| 54c0262 | feat(list-view): add time-based grouping and completed-task splitting | CP1 |
| 44f56cd | merge: integrate CP1 core task management into develop | Release |
| f28f714 | chore(release): prepare v0.1.0 | Release |
| 0896f8b | release: v0.1.0 — Checkpoint 1 Core Task Management | Release |
| 18456f2 | docs: add release process guide, update README for v0.1.0 | Release |
| 134a2ec | feat(identity): add domain identity entities, infrastructure, and migrations | CP2 |
| 7905d82 | feat(auth): add JWT auth endpoints, ICurrentUserService, protect routes | CP2 |
| f7c5f1e | test(auth): add auth integration tests, update all tests for JWT auth | CP2 |
| b1ff205 | refactor(domain): add ValueObject\<T\> base class, IReconstructable, and EF extensions | CP2 |
| e26dace | feat(auth): add frontend auth system, E2E tests, fix Zustand 5 + React 19 hydration | CP2 |
| 0580aa4 | docs: update all project docs for CP2 authentication completion | CP2 |
| de76a69 | feat(api): switch to HttpOnly cookie refresh tokens, add security headers, CORS, and rate limiting | CP2 Security |
| 913c9c3 | feat(client): switch to memory-only auth with HttpOnly cookie refresh and silent token renewal | CP2 Security |
| d0ba44c | test(auth): add security hardening tests and update existing tests for cookie-based auth | CP2 Security |
| 35ad089 | test(e2e): switch from localStorage injection to cookie-based loginViaApi | CP2 Security |
| ebcf97e | docs: update journal, research, and tradeoffs for CP2 security hardening | CP2 Security |
| 212a6a8 | test(e2e): eliminate flaky tests via unique users and serial execution | CP2 Security |
| e38ffac | merge: back-merge release/0.2.0 into develop | Release |
| 8090552 | feat(api): add filter and search query params to task listing | CP3 |
| 0b32f0d | feat(ui): add Sheet, Calendar, Popover, and Label primitives | CP3 |
| ec52d55 | feat(client): add dark/light theme with persisted preference | CP3 |
| c120bf0 | feat(client): add use-media-query hook and toast helpers | CP3 |
| e679d57 | feat(ui): add RouteErrorBoundary with recovery UI | CP3 |
| 69c18ab | feat(tasks): add loading skeletons and empty state components | CP3 |
| 466262d | feat(tasks): add FilterBar with search, status, priority, and tag filters | CP3 |
| e298324 | feat(tasks): add TaskDetailSheet with inline editing | CP3 |
| c5e8be3 | feat(tasks): enhance task components with DnD, toasts, and responsive UI | CP3 |
| 9d072dd | feat(client): wire CP3 features into app shell | CP3 |
| ad5c52c | merge: feature/cp3-rich-ux into develop — CP3 Rich UX | CP3 |
| c791563 | chore(release): prepare v0.3.0 | Release |
| b71d9fd | merge: back-merge release/0.3.0 into develop | Release |
| 248de48 | feat(logging): add Serilog with PII masking and correlation enrichment | CP4 |
| bd233de | feat(auth): add SystemAdmin role with authorization policies | CP4 |
| 4e74cd5 | feat(audit): add audit trail with domain event handlers and Administration context | CP4 |
| 871b9c7 | feat(admin): add user management queries and admin endpoints | CP4 |
| 3855aa4 | feat(admin-ui): add AdminLayout, UserManagementView, and admin routing | CP4 |
| 3d6d99d | feat(security): add AES-256-GCM field encryption for PII data at rest | CP4 |
| 3fcda6f | feat(pii): add PII redaction and reveal with audit logging | CP4 |
| 1d3bec3 | feat(audit-ui): add audit log viewer with filters and pagination | CP4 |
| 5240f4c | feat(i18n): add i18next with en + pt-BR translations across all components | CP4 |
| 8172d24 | feat(otel): add W3C traceparent propagation from frontend to backend | CP4 |
| d57fc1a | test(db): add dual-provider test infrastructure for SQL Server | CP4 |
| a6575eb | feat(telemetry): enable Azure Monitor OpenTelemetry integration | CP4 |
| d5346fd | feat(health): expose health endpoints in all environments | CP4 |
| 61cec27 | ci(docker): add multi-stage Dockerfile for API | CP4 |
| 364961b | ci(actions): add GitHub Actions CI/CD pipeline | CP4 |
| d3070c7 | ci(terraform): add Azure infrastructure with staged deployment | CP4 |
| 0fbf76f | chore(dx): add developer CLI script | CP4 |
| 5f045bc | docs: update documentation for CP4 infrastructure changes | CP4 |
| 0a9a078 | fix(admin): add missing LanguageSwitcher to AdminLayout header | CP4 |
| 12981dc | fix(admin): show pagination controls on single-page results | CP4 |
| 2b388a9 | fix(infra): complete CP4 infrastructure — Dockerfile, CI/CD, and bug fixes | CP4 |
| 8a168ee | docs(csharp): improve XML documentation for API contracts and middleware | CP4 |
| 9b2ae63 | docs(csharp): add XML documentation to domain, infrastructure, and service defaults | CP4 |
| 65a702b | docs(csharp): add XML documentation to endpoints and domain types | CP4 |
| 823672c | docs: add CP4 commit history to TASKS.md | CP4 |
| 548c97e | feat(client): improve mobile responsiveness and apply Lemon.DO branding | CP5 |
| 19c3d0c | feat(i18n): add Spanish (es) language support | CP5 |
| 88ffe53 | feat(pwa): add service worker, manifest, install and update prompts | CP5 |
| dd16176 | feat(auth): add password strength meter to registration form | CP5 |
| e69dd3c | feat(analytics): add backend analytics port/adapter with domain event handlers | CP5 |
| 2a962c9 | feat(analytics): add frontend event tracking with batching | CP5 |
| 847ab45 | docs: update changelog, journal, and tasks for CP5 mobile and branding changes | CP5 |
| 1758924 | feat(onboarding): add OnboardingCompletedAt to User entity with dual-provider migration | CP5 |
| 4fd7733 | feat(client): add landing page with scroll animations and route restructure | CP5 |
| c8ca5ff | feat(onboarding): add tooltip tour with celebration animation | CP5 |
| 09202c0 | feat(notifications): add Notification entity, repository, endpoints, and due date reminders | CP5 |
| 3d7654a | feat(notifications): add notification bell, dropdown, and push subscription frontend | CP5 |
| 7c4e197 | feat(offline): add offline read support with Workbox caching and offlineFirst query mode | CP5 |
| 83aef47 | fix(tasks): add debounced auto-save for task description with flush on unmount | CP5 |
| 629798c | feat(auth): add dev-only password auto-fill for demo accounts in reveal dialogs | CP5 |
| f20170a | feat(auth): add self-reveal for user's own redacted profile data | CP5 |
| cb89dbe | feat(client): add "How I Built This" story page at /story | CP5 |
| d721908 | feat(offline): add IndexedDB mutation queue with FIFO drain and conflict handling | CP5 |
| 4a46301 | fix(lint): resolve all ESLint errors across CP5 files | CP5 |
| a96fadf | test(e2e): add CP5 E2E specs, multi-browser config, and visual regression | CP5 |
| 94b6463 | docs(roadmap): add frontend bundle optimization to Tier 9 | CP5 |
| 0527416 | fix(client): move OfflineBanner inside QueryProvider to prevent crash | CP5 |
| c217adc | chore: add Git LFS tracking for .ai files and commit logo | CP5 |
| 098c65c | fix(client): drain offline queue on startup and invalidate caches after drain | Post-release |
| aaa65e3 | style(theme): switch light primary to purple and add brand token | Post-release |
| 2dfdb24 | feat(ui): add brand button variant and update GlowButton to use brand | Post-release |
| 80733cb | style(layouts): use primary for logo, brand for nav, GlowButton for CTA | Post-release |
| f4d356f | feat(landing): add lucide icons to DevOps pipeline detail cards | Post-release |
| 6ba03f3 | style(tasks): add semi-transparent card surface to list view rows | Post-release |
| 4cbe54e | fix(ui): center evaluator modal on mobile viewports | v1.0.4 |
| 8024734 | fix(ui): show loading animation instead of black screen during auth hydration | v1.0.4 |
| 58cce0b | fix(ui): make evaluator modal body scrollable instead of entire panel | v1.0.4 |
| bb88652 | fix(ui): make dialogs and sheets keyboard-aware on mobile | v1.0.4 |
| 3c714a0 | fix(ui): fix mobile kanban drag-scroll direction lock and increase snap cooldown | v1.0.4 |
| 48833bb | feat(ui): bump font size hierarchy for improved readability | v1.0.4 |
| 9d432a0 | fix(i18n): tighten methodology page copy across all 3 locales | v1.0.4 |
| 88c38d8 | fix(auth): block deactivated users on token refresh and enforce lockout on password verification | Security |
| 90091dd | feat(api): add ActiveUserMiddleware to reject deactivated users | Security |
| 416c0b1 | fix(api): handle malformed JSON and bad request bodies in error middleware | Security |
| f8ae366 | feat(api): add input validation to paginated and bulk endpoints | Security |
| 564d049 | test(api): add security hardening tests for all endpoint groups | Security |
| 0dc59c3 | refactor(security): enforce resource ownership via owner-scoped repository queries | Security |
| b045b3e | fix(infra): use raw SQL for task search to bypass value-converter cast exception | Security |
| 3cb53c9 | feat(domain): add visible-character and null-byte validation to value objects | v1.0.8 |
| 4b59ae9 | feat(admin): prevent self-deactivation and self-role-removal | v1.0.8 |
| e4677e3 | fix(auth): use atomic SQL for refresh token rotation to prevent race conditions | v1.0.8 |
| 0e98bb6 | fix(api): harden middleware against injection and concurrency errors | v1.0.8 |
| c689173 | refactor(test): extract security test infrastructure and parameterized baselines | v1.0.8 |
| 3a7e6ad | chore(dev): add test-results infrastructure to dev CLI | v1.0.8 |

---

# Upcoming Checkpoints (CP6–CP11)

> v2 evolves LemonDo from a task management app into Bruno's personal development command center.
> Detailed checkpoint plans, task breakdowns, and technology spikes are in [`docs/roadmap/checkpoints.md`](./docs/roadmap/checkpoints.md).

## Planning Phases

| Phase | Focus | Status |
|-------|-------|--------|
| Phase 0 | Workspace setup (worktree, docs) | DONE |
| Phase 1 | Documentation decomposition (59 files) | DONE |
| Phase 2 | v2 requirements expansion (13 tasks + major domain expansions) | DONE |
| Phase 3 | Implementation planning (checkpoints, tasks, spikes, gates) | DONE |

## Technology Spikes

| ID | Spike | Blocks | Status |
|----|-------|--------|--------|
| SK-01 | simple-git worktree operations | CP6 | TODO |
| SK-02 | ngrok REST API + tunnel lifecycle | CP6 | TODO |
| SK-03 | Redis Streams bidirectional (.NET + Node.js) | CP8 | TODO |
| SK-04 | Claude Agent SDK sidecar lifecycle | CP8 | TODO |
| SK-05 | SignalR Hub → SSE bridge for streaming | CP8 | TODO |
| SK-06 | Gmail OAuth2 + Push Notifications | CP10 | TODO |
| SK-07 | Baileys (WhatsApp) stability assessment | CP10 | TODO |
| SK-08 | Discord.Net Gateway WebSocket | CP10 | TODO |

## Implementation Checkpoints

| Checkpoint | Name | Module(s) | Tasks | Tests | Status |
|------------|------|-----------|-------|-------|--------|
| CP6 | Foundation & Projects | Projects | 34 | +180 | TODO |
| CP7 | People & Relationships | People, ProjectTaskBridge | 27 | +130 | TODO |
| CP8 | Agent Core | Agents | 33 | +200 | TODO |
| CP9 | Agent Intelligence | Agents (skills/chains), ProjectAgentBridge, AgentTaskBridge | 43 | +180 | TODO |
| CP10 | Communications | Comms | 31 | +160 | TODO |
| CP11 | Integration & Polish | All modules | 18 | +60 | TODO |
| **Total** | | | **~155** | **+910** | |

> **Critical Path**: CP6 → CP8 → CP9 → CP11
> **Parallel Track**: CP7 → CP10 (can run alongside the critical path)
> **Estimated Effort**: ~53 solo weeks (compresses significantly with parallel agent sessions)

## Decision Log (CP6+)

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-18 | Event-sourced Node.js sidecars with Redis Streams (ADR-006) | Claude Agent SDK is TypeScript-only; .NET API needs bidirectional communication with sidecar |
| 2026-02-18 | Bridge bounded contexts for cross-domain integration | ProjectAgentBridge, AgentTaskBridge, ProjectTaskBridge keep core domains pure |
| 2026-02-18 | Skills system with memory pills | Composable agent configurations that improve over time through consolidation |
| 2026-02-18 | WorkQueue moved from Agents to ProjectAgentBridge | Batch execution is a project orchestration concern, not an agent runtime concern |
| 2026-02-18 | AgentSession simplified: WorkingDirectory + Objective only | Bridge contexts own the project/task correlation, keeping Agents context clean |
| 2026-02-18 | LinkedIn deferred to v3 | No reliable API; scraping is ToS-violating and fragile |
| 2026-02-18 | Baileys for WhatsApp (spike required) | Official Cloud API not suitable for personal inbox; Baileys carries ToS risk |
| 2026-02-18 | Per-session model selection | Users choose between Haiku (cheap/fast) and Opus (expensive/capable) per session |
| 2026-02-18 | Auto-continue with validation criteria | Sessions continue autonomously until tests pass or max continuations reached |
| 2026-02-18 | Two-phase lifecycle commands | Request → Confirm pattern for all destructive session actions (interrupt, pause, cancel) |
