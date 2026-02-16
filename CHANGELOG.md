# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

Checkpoint 4: Production Hardening — observability, security, admin tooling, audit trail, i18n, and data encryption.

### Added

- **Serilog structured logging** with PII destructuring policy and correlation ID enrichment
  - Automatic masking of email, password, and display name properties in logs
  - Console + OpenTelemetry sinks for unified observability
- **SystemAdmin role** with `RequireAdminOrAbove` and `RequireSystemAdmin` authorization policies
  - Three-tier role hierarchy: User < Admin < SystemAdmin
- **Audit trail** via new Administration bounded context
  - `AuditEntry` entity tracking security-relevant actions (login, register, task CRUD, role changes, PII reveals)
  - Domain event handlers auto-create audit entries on key mutations
  - `IRequestContext` captures IP address and user agent per request
  - Paginated, filterable search query (date range, action type, actor, resource)
- **Admin panel** for user management
  - Paginated user list with search and role filter
  - Role assignment and removal (SystemAdmin only)
  - User deactivation and reactivation (SystemAdmin only)
  - `AdminRoute` guard checking Admin/SystemAdmin roles
  - `AdminLayout` with sidebar navigation (Users, Audit Log)
- **AES-256-GCM field encryption** for PII data at rest
  - `EncryptedEmail` and `EncryptedDisplayName` columns on `AspNetUsers`
  - Random 12-byte IV per encryption, tamper detection via authentication tag
  - Identity continues using `NormalizedEmail` for lookups (no breaking changes)
- **PII redaction in admin views** — emails and names masked by default
  - `PiiRedactor` utility for consistent masking (`j***@example.com`)
  - SystemAdmin "Reveal" action decrypts real values with audit trail entry
  - 30-second auto-hide with amber highlight in UI
- **Admin audit log viewer** with filters (date range, action, resource type) and pagination
  - Color-coded action badges for visual scanning
- **i18n** with i18next supporting English and Portuguese (Brazil)
  - 158 translation keys across all frontend components
  - `LanguageSwitcher` dropdown with browser language auto-detection
  - localStorage persistence for language preference
- **W3C traceparent propagation** from frontend to backend
  - Every API request includes a `traceparent` header for distributed tracing
  - Zero new npm dependencies (uses native `crypto.getRandomValues`)
- **485 tests** total (321 backend + 164 frontend), up from 478
  - 59 new backend tests (encryption, PII redaction, audit, admin endpoints)
  - 3 new frontend tests (traceparent format validation)

### Changed

- All frontend components now use `useTranslation()` + `t()` for user-facing strings
- Admin user list shows PII-redacted values by default
- API client sends `traceparent` and `X-Correlation-Id` headers on every request

## [0.3.0] - 2026-02-15

Checkpoint 3: Rich UX — dark mode, filter bar, task detail sheet, loading skeletons, empty states, error boundaries, and enhanced interactions.

### Added

- **Dark/light theme** with system preference detection and persisted user choice via Zustand store
  - `ThemeToggle` atom with sun/moon icon and keyboard shortcut
  - CSS custom properties for seamless dark mode across all components
- **Filter bar** with search, status, priority, and tag filters
  - Real-time text search across task titles and descriptions
  - Multi-select status and priority filters
  - Tag filter with suggestions from existing tasks
  - Active filter count badge and clear-all button
- **Task detail sheet** (slide-over panel) for inline task editing
  - Edit title, description, priority, due date, and tags without leaving the board
  - Calendar date picker for due dates
  - Tag management with add/remove and case-insensitive duplicate prevention
  - Tag suggestions based on existing tags across all tasks
  - Complete/uncomplete toggle within the sheet
- **Loading skeletons** for board and list views during data fetch
- **Empty state components** — `EmptyBoard` for fresh users, `EmptySearchResults` when filters match nothing
- **Route error boundary** with recovery UI and navigation back to home
- **Toast notifications** via Sonner for task mutations (create, complete, delete, move)
- **Backend filter/search API** — `GET /api/tasks` now supports `search`, `status`, `priority`, and `tag` query params
- **Shadcn/ui primitives**: Sheet, Calendar, Popover, Label components
- **`useMediaQuery` hook** for responsive behavior
- **55 new E2E tests** covering CP3 features (filters, detail sheet, dark mode, empty states)
- **161 frontend tests** total (up from 49 in v0.2.0)

### Changed

- Board view renders `EmptyBoard` component when no tasks exist (instead of empty columns)
- `SortableTaskCard` now has fade-in animation on mount
- `TaskCard` opens detail sheet on click (instead of no-op)
- `DueDateLabel` suppresses overdue styling on completed tasks
- Enhanced `KanbanColumn` with improved drag-and-drop visual feedback

### Fixed

- Horizontal overflow in task detail sheet when due date is set
- Overdue styling incorrectly applied to completed tasks
- Tag creation allowing case-insensitive duplicates

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

[unreleased]: https://github.com/btachinardi/lemon-todo/compare/v0.3.0...HEAD
[0.3.0]: https://github.com/btachinardi/lemon-todo/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/btachinardi/lemon-todo/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/btachinardi/lemon-todo/releases/tag/v0.1.0
