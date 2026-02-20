# Architecture Decisions

> Architectural decision log and trade-off analysis for the LemonDo project.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [adr-001-planning-delivery.md](./adr-001-planning-delivery.md) | Assignment context, assumptions, and planning & delivery trade-offs | Active |
| [adr-002-architecture-infrastructure.md](./adr-002-architecture-infrastructure.md) | Technology choices, bounded context architecture, and scalability | Active |
| [adr-003-domain-design.md](./adr-003-domain-design.md) | Domain design trade-offs and card ordering / API design | Active |
| [adr-004-auth-security.md](./adr-004-auth-security.md) | Authentication, token strategy, and protected data decisions | Active |
| [adr-005-frontend-testing-pwa.md](./adr-005-frontend-testing-pwa.md) | Frontend UX, API type safety, testing strategy, and offline/PWA | Active |
| [adr-006-agent-session-architecture.md](./adr-006-agent-session-architecture.md) | Agent Sessions use event-sourced Node.js sidecars with Redis Streams and ACL | Accepted |
| [adr-007-agent-bidirectional-comms.md](./adr-007-agent-bidirectional-comms.md) | Bidirectional agent session communication with domain-level message queuing | Accepted |
| [trade-offs.md](./trade-offs.md) | Pointer — all content decomposed into individual ADR files above | Active |

---

## Summary

Architectural decisions are tracked in two complementary forms. The decision log below provides a chronological record of every significant choice made during development, capturing the rationale at the moment it was made. The trade-offs document provides a structured comparison of each decision against the alternatives that were considered and forgone.

The project was built as a take-home assignment interpreted through the lens of regulated healthcare, which drove decisions toward HIPAA-ready patterns, offline reliability, Azure deployment, and product analytics. Most early decisions were made on 2026-02-13 and 2026-02-14 during the planning phase, then refined as implementation revealed edge cases throughout the checkpoint-based delivery.

Key architectural themes that appear repeatedly: the DDD bounded context split between Task and Board, the three-form protected data strategy (redacted/hashed/encrypted), the memory-only auth store with HttpOnly cookie refresh, and the sparse decimal rank ordering strategy for drag-and-drop. These are the decisions with the most downstream impact and the most detailed rationale in the trade-offs document.

The trade-off tables are now decomposed into five ADR files organized by domain. ADR-001 covers the assignment framing and delivery strategy that shaped all other decisions. ADR-002 covers infrastructure and bounded context choices. ADR-003 covers domain modeling and the card ordering API design. ADR-004 is the most extensive, covering the full authentication and protected data strategy. ADR-005 covers the frontend UX library choices, type-safe API contract generation, testing strategy, and the offline/PWA mutation queue. ADR-006 is the first v2 architectural decision, documenting why Agent Sessions use event-sourced Node.js sidecars with Redis Streams and an Anti-Corruption Layer rather than direct API calls or embedded runtimes. ADR-007 extends ADR-006 by making that communication bidirectional: inbound steering messages, interruptions, and queued follow-ups flow from the .NET backend through a per-session Redis command stream into the Node.js sidecar. It also promotes the message queue to a first-class domain aggregate (`SessionMessageQueue`), introduces a session pool domain service for concurrent session limits, and establishes MCP custom tools as a domain concept on session templates.

---

## Decision Log

> **Source**: Extracted from TASKS.md (Decision Log section)

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
