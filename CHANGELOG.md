# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-02-15

Checkpoint 2: Authentication & Authorization — secure multi-user support with JWT tokens, cookie-based refresh, and user-scoped data isolation.

### Added

- **User authentication** with ASP.NET Core Identity and JWT tokens
  - Register, Login, Logout, Refresh, and GetCurrentUser endpoints
  - Access tokens (15min) + refresh tokens (7 days) with secure hashing
  - JTI claim for token uniqueness
- **User-scoped data** via `ICurrentUserService` — each user sees only their own tasks and boards
  - Default board auto-created on registration
  - Role seeding (User, Admin) on startup
- **Cookie-based refresh tokens** — HttpOnly, SameSite=Strict, path-scoped cookies for secure token refresh
  - Silent refresh on page load via `AuthHydrationProvider`
  - `RefreshTokenCleanupService` background job (6-hour interval)
- **Security hardening**
  - `SecurityHeadersMiddleware` — X-Content-Type-Options, X-Frame-Options, CSP, Referrer-Policy
  - Account lockout after 5 failed login attempts (15min lockout)
  - PII masking in structured logs
  - CORS configured with `AllowCredentials()` for cookie-based auth
- **Frontend auth system**
  - Login and Register pages with form validation and error handling
  - Zustand auth store (memory-only, no persist) for React 19 compatibility
  - `AuthHydrationProvider` for safe store rehydration
  - Protected routes with automatic redirect to `/login`
  - JWT bearer token injection in API client with 401 handling
  - User menu dropdown with display name and sign out
- **Identity domain model** — User entity, DisplayName and Email value objects with validation
- **388 tests** across all layers
  - 26 new API auth tests (registration, login, token refresh, role assignment, data isolation)
  - 5 new E2E auth tests with cookie-based `loginViaApi` helper
  - Identity domain unit tests (User entity, DisplayName, Email value objects)
  - Token refresh and security header tests

### Changed

- `LemonDoDbContext` now extends `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` instead of `DbContext`
- All task/board endpoints now require authentication (`RequireAuthorization()`)
- Application command/query handlers inject `ICurrentUserService` instead of using `UserId.Default`
- Value objects refactored to `ValueObject<T>` base class with `IReconstructable<TSelf, TValue>` interface
- E2E tests use unique users per test and serial execution for SQLite concurrency safety

## [0.1.0] - 2026-02-15

Checkpoint 1: Core Task Management — a full-stack task management application with DDD architecture, kanban board, and list view in single-user mode.

### Added

- **Domain model** with two bounded contexts (Task and Board) following DDD principles
  - Task aggregate: status lifecycle, priority, tags, due dates, archiving
  - Board aggregate: columns with target status, spatial placement via TaskCard value objects
  - Value objects for all domain concepts (TaskTitle, Tag, Priority, etc.)
  - Domain events on every mutation for extensibility
  - Result pattern for error handling (no exceptions for business logic)
- **Application layer** with 10 command handlers and 4 query handlers (CQRS)
  - Cross-context coordination at the application layer (Task + Board)
  - Domain event dispatch infrastructure in SaveChangesAsync
- **Infrastructure layer** with EF Core + SQLite persistence
  - DateTimeOffset-to-string convention for SQLite ORDER BY support
  - OwnsMany for tags (separate table for queryability)
  - Automatic migrations on startup
- **API layer** with 18 minimal API endpoints (12 task + 6 board)
  - Result-to-HTTP mapping (validation=400, not_found=404, business_rule=422)
  - Error handling middleware with correlation IDs
  - Health checks (/health, /alive) and Scalar API docs (/scalar/v1)
- **React frontend** with Architecture Tiers and Component Taxonomy
  - 12 Shadcn/ui design system components
  - 4 domain atoms: PriorityBadge, TaskStatusChip, DueDateLabel, TagList
  - 3 domain widgets: TaskCard, KanbanColumn, QuickAddForm
  - 2 domain views: KanbanBoard (with drag-and-drop), TaskListView (with time-based grouping)
  - TanStack Query for server state, Zustand for client state (persisted)
  - React Router with kanban (/), list (/list), and 404 routes
- **Drag-and-drop** kanban board using @dnd-kit with cross-column card movement
- **Sparse decimal rank ordering** for card positions (midpoint insertion, O(1) moves)
- **Neighbor-based move API** — frontend sends previousTaskId/nextTaskId, backend computes rank
- **Full-stack observability** with OpenTelemetry traces, metrics, and structured logging
- **242+ tests** across all layers
  - 174 backend tests (unit, property, integration) with MSTest 4 + FsCheck
  - 48 frontend tests (component + property) with Vitest + fast-check
  - 20 E2E tests with Playwright
- **.NET Aspire** orchestration (AppHost, ServiceDefaults) with dynamic port assignment
- **Comprehensive documentation**: PRD, domain model, scenarios, guidelines, journal, tradeoffs, roadmap
- **Lemon.io-inspired design** with custom theme tokens, fonts, and branding
- **Version display** in frontend footer and backend startup logs for traceability
- **Centralized .NET versioning** via `src/Directory.Build.props`

### Changed

- **Bounded context split**: separated Task Management into Task (upstream) and Board (downstream, conformist) contexts for clearer aggregate boundaries
- **Card ordering**: replaced dense integer positions with sparse decimal ranks to eliminate position collisions on reorder
- **Archive decoupled from status**: any task can be archived regardless of lifecycle state (Todo, InProgress, Done)
- **TaskCard made immutable**: board uses remove+add pattern instead of mutating card properties

### Fixed

- Auto-assignment of new tasks to the default board's first column
- Card moves persisting across columns on drag-and-drop
- Position drift caused by dense integer collisions during reorder
- Orphaned cards remaining on board after task deletion
- Column-status invariant enforced with single source of truth (column determines status)
- Drop target accuracy for cross-column card positioning
- Board query side effects removed (board seeded on startup instead)

[unreleased]: https://github.com/btachinardi/lemon-todo/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/btachinardi/lemon-todo/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/btachinardi/lemon-todo/releases/tag/v0.1.0
