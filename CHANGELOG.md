# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.1] - 2026-02-16

Patch release with mobile responsiveness, accessibility, and bug fixes.

### Fixed

- Due date shifting to previous day in western timezones (UTC offset rounding)
- Scalar API docs blocked by restrictive Content-Security-Policy
- Missing accessible `DialogTitle` and `DialogDescription` on TaskDetailSheet
- Missing `sr-only` translation keys for TaskDetailSheet screen reader labels
- Missing visible text labels on icon-only buttons in mobile menus
- Mobile kanban drag-and-drop not working on touch devices
- Mobile navigation missing from landing page header
- Missing responsive mobile menus in Dashboard and Admin layouts
- Dev account switcher overlapping mobile quick-add bar
- Missing column-snap auto-scroll during mobile kanban drag
- Vite proxy config test failing when `launchSettings.json` is missing
- Environment detection for demo accounts replaced with explicit feature flag

## [1.0.0] - 2026-02-16

First stable release. Checkpoint 5: Advanced & Delight — PWA, offline support, onboarding, analytics, notifications, multi-browser E2E, visual regression, and Spanish i18n.

### Added

- **PWA support** — service worker via vite-plugin-pwa, web manifest, install and update prompts
  - Workbox runtime caching: NetworkFirst for `/api/*`, CacheFirst for fonts/images
  - Auth, analytics, and push endpoints excluded from caching
- **Offline read support** — cached task/board data viewable when offline
  - TanStack Query `networkMode: 'offlineFirst'` with extended GC time
  - OfflineBanner shows "Viewing cached data" when offline with cache, "You are offline" without
  - AuthHydrationProvider skips silent refresh when `navigator.onLine` is false
- **Offline mutation queue** — create/complete/move tasks offline, sync when reconnected
  - IndexedDB-backed FIFO queue with `enqueue()`, `drain()`, `clear()`, `getPendingCount()`
  - Automatic drain on `online` event with silent refresh before replay
  - 409 conflict handling: toast notification + discard mutation + cache invalidation
  - SyncIndicator component showing pending count, syncing state, and "All synced" confirmation
- **Onboarding flow** — guided first task creation for new users
  - Server-side `OnboardingCompletedAt` field on User entity with dual-provider migration
  - 3-step tooltip tour: create task → complete it → explore board
  - Auto-advance via MutationObserver watching `data-onboarding` DOM attributes
  - Celebration animation (checkmark burst) on completion
  - Skip button for immediate dismissal
  - Existing users auto-skip (data migration sets timestamp)
- **Notification system** — in-app notifications with Web Push support
  - Notification bounded context: entity, repository, NotificationType enum (DueDateReminder, TaskOverdue, Welcome)
  - DueDateReminderService (BackgroundService) checks tasks due within 24h every 6 hours
  - Welcome notification auto-created on user registration via domain event handler
  - NotificationBell with unread count badge (30s polling)
  - NotificationDropdown with mark-read and mark-all-read
  - Web Push via VAPID: subscription management, push event handler in service worker
  - API endpoints: list, unread-count, mark-read, mark-all-read, push subscribe/unsubscribe, VAPID key
- **Analytics event tracking** — privacy-first analytics with port/adapter pattern
  - Backend: `IAnalyticsService` interface + `ConsoleAnalyticsService` (Serilog, SHA-256 hashed user IDs)
  - Domain event handlers: task created/completed, user registered
  - Frontend: batched tracking with 30s flush and `visibilitychange` flush
  - Device context: viewport, locale, theme, app version
  - `POST /api/analytics/events` endpoint
- **Password strength meter** on registration form with animated progress bar and requirement checklist
  - Evaluates against backend ASP.NET Identity rules: 8+ chars, uppercase, lowercase, digit
  - Bonus criteria: special character, 12+ characters
  - 5 strength levels (Too weak → Very strong) with color-coded feedback
  - Animated checkmark SVG for each passed requirement
  - Submit button disabled until all required criteria pass
- **Show/hide password toggle** on registration form with eye icon
- **Spanish (es) language support** — third locale alongside English and Portuguese (~180 keys)
- **Landing page** with hero, features, security, and open-source sections with scroll animations
- **"How I Built This" story page** at `/story` — interactive engineering narrative
- **Description auto-save** — debounced (1s) save with flush on unmount and save indicator
- **Dev account password auto-fill** — one-click login for seeded test accounts in development
- **Self-reveal for user's own redacted profile** — users can see their own email/name without admin intervention
- **Custom domains** — `api.lemondo.btas.dev` (API) and `lemondo.btas.dev` (frontend) via Terraform + managed certs
- **Mobile responsiveness overhaul**
  - Bottom-anchored task input bar with `env(safe-area-inset-bottom)` for notched devices
  - Native touch scrolling for kanban columns (replaced Radix ScrollArea with `overflow-x-auto`)
  - Responsive card layouts for admin tables on mobile (`UserCard`, `AuditLogCard`)
  - Minimum 44px touch targets on all interactive header elements
  - Toolbar overflow fixes (icon-only buttons on mobile)
- **Lemon.DO branding refresh**
  - Cartoon lemon mascot icon in Dashboard, Admin, and Auth layouts
  - Nunito brand font (Black weight) matching logo typography
  - Updated favicons (ICO, PNG 16/32, Apple Touch Icon, Android Chrome 192/512)
  - Updated web manifest with brand name and theme colors
- **Multi-browser E2E testing** — Chromium + Firefox + WebKit + device emulation (iPhone 14, iPad Mini, Pixel 7)
- **Visual regression baselines** — Playwright `toHaveScreenshot()` for board, list, auth, and landing views in light + dark themes
- **41 new E2E tests**: language (5), onboarding (7), notifications (9), offline (6), PWA (4), visual regression (10)
- **908 tests** total (406 backend + 406 frontend + 96 E2E), up from 668

### Changed

- TanStack Query default `networkMode` set to `offlineFirst` for offline resilience
- OfflineBanner now shows differentiated messages for cached vs no-cache offline states
- All 3 i18n locales updated with ~20 new keys for onboarding, notifications, offline, and PWA

### Fixed

- Task description changes lost when closing detail sheet quickly (debounced auto-save with flush on unmount)
- ESLint errors across 7 CP5 files (react-hooks/refs, setState in effect body, unused variables, react-refresh)
- React 19 `useRef<T>()` requiring explicit `undefined` initial value
- **500 on registration** — AES-256-GCM encryption key was 33 bytes (not 32); regenerated proper 32-byte key
- **SPA route 404 on refresh** — added `staticwebapp.config.json` with `navigationFallback` for Azure Static Web Apps

## [0.4.1] - 2026-02-16

Custom domains for Azure deployment with managed TLS certificates.

### Added

- **Custom domains** for Azure deployment: `api.lemondo.btas.dev` (API) and `lemondo.btas.dev` (frontend)
  - Managed TLS certificates via Azure (Container App + Static Web App)
  - Three-phase DNS setup documented in `infra/README.md`
  - Dual CORS origins for seamless transition (custom domain primary, Azure default secondary)
- **Cross-origin API support** via `VITE_API_BASE_URL` environment variable
  - Frontend `api-client.ts`, `token-refresh.ts`, and `AuthHydrationProvider` updated for absolute URLs
  - CI/CD pipeline injects API URL during frontend build
- Static Web App upgraded to **Standard SKU** (required for custom domains)

## [0.4.0] - 2026-02-16

Checkpoint 4: Production Hardening — observability, security, admin tooling, audit trail, i18n, data encryption, and cloud deployment.

### Added

- **Serilog structured logging** with protected data destructuring policy and correlation ID enrichment
  - Automatic masking of email, password, and display name properties in logs
  - Console + OpenTelemetry sinks for unified observability
- **SystemAdmin role** with `RequireAdminOrAbove` and `RequireSystemAdmin` authorization policies
  - Three-tier role hierarchy: User < Admin < SystemAdmin
- **Audit trail** via new Administration bounded context
  - `AuditEntry` entity tracking security-relevant actions (login, register, task CRUD, role changes, protected data reveals)
  - Domain event handlers auto-create audit entries on key mutations
  - `IRequestContext` captures IP address and user agent per request
  - Paginated, filterable search query (date range, action type, actor, resource)
- **Admin panel** for user management
  - Paginated user list with search and role filter
  - Role assignment and removal (SystemAdmin only)
  - User deactivation and reactivation (SystemAdmin only)
  - `AdminRoute` guard checking Admin/SystemAdmin roles
  - `AdminLayout` with sidebar navigation (Users, Audit Log)
- **AES-256-GCM field encryption** for protected data at rest
  - `EncryptedEmail` and `EncryptedDisplayName` columns on `AspNetUsers`
  - Random 12-byte IV per encryption, tamper detection via authentication tag
  - Identity continues using `NormalizedEmail` for lookups (no breaking changes)
- **Protected data redaction in admin views** — emails and names masked by default
  - `ProtectedDataRedactor` utility for consistent masking (`j***@example.com`)
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
- **Task Sensitive Note** — encrypted free-text field on tasks for storing sensitive information
  - `SensitiveNote` value object (max 10,000 chars) with AES-256-GCM encryption at rest
  - Owner can view their own note via password re-authentication (`POST /api/tasks/:id/view-note`)
  - SystemAdmin break-the-glass reveal with justification + audit trail (`POST /api/admin/tasks/:id/reveal-note`)
  - Task detail sheet with encrypted note section: add/replace textarea, "View Note" dialog with 30s auto-hide
  - Lock icon badge on task cards when a sensitive note exists
  - `SensitiveNoteRevealed` audit action for both owner and admin access
- **Dual database support** — SQLite (development) + SQL Server (production)
  - `DatabaseProvider` configuration key for provider selection
  - Separate EF Core migration assemblies (`Migrations.Sqlite` + `Migrations.SqlServer`)
  - `DesignTimeDbContextFactory` per provider for `dotnet ef` tooling
  - Unconditional `MigrateAsync()` on startup for both providers
- **Azure infrastructure** via Terraform with 3 progressive stages
  - Stage 1 MVP: Container App, ACR, SQL Database, Key Vault, Static Web App, App Insights, Log Analytics (~$18/mo)
  - Stage 2 Resilience: + Front Door with WAF, VNet, private endpoints (~$180/mo)
  - Stage 3 Scale: + Redis Cache, CDN, premium Container Apps with auto-scaling (~$1.7K/mo)
  - 10 reusable Terraform modules (container-app, sql-database, key-vault, monitoring, static-web-app, networking, frontdoor, cdn, redis, app-service)
  - Remote state backend (Azure Storage) with bootstrap script
- **CI/CD pipeline** via GitHub Actions
  - 4-job test matrix: backend (SQLite), backend (SQL Server), frontend (lint + test + build), E2E
  - Docker build and push to Azure Container Registry with commit SHA tags
  - Container App deployment via `az containerapp update`
  - Static Web App deployment via `azure/static-web-apps-deploy`
- **Multi-stage Dockerfile** for API containerization
  - Non-root user, curl healthcheck, migration assemblies included
  - `.dockerignore` for minimal image size
- **Developer CLI** (`./dev`) — unified bash script for all development commands
  - `build`, `test` (backend/frontend/e2e with SQLite/SQL Server variants), `lint`, `start`, `verify`
  - `migrate add/list/remove` (dual-provider), `docker up/down`
  - `infra bootstrap/init/plan/apply/destroy/output/status/unlock`
- **668 tests** total (370 backend + 243 frontend + 55 E2E), up from 478

### Changed

- All frontend components now use `useTranslation()` + `t()` for user-facing strings
- Admin user list shows protected-data-redacted values by default
- API client sends `traceparent` and `X-Correlation-Id` headers on every request
- Renamed "PII" terminology to "Protected Data" across entire codebase (zero functional changes)
- Azure deployment uses Container Apps instead of App Service (VM quota unavailable)

### Fixed

- Admin pagination controls not shown on single-page results
- Missing LanguageSwitcher in AdminLayout header
- Wrong password in note reveal dialog not showing error feedback
- Unsaved description changes lost when closing task detail sheet

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
  - Protected data masking in structured logs
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

[unreleased]: https://github.com/btachinardi/lemon-todo/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/btachinardi/lemon-todo/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/btachinardi/lemon-todo/compare/v0.4.1...v1.0.0
[0.4.1]: https://github.com/btachinardi/lemon-todo/compare/v0.4.0...v0.4.1
[0.4.0]: https://github.com/btachinardi/lemon-todo/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/btachinardi/lemon-todo/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/btachinardi/lemon-todo/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/btachinardi/lemon-todo/releases/tag/v0.1.0
