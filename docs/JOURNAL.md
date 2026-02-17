# LemonDo - Development Journal

> **Date**: 2026-02-13
> **Status**: Active
> **Purpose**: Captures the complete thought process, from inception to production - every decision, phase, and lesson learned.

---

## The Starting Point

**Date: February 13, 2026**

LemonDo started with an empty folder and a clear vision: build a production-grade task management platform that proves you can have great UX AND compliance. Not one or the other.

The constraints:
- .NET Aspire for cloud-native orchestration
- React + Shadcn/ui for a premium frontend
- Strict TDD methodology
- DDD architecture throughout
- HIPAA-level data protection

## Phase 1: Planning Before Code

Planning is not wasted time. It's compressed debugging. Five foundational documents were created before writing a single line of code:

### 1.1 Product Requirements Document (PRD)

The first document ([PRD.draft.md](./PRD.draft.md)) captured everything known at that stage:
- 10 functional requirement groups (FR-001 through FR-010)
- 10 non-functional requirement groups (NFR-001 through NFR-010)
- Success metrics with concrete targets
- Risk assessment with mitigations
- Clear "out of scope" boundaries to prevent scope creep

**Decision**: Scalar over Swagger for API documentation. Starting with .NET 9, Scalar became the modern default. It loads faster, has better search, and its dark mode matches the premium UI goals.

**Decision**: SQLite for MVP. Some might call this controversial for a "HIPAA-compliant" app. The reasoning: SQLite is more than capable for the MVP scale, the repository pattern makes swapping to PostgreSQL a one-file change, and it eliminates infrastructure complexity during development.

### 1.2 Technology Research

Every technology in the stack was researched ([RESEARCH.md](./RESEARCH.md)), verifying:
- Latest stable versions (not bleeding edge, not outdated)
- Compatibility between all pieces of the stack
- Features relevant to the project's requirements

Key findings:
- **.NET 10** is the current LTS (3-year support). LemonDo uses 10.0.103.
- **Aspire 13** dropped the ".NET" prefix and added `AddJavaScriptApp` which auto-generates Dockerfiles for the React frontend.
- **Vite 7** is the latest major version (7.3.1). Vite 6 is now in maintenance.
- **React 19.2** brought the React Compiler for automatic memoization.
- **Shadcn/ui** added component styles (Vega, Nova, etc.) and Base UI support in February 2026.

### 1.3 User Scenarios

This is where the planning leveled up. Instead of jumping to domain design, detailed storyboards were written ([SCENARIOS.md](./SCENARIOS.md)) from the USER's perspective:

Three personas were created:
- **Sarah** (Freelancer): Needs quick task capture on mobile
- **Marcus** (Team Lead): Needs Kanban with compliance
- **Diana** (System Admin): Needs audit trails and PII management

Then 10 scenarios were walked through step-by-step, documenting:
- What the user sees at each step
- What they expect to happen
- What emotions they should feel
- What analytics events should be tracked

**Insight**: This exercise revealed that quick-add (title-only, one tap) is THE killer feature. The original PRD required title + description for task creation. The scenario analysis showed that Sarah creates tasks in 2-second bursts while walking between meetings. The minimum was changed to title-only.

**Insight**: Offline support isn't a nice-to-have. Sarah's airplane scenario proved that creating and completing tasks offline is essential, not just viewing.

**The North Star Metric**: Weekly Active Task Completers (WATC). A user who completes at least one task per week. This measures actual value delivery, correlates with retention, and is not gameable.

### 1.4 Revised PRD

After scenarios, [PRD.md](./PRD.md) was created - the official requirements document, incorporating everything learned. Key changes:
- Quick-add promoted to P0
- Onboarding celebrations upgraded from P1 to P0
- Offline CRUD (not just viewing) became a requirement
- PII default-redacted in admin views (not opt-in redaction, but opt-in reveal)
- New NFR section for micro-interactions and UX polish

The original PRD was kept intact to show the project's evolution.

### 1.5 Domain Design

With requirements solid, the domain was designed ([DOMAIN.md](./DOMAIN.md)):

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

## Phase 2: Development Guidelines

Before touching code, the rules of engagement were established ([../GUIDELINES.md](../GUIDELINES.md)):

- **Strict TDD**: RED-GREEN-VALIDATE. No production code without a failing test.
- **Frontend Architecture**: Two orthogonal systems - Architecture Tiers (Routing -> Pages & Layouts -> State Management -> Components) for separation of concerns, and Component Taxonomy (Design System -> Domain Atoms -> Domain Widgets -> Domain Views) for composition granularity.
- **Gitflow**: main + develop + feature branches. Conventional commits. Atomic commits.
- **Security**: PII redaction in logs, OWASP Top 10 compliance, rate limiting.
- **Accessibility**: WCAG 2.1 AA minimum, Radix primitives for built-in a11y.

### Interlude: The State Management Gap

During a checkpoint review of GUIDELINES.md, a significant blind spot was identified: **no explicit state management strategy for the frontend**. The original component architecture described what components render, but not how they get their data.

Two critical libraries were added to the stack:

**TanStack Query 5** for server state (data from the API):
- Replaces the `useState` + `useEffect` + `fetch` anti-pattern
- Automatic caching, deduplication, and background refetching
- Offline mutation queue - critical for the PWA scenario (Sarah on a plane)
- Optimistic updates for that instant-feeling UI

**Zustand 5** for client state (UI preferences, form drafts, offline queue):
- No provider wrapper needed (unlike Redux or Context)
- Built-in `persist` middleware for localStorage/IndexedDB
- Tiny (~1KB) - important for a mobile-first PWA

The key rule: **TanStack Query owns all server data, Zustand owns all client state, React Context is only for low-frequency cross-cutting providers.** Components never mix `fetch` calls with rendering.

### Interlude: Untangling "Layers"

A realization emerged: two orthogonal concepts were being conflated under the same "layer" word. What the codebase actually has are two independent organizational systems:

**Architecture Tiers** answer *"what is this code responsible for?"* - separation of concerns:
```
Routing → Pages & Layouts → State Management → Components
```

**Component Taxonomy** answers *"how big and domain-aware is this UI piece?"* - composition granularity:
```
Design System → Domain Atoms → Domain Widgets → Domain Views
```

The old L1/L2/L3 labels tried to do both jobs at once and created confusion. The new model is cleaner: Architecture Tiers flow data top-down (from URL to pixels), while the Component Taxonomy flows bottom-up (small primitives compose into bigger domain-aware pieces). See [../GUIDELINES.md](../GUIDELINES.md) for the full specification with examples and import rules.

### Interlude: Rethinking Delivery Strategy

After running review agents against the documentation, a hard truth surfaced: the plan was to build everything at once. Six bounded contexts, HIPAA compliance, three languages, offline CRUD, analytics - all marked as P0. The reviewers flagged this as over-engineering, and they were right.

But the ambition isn't wrong - the delivery order is. The project restructured into **checkpoint-based delivery**: five incremental checkpoints where each one produces a complete, runnable application. Stopping at any checkpoint still yields something presentable that demonstrates real architecture and thought process.

**The key insight**: Checkpoint 1 addresses *every* core evaluation criteria (API design, data structures, component design, F/E↔B/E communication, clean code). Later checkpoints layer on production features progressively.

| Checkpoint | Focus | Key Trade-off |
|---|---|---|
| **CP1: Core Tasks** | Working full-stack CRUD | Single-user mode (no auth yet) |
| **CP2: Auth** | User accounts, JWT, RBAC | Two roles (User, Admin), not three |
| **CP3: Rich UX** | Drag-drop, theme, quick-add, polish | Theme before i18n |
| **CP4: Production** | Observability, PII, audit, i18n | "HIPAA-Ready" not certified |
| **CP5: Advanced** | PWA, onboarding, analytics, E2E | Lightweight implementations proving architecture |

**Decision**: Tasks before Auth. A bold but deliberate choice. CP1 runs in single-user mode to demonstrate clean architecture end-to-end without auth complexity. The repository pattern means adding user-scoped queries in CP2 is a one-line change - proving the architecture's extensibility.

**Decision**: HIPAA downgraded from P0 to "HIPAA-Ready infrastructure." Full HIPAA compliance requires BAAs, legal review, workforce training, and incident response procedures - that's a business framework, not a codebase feature. The project implements the *technical controls* (encryption, audit trails, PII redaction) and documents what's needed for full certification.

See [../TASKS.md](../TASKS.md) for the complete checkpoint breakdown with every task.

---

## Phase 3: Codebase Bootstrap

**Date: February 14, 2026**

This phase brought the planning to life. The entire .NET Aspire solution, React frontend, test infrastructure were scaffolded and wired together. The build is clean (9/9 projects, 0 warnings) and all smoke tests pass.

### 3.1 .NET Aspire Solution

Several .NET 10 changes surfaced during bootstrapping:

- **`.slnx` format**: `dotnet new sln` now creates `.slnx` (XML-based) by default, not the legacy `.sln`. Lighter and cleaner.
- **Aspire workload deprecated**: Aspire is now distributed as NuGet packages. Templates are installed via `dotnet new install Aspire.ProjectTemplates` (v13.1.1).
- **`dotnet test --solution` syntax**: .NET 10 requires the `--solution` flag for solution paths; positional arguments no longer work.

The solution follows strict DDD layering: Domain ← Application ← Infrastructure ← Api ← AppHost. ServiceDefaults provides health checks (`/health`, `/alive`), OpenTelemetry, and resilience. Scalar serves API docs at `/scalar/v1`.

### 3.2 React Frontend

Vite 7 + React 19 + TypeScript 5.9 with Tailwind CSS 4 (CSS-first, `@tailwindcss/vite` plugin - no PostCSS). Shadcn/ui initialized with path aliases (`@/*` → `./src/*`). All state management libraries installed: TanStack Query 5, Zustand 5, react-i18next.

Frontend folder structure follows the Architecture Tiers + Component Taxonomy from GUIDELINES.md: `app/` (routing, pages, layouts, providers), `domains/` (feature modules), `ui/` (design system), `lib/` (utilities).

### 3.3 The MSTest Decision

**This was the most important technical decision during bootstrap.**

The initial choice was xUnit v3, following community convention. But during setup, the xUnit v3 template defaulted to `net8.0` instead of auto-detecting the .NET 10 SDK. This caused immediate compatibility errors when adding project references.

Rather than force-fixing the target framework, the team stopped and asked: *"If the tooling is fighting back at bootstrap, is this the wrong tool?"*

Research confirmed the instinct:
- **xUnit v3 has an active .NET 10 bug** (GitHub issue #3413) - "catastrophic failure" in CI environments
- **xUnit v3 templates don't auto-detect SDK version** - they hardcode `net8.0`
- **MSTest is first-party** - maintained by Microsoft, ships with every .NET SDK, guaranteed same-day compatibility

The project switched to **MSTest 4.0.1 with Microsoft.Testing.Platform (MTP)**:
- `dotnet new mstest` → auto-targeted `net10.0` with zero friction
- All project references added without conflicts
- MTP is the modern test runner replacing VSTest (configured via `global.json`)
- Requires `<EnableMSTestRunner>true</EnableMSTestRunner>` + `<OutputType>Exe</OutputType>` in each test project

**FsCheck 3.3.2** (property-based testing) works perfectly with MSTest via the core API - `Prop.ForAll` and `Check.Quick` in any `[TestMethod]`. No framework-specific adapter needed.

**Lesson**: First-party tooling matters. When building on a new framework version, choose tools from the same vendor when possible. Community tools may lag behind.

### 3.4 Aspire ↔ React Integration

AppHost orchestrates both the API and React frontend via `AddJavaScriptApp`. Vite's dev server proxies `/api` requests to the API backend. Environment variables (`PORT`, `services__api__https__0`) enable Aspire to control port assignment.

### 3.5 Verification

- **Build**: 9/9 projects compile with 0 warnings, 0 errors
- **Backend tests**: 3/3 pass (Domain, Application, Api smoke tests via MSTest + MTP)
- **Frontend tests**: 1/1 pass (Vitest smoke test)

---

## Checkpoint 1: Core Task Management

**Date: February 14, 2026**

Full-stack task management in single-user mode. 11 commits, 149 backend tests + 49 frontend tests, 0 build warnings.

### 1.1 Domain Layer (Steps 0-2)

Built the complete domain model following DDD principles:

- **Common base types**: `Entity<TId>`, `ValueObject`, `Result<TValue, TError>`, `DomainEvent`, `PagedResult<T>`
- **TaskItem aggregate**: 7 value objects (TaskItemId, TaskTitle, TaskDescription, Tag, Priority, TaskStatus, UserId), 12 domain events, full invariant enforcement
- **Board/Column aggregates**: Board owns Columns via aggregate boundary, default board factory with 3 columns

**Property testing with FsCheck**: Every value object validated with `Prop.ForAll` - any string 1-500 chars creates a valid TaskTitle, any string 1-50 chars lowercase creates a valid Tag, etc.

### 1.2 Application Layer (Step 3)

10 command handlers + 4 query handlers. Clean separation - handlers receive repository interfaces, return `Result<T, DomainError>`. NSubstitute for mocking. All tests verify both happy path and error cases.

### 1.3 Infrastructure Layer (Step 4)

EF Core with SQLite. Key discoveries:

- **SQLite DateTimeOffset limitation**: SQLite can't ORDER BY DateTimeOffset columns. Fixed with `ConfigureConventions` to convert all DateTimeOffset to string globally.
- **DesignTimeDbContextFactory**: EF migration tools need to instantiate DbContext without DI. Created a factory that provides standalone context creation.
- **OwnsMany for Tags**: Tags stored in a separate `TaskItemTags` table (not JSON column) for queryability.

### 1.4 API Layer (Steps 5-6)

12 task endpoints + 6 board endpoints using minimal API pattern. `ResultExtensions` maps domain errors to HTTP status codes (validation→400, not_found→404, business_rule→422). `ErrorHandlingMiddleware` catches unhandled exceptions.

19 integration tests using `WebApplicationFactory` with in-memory SQLite. **ClassLevel parallelism** required - MethodLevel caused race conditions on shared database state.

### 1.5 Frontend (Steps 7-10)

- **Design System**: 12 Shadcn components installed to flat `src/ui/` directory
- **Domain types**: TypeScript interfaces matching backend DTOs exactly (camelCase)
- **API client**: Fetch-based with error handling, type-safe methods for all endpoints
- **Component hierarchy**: Atoms (PriorityBadge, TaskStatusChip, DueDateLabel, TagList) → Widgets (TaskCard, KanbanColumn, QuickAddForm) → Views (KanbanBoard, TaskListView)
- **State management**: TanStack Query hooks for server state, Zustand store for view preferences (persisted)
- **Routing**: React Router with `/` (kanban), `/list`, and 404
- **Tests**: 49 component tests including fast-check property tests

**Key frontend decision**: Used sonner Toaster directly instead of Shadcn wrapper to avoid requiring next-themes ThemeProvider in CP1. Theme support comes in CP3.

**Key frontend lesson**: TypeScript 5.9 with `erasableSyntaxOnly` disallows class parameter properties with `readonly` keyword. Use explicit field declarations instead.

### 1.6 Verification

| Check | Result |
|---|---|
| **Backend Build** | 9/9 projects, 0 warnings, 0 errors |
| **Frontend Build** | 1908 modules, 412 KB JS + 39 KB CSS |
| **Backend Tests** | 149 passed, 0 failed, 0 skipped |
| **Frontend Tests** | 49 passed, 0 failed |
| **Frontend Lint** | Clean, no issues |

---

## Domain Redesign: Column-Status Invariant

**Date: February 14, 2026**

### The Problem

During CP1 validation, tasks never appeared on the kanban board. Root cause: `TaskItem.Create()` didn't assign a `ColumnId`, creating an orphaned task with no board position. A quick fix was applied, but the real problem ran deeper — the domain model had **two independent sources of truth** for task lifecycle (Status enum and Column position) that could desync.

### The Solution: Two-Commit Redesign

#### Commit 1: Rename TaskItem to BoardTask

Mechanical rename across ~73 files. Tasks are always bound to boards — `BoardTask` reflects this. No behavior changes.

Renames: `TaskItem→BoardTask`, `TaskItemId→BoardTaskId`, `TaskItemStatus→BoardTaskStatus`, `ITaskItemRepository→IBoardTaskRepository`, `TaskItemDto→BoardTaskDto`, etc.

#### Commit 2: Column-Status Invariant

The core redesign. Key design decisions:

1. **Column determines status (one source of truth)**. Each Column stores a `TargetStatus: BoardTaskStatus` (Todo, InProgress, Done). When a task is placed in a column via `MoveTo()`, its status is atomically set to the column's target.

2. **ColumnRole rejected as redundant**. Initially planned a separate `ColumnRole` enum, but it was a 1:1 mapping to `BoardTaskStatus`. The column directly stores what status it assigns — no extra indirection.

3. **Archived is NOT a lifecycle status**. Removed `Archived` from the `BoardTaskStatus` enum. Archive is a visibility flag (`IsArchived` bool) orthogonal to board position. A task stays `Done` when archived.

4. **MoveTo() is the single source of truth**. `Complete()` and `Uncomplete()` removed from the entity. The application layer convenience handlers resolve them to `MoveTo()` calls targeting the board's Done/Todo column.

5. **Separate events for separate concerns**. `TaskMovedEvent` tracks column movement. `TaskStatusChangedEvent` tracks lifecycle changes. Both raised independently. `TaskCompletedEvent` and `TaskUncompletedEvent` deleted.

6. **Board invariants enforced**. Board must always have at least one Todo column and one Done column. `GetInitialColumn()` and `GetDoneColumn()` provide entry/exit points.

7. **WipLimit renamed to MaxTasks**. The "WIP" terminology had no place in the domain vocabulary.

### Key Files Changed

| Area | Changes |
|------|---------|
| Domain entities | BoardTask (ColumnId required, MoveTo sets status), Board (TargetStatus on columns, invariants), Column (TargetStatus instead of ColumnRole) |
| Domain events | TaskCreatedEvent (+ ColumnId, Position, InitialStatus), TaskMovedEvent (FromColumnId non-nullable), NEW TaskStatusChangedEvent, DELETED TaskCompletedEvent/TaskUncompletedEvent |
| Application handlers | All command handlers updated to resolve columns from board, derive status from column's TargetStatus |
| DTOs | ColumnDto (+ TargetStatus, MaxTasks), BoardTaskDto (ColumnId required) |
| Frontend types | TaskStatus (removed Archived), Column (+ targetStatus as TaskStatus, maxTasks) |
| EF configuration | Column TargetStatus mapping, BoardTask ColumnId required, fresh migration |
| Tests | 162 backend + 48 frontend = 210 total, all passing |

### Lessons Learned

- **Two sources of truth always desync eventually.** Column + Status looked independent but had implicit coupling. Making the relationship explicit (column determines status) eliminated an entire class of bugs.
- **Avoid unnecessary abstractions.** ColumnRole was a 1:1 mapping to BoardTaskStatus. Direct usage is clearer than an indirection layer.
- **Archive ≠ lifecycle state.** Visibility flags (archive, soft-delete) are orthogonal to entity lifecycle. Mixing them into the status enum creates invalid state combinations.

---

## Bounded Context Split: Task & Board Separation

**Date: February 14, 2026**

### The Problem

A DDD review of the CP1 codebase identified that Task and Board were tightly coupled in a single bounded context. Tasks stored `ColumnId` and `Position` (board spatial concerns), and every status change required loading the Board aggregate. This violated several DDD principles: bounded context identification (FAIL -- everything was lumped into "Task Management"), aggregate design (FAIL -- Task carried responsibilities belonging to Board), and context mapping (FAIL -- no explicit relationship between the two concepts).

### The Solution

Split into two bounded contexts with a clear dependency direction:

- **Task Context** (upstream): `Task` entity owns its own lifecycle -- status, priority, tags, due date, description. It knows nothing about boards or columns.
- **Board Context** (downstream, conformist): `Board` aggregate manages spatial placement via `TaskCard` value objects (`TaskId` + `ColumnId` + `Position`). It imports `TaskId` and `TaskStatus` from the Task context.

Application handlers coordinate cross-context operations. There is no domain-level coupling between the two contexts.

### Key Design Decisions

1. **Board gains TaskCard value object**. Instead of Task storing `ColumnId` and `Position`, the Board aggregate owns a `Cards: List<TaskCard>` collection where each `TaskCard` holds `TaskId + ColumnId + Position`. Spatial placement is a board concern, not a task concern.

2. **Entity renamed from BoardTask to Task**. Tasks exist independently of boards. The `BoardTask` name from the previous redesign implied tasks were owned by boards, which is no longer true. For `System.Threading.Tasks.Task` collisions, qualified names are used (`using TaskEntity = LemonDo.Domain.Tasks.Task`).

3. **Board is conformist to Task context**. The Board context imports `TaskId` and `TaskStatus` directly from the Task context. No anti-corruption layer needed -- the conformist relationship keeps things simple since both contexts share a single codebase.

4. **Application-layer cross-context coordination**. Handlers orchestrate between the two aggregates:
   - `CreateTask` = `Task.Create()` + `board.PlaceTask()`
   - `MoveTask` = `board.MoveCard()` + `task.SetStatus()`
   - `CompleteTask` = resolve Done column + `board.MoveCard()` + `task.SetStatus()`
   - `UncompleteTask` = resolve Todo column + `board.MoveCard()` + `task.SetStatus()`

### The Migration

A two-table approach for the `SplitTaskBoardContexts` migration:

1. Created `TaskCards` table, populated from existing `Tasks.ColumnId` + `Tasks.Position` data
2. Dropped `ColumnId` and `Position` columns from the `Tasks` table
3. Renamed `BoardTaskTags` table to `TaskTags`

### Frontend Impact

The KanbanBoard view was rewritten to use `board.cards` for task-to-column mapping instead of `task.columnId`. The card collection on the board response now drives which tasks appear in which columns. Mutation hooks were updated to invalidate both task and board queries after operations that affect placement.

### Verification

| Check | Result |
|---|---|
| **Backend Tests** | 174 passed, 0 failed |
| **Frontend Tests** | 48 passed, 0 failed |
| **E2E Tests** | 20 passed, 0 failed |
| **Total Tests** | 242 |
| **Build Warnings** | 0 |
| **Files Changed** | ~98 |

### Lessons Learned

1. **Spatial placement is a BOARD concern, not a task concern.** Which column a task sits in and what position it occupies are questions about board layout, not task identity. Tasks should only know about their own lifecycle (status, priority, tags).

2. **Bounded context splits are major refactors but they pay off.** ~98 files changed across all layers, but the result is clearer responsibilities and simpler aggregates. Each context can now evolve independently.

3. **The conformist relationship keeps things simple.** Since both contexts share the same codebase and live in the same process, Board just imports types from Task directly. No anti-corruption layer, no translation, no mapping -- just direct dependency. This is the right trade-off for a monolith.

---

## Bug Fix: Card Ordering System Redesign

**Date: February 15, 2026**

### The Problem

After completing the bounded context split, manual testing revealed two interrelated bugs in the kanban board:

1. **Position drift**: Dragging a task card to reorder within the same column sometimes placed it one position lower than expected. The issue was intermittent and worsened after multiple consecutive moves.

2. **Orphaned cards**: The GET `/api/boards/default` endpoint returned 17 card items while only 5 tasks existed on the board.

### Investigation

The full execution path was traced: frontend drag handler → API → domain `Board.MoveCard()` → EF Core persistence → query/DTO response → frontend sort.

**Root cause of position drift** (`Board.MoveCard()` in `Board.cs:210-232`): The frontend sent the visual array index (0, 1, 2...) as the position. The backend stored it verbatim on the new `TaskCard` without reindexing other cards in the column. After multiple moves, position collisions accumulated — two cards with position=1 produced an unstable sort, making cards appear in the wrong order on the next load.

Example: Column has A(pos=0), B(pos=1), C(pos=2). Drag C between A and B → frontend sends position=1 → DB state becomes A(pos=0), B(pos=1), C(pos=1). On reload, B and C are non-deterministically ordered.

**Root cause of orphaned cards** (`DeleteTaskCommandHandler` in `DeleteTaskCommand.cs:14-30` and `ArchiveTaskCommandHandler` in `ArchiveTaskCommand.cs:12-30`): Neither handler called `Board.RemoveCard()`. When tasks were soft-deleted or archived, their `TaskCard` entries remained on the board. Over time, 12 deleted tasks left orphaned cards, inflating the count from 5 to 17. The frontend filtered them out visually (cards for non-existent tasks got dropped in the render loop), but the stale data was still returned by the API.

### Solutions Considered

#### For the ordering problem

Four strategies were evaluated to replace the broken dense-integer positioning:

**1. Sparse numeric ranks** — Store ranks as 1000, 2000, 3000. Insert between A and B by computing `(rankA + rankB) / 2`. Only the moved row is updated. Rebalance a local window when gaps become too small. Use DECIMAL, not float (precision drift). *Simple, well-understood, good default.*

**2. Fractional indexing / LexoRank-style strings** — Use lexicographically sortable base-36/base-62 strings. Insert between "a0" and "a8" → "a4". Mostly O(1) writes, avoids float precision entirely, better for high-frequency reorders. Occasional compaction needed. *Scales better but adds string complexity.*

**3. Linked list pointers (prev_id, next_id)** — Each card stores references to its neighbors. A move updates old neighbors + new neighbors + moved card (handful of rows, never whole-list). *Reads and pagination are more complex; harder to query efficiently for sorted lists.*

**4. CRDT sequence identifiers** — Logoot/LSEQ/RGA variants for concurrent collaborative editing. *Massive overkill for a single-user todo app.*

**Decision**: Sparse numeric ranks with `decimal`. It's the simplest strategy that eliminates the bug class, it only updates one row per move, and decimal arithmetic avoids float precision drift. Rebalancing is a rare local operation. LexoRank was tempting but adds unnecessary complexity for this scale.

#### For the API contract

The next question was how the frontend communicates the desired position to the backend. Three options:

**Option A: Frontend sends array index** (current approach) — The frontend sends `position: 2` meaning "put this at index 2 in the column." The backend must load all cards in the column, sort them by rank, find the neighbors at that index, and compute the midpoint rank. *Frontend stays simple but backend does an extra read-to-sort on every move.*

**Option B: Frontend sends the rank directly** — The frontend knows the ranks of neighboring cards (from the board DTO) and computes the midpoint itself. The backend just stores it. *Leaks the ranking system into the frontend. If the strategy changes, the frontend must change too.*

**Option C: Frontend sends neighbor card IDs** — The frontend sends `previousTaskId` and `nextTaskId` — the cards directly above and below the drop target. The backend looks up those two cards' ranks (O(1) each) and computes the midpoint. Null values handle edge cases: `previous=null` means top of column; `next=null` means bottom; both null means only card. *Frontend stays dumb, backend avoids read-to-sort, API contract is explicit about intent.*

**Decision**: Option C — neighbor-based API. This is the cleanest separation of concerns:
- The frontend already knows which cards are above and below from its visual state (the `columnItems` array). Extracting neighbor IDs at the drop index is trivial.
- The backend doesn't need to load and sort all column cards — just two O(1) lookups by TaskId.
- The API contract is unambiguous: "place this card between these two cards" has exactly one correct interpretation, unlike "position 2" which is meaningless when positions are corrupted.
- If a later switch from sparse numeric to LexoRank or any other strategy occurs, only the backend rank-computation changes. The API contract and frontend are unaffected.

### The Final Design

**API contract change**:
```
// Before
POST /api/tasks/{id}/move
{ "columnId": "...", "position": 2 }

// After
POST /api/tasks/{id}/move
{ "columnId": "...", "previousTaskId": "..." | null, "nextTaskId": "..." | null }
```

**Domain changes**:
- `TaskCard.Position` (int) → `TaskCard.Rank` (decimal)
- `Column.NextRank` (decimal) — per-column monotonic counter starting at 1000, incremented by 1000 on each placement. Each column independently tracks its highest rank, avoiding cross-column interference and eliminating the need to scan all column cards to find the max rank.
- `Board.PlaceTask()` assigns the target column's `NextRank` to the new card and bumps the counter. New cards always land at a rank higher than any existing card in that column.
- `Board.MoveCard()` takes `previousTaskId` / `nextTaskId`, looks up their ranks, computes midpoint. When placing at the bottom of a column (`nextTaskId=null`), computes `previousRank + 1000` and bumps that column's `NextRank` past the new rank.
- New `Board.RebalanceColumnRanks()` for when gaps become too small (rare)

**Orphan cleanup** (asymmetric by intent):
- `DeleteTaskCommandHandler` calls `Board.RemoveCard()` — deletion is destructive, no undelete flow exists
- `ArchiveTaskCommandHandler` does NOT touch the board card — archive is reversible, and unarchiving should restore the card to its original column and rank without data loss
- Board query handlers (`GetDefaultBoardQueryHandler`, `GetBoardQueryHandler`) inject `ITaskRepository` to fetch active task IDs, then the DTO mapper filters out cards for archived/deleted tasks. This resolves the 17-card symptom at the read layer while preserving archived card placement in the database

**Frontend changes** (minimal):
- `handleDragEnd` extracts `previousTaskId` / `nextTaskId` from the `columnItems` array at the drop index
- `MoveTaskRequest` sends neighbor IDs instead of position integer
- Sort by `rank` (number) instead of `position` — functionally identical, just a field rename

### Key Files Changed

| Area | Files | Change |
|------|-------|--------|
| Domain | `TaskCard.cs`, `Board.cs`, `Column.cs` | Rank replaces Position, per-column NextRank counter, neighbor-based rank computation |
| Application | `MoveTaskCommand.cs` | Accept previousTaskId/nextTaskId, pass to domain |
| Application | `DeleteTaskCommand.cs` | Add Board.RemoveCard() cleanup on delete |
| Application | `GetDefaultBoardQuery.cs`, `GetBoardQuery.cs` | Filter cards for archived/deleted tasks |
| Application | `CreateTaskCommand.cs` | PlaceTask with auto-rank |
| Application | `BoardDtoMapper.cs`, `BoardDto.cs` | Map Rank to DTO |
| Infrastructure | `BoardConfiguration.cs` | Column type decimal for Rank |
| Infrastructure | New migration | Position→Rank schema change |
| API | `TaskEndpoints.cs`, `MoveTaskRequest` | New request shape |
| Frontend | `board.types.ts`, `api.types.ts` | Rank field, neighbor-based move request |
| Frontend | `KanbanBoard.tsx` | Extract neighbors, sort by rank |
| Frontend | `factories.ts` | Update test factories |

### Lessons Learned

1. **Dense integer positions are a trap.** They work fine for append-only lists but break silently under reordering because every move theoretically requires reindexing all subsequent items. In practice, that reindexing gets skipped (as it did here), and position collisions accumulate until the sort becomes non-deterministic.

2. **The API contract should express intent, not implementation.** "Place between these two cards" is intent. "Set position to 2" is an implementation detail that assumes dense integers. Intent-based APIs survive backend strategy changes.

3. **Aggregate cleanup must be coordinated across contexts.** When Task and Board were split into separate bounded contexts, the Delete/Archive handlers only updated the Task aggregate. The Board aggregate's cards were left behind. Cross-context operations (even destructive ones) need explicit coordination at the application layer.

---

## Domain Fix: Archive Decoupled from Status

**Date: February 15, 2026**

### The Problem

During E2E test authoring for card ordering, it was discovered that `Task.Archive()` required the task to be in `Done` status. This forced a `completeTask()` → `archiveTask()` sequence in tests, and more importantly, it was a design flaw: archiving is a visibility/organizational concept, not a lifecycle one.

A user should be able to archive any task regardless of its status — a stale Todo task, an abandoned InProgress task, or a completed Done task. Requiring completion first is an unnecessary constraint that doesn't serve the user's intent.

### The Fix

1. **Removed the `Status == Done` guard from `Task.Archive()`** — any non-deleted task can now be archived.
2. **Removed `IsArchived = false` from `SetStatus()`** — previously, transitioning away from Done reset the archive flag. Since archiving is now status-independent, only the explicit `Unarchive()` method clears it.

### Design Principle Reinforced

Archive is orthogonal to lifecycle. Two independent axes:
- **Status axis**: Todo → InProgress → Done (lifecycle)
- **Visibility axis**: Active ↔ Archived (organizational)

These axes should never be coupled. A task can be archived at any point on the status axis, and status changes should never affect the visibility axis.

### Verification

| Check | Result |
|---|---|
| **Backend Tests** | 193 passed, 0 failed |
| **Frontend Tests** | 53 passed, 0 failed |
| **E2E Tests** | 33 passed, 0 failed |
| **Total** | 279 tests, all green |

---

## Release: v0.1.0

**Date: February 15, 2026**

### Version Strategy

The project adopted **pre-1.0 SemVer** (`0.MINOR.PATCH`) for initial development. v0.1.0 signals "first feature-complete checkpoint" rather than production stability. The `1.0.0` release will come when the app has authentication, polish, and production hardening (likely after CP3 or CP4).

### Release Infrastructure

- **Centralized .NET versioning**: `src/Directory.Build.props` sets `Version`, `AssemblyVersion`, `FileVersion`, and `InformationalVersion` for all 6 source projects. Test projects under `tests/` are intentionally excluded — they're internal tooling, not versioned deliverables.
- **Frontend version**: `src/client/package.json` version bumped from `0.0.0` to `0.1.0`. Version displayed in the footer of the dashboard layout.
- **Backend version logging**: API startup logs emit the assembly informational version for traceability.
- **CHANGELOG.md**: Created in [Keep a Changelog](https://keepachangelog.com/) format with curated, user-facing entries grouped into Added/Changed/Fixed sections.
- **Annotated git tag**: `v0.1.0` with message, tagger, and timestamp. GitHub recognizes annotated tags as releases.

### Gitflow Process

The release followed strict gitflow:

1. `feature/cp1-core-task-management` merged to `develop` (--no-ff)
2. `release/0.1.0` branch created from `develop`
3. Release prep committed on `release/0.1.0` (version bumps, CHANGELOG, docs)
4. Verification gate passed (build, tests, lint)
5. `release/0.1.0` merged to `main` (--no-ff) and tagged `v0.1.0`
6. `release/0.1.0` back-merged to `develop`
7. Stale branches cleaned up (`release/0.1.0`, `feature/cp1-core-task-management`, `feature/phase3-bootstrap`)

### What Ships in v0.1.0

See [CHANGELOG.md](../CHANGELOG.md) for the full list. Highlights:

- Full-stack DDD task management with two bounded contexts
- Kanban board with drag-and-drop (sparse rank ordering)
- List view with time-based grouping
- 18 API endpoints with Result-to-HTTP mapping
- 242+ tests (backend unit/property/integration + frontend component/property + E2E)
- Lemon.io-inspired design with custom theme
- Full-stack observability (OpenTelemetry + correlation IDs)

---

## CP2 Prep: ValueObject\<T\> Base Class Refactor

**Date: February 15, 2026**

### The Problem

Every value object in the codebase repeated the same boilerplate: a `Value` property, `GetEqualityComponents()` yielding that value, and a `ToString()` override. That's ~5 lines of ceremony per VO across 11 files. Worse, every EF Core configuration repeated a verbose `HasConversion(vo => vo.Value, value => SomeVO.SomeMethod(value))` call, and each VO used a *different* reconstruction method (`From()`, `Create().Value`, `new()`) — an inconsistency smell.

### The Solution: Three New Abstractions

**1. `ValueObject<T>` base class** (`Domain/Common/ValueObjectOfT.cs`): Extends the existing `ValueObject` with a generic `Value` property, single-value equality, and `ToString()`. Single-value VOs (all 11 of ours) inherit from this instead of raw `ValueObject`.

**2. `IReconstructable<TSelf, TValue>` interface** (`Domain/Common/IReconstructable.cs`): A static abstract interface that standardizes how VOs are materialized from trusted persistence values. Every VO implements `static TSelf Reconstruct(TValue value) => new(value)`. This is *not* validation — it bypasses `Create()` entirely, because values coming from the database are already trusted.

**3. `ValueObjectPropertyExtensions`** (`Infrastructure/Persistence/Extensions/`): Three extension methods on EF Core's `PropertyBuilder<TVO>`:
- `IsValueObject()` — for `Guid`-backed VOs (just conversion)
- `IsValueObject(maxLength)` — for required `string`-backed VOs (conversion + max length + IsRequired)
- `IsNullableValueObject(maxLength)` — for nullable `string`-backed VOs (null-safe conversion + max length)

### Design Decision: No Static Interface for Create()

A deliberate decision was made **not** to create an `ICreatable<TSelf, TInput>` interface to standardize the `Create()` factory pattern. The reason: `Create()` methods are inherently non-uniform:

- **ID types** (TaskId, BoardId, etc.) have no `Create()` at all — they use `New()` / `From()`
- **String VOs** return `Result<TSelf, DomainError>` but differ in parameters and normalization logic (Tag lowercases, Email validates format + lowercases, DisplayName enforces min length, TaskDescription allows null/empty)

A shared interface would either be too generic to enforce anything useful or too restrictive to accommodate the variations. `Reconstruct` works precisely because reconstruction is *always* the same shape: trusted value in, VO out, no validation. `Create` is the opposite — it's where each VO's unique domain rules live.

### Gotcha: CS8927 — Static Abstract Members in Expression Trees

EF Core's `HasConversion` takes expression trees, but C# prohibits calling static abstract interface members inside expression trees (CS8927). The fix: capture `TVO.Reconstruct` as a `Func<>` delegate first, then use the delegate in the lambda. The expression tree sees a captured variable invocation (legal) instead of a static virtual dispatch (illegal).

```csharp
// Won't compile — CS8927
builder.HasConversion(vo => vo.Value, value => TVO.Reconstruct(value));

// Works — delegate capture
Func<Guid, TVO> reconstruct = TVO.Reconstruct;
builder.HasConversion(vo => vo.Value, value => reconstruct(value));
```

### Impact

- **11 value objects** lost ~5 lines of boilerplate each (55 lines removed)
- **2 EF configurations** replaced verbose conversion calls with one-liner extensions
- **0 test changes** — public API (`.Value`, `.Create()`, `.From()`) unchanged
- **246 backend + 80 frontend tests** still pass

---

## Checkpoint 2: Authentication & Authorization

**Date**: 2026-02-15

### What Was Built

Full authentication system across backend, frontend, and E2E tests:

**Backend (Phases 1-5)**:
- Domain Identity entities: `User`, `Email` VO, `DisplayName` VO in `Domain/Identity/`
- Infrastructure: `ApplicationUser : IdentityUser<Guid>`, `IdentityDbContext`, `RefreshToken` entity
- 5 auth endpoints: `POST /api/auth/register|login|refresh|logout`, `GET /api/auth/me`
- `JwtTokenService` for access + refresh token generation with `jti` uniqueness claim
- `ICurrentUserService` abstraction replacing ~10 `UserId.Default` references
- `RequireAuthorization()` on all task/board endpoints
- Role seeding (User, Admin) + auto-assign "User" on registration
- Default board auto-created per user on registration

**Frontend (Phases 6-10)**:
- Auth pages: `LoginPage`, `RegisterPage` with `AuthLayout`
- Zustand auth store (memory-only, no persistence) + `AuthHydrationProvider` (silent refresh via HttpOnly cookie)
- Protected routes: `ProtectedRoute`, `LoginRoute`, `RegisterRoute`
- Bearer token injection from Zustand memory in `api-client.ts` with 401 → token refresh → retry
- `credentials: 'include'` on all fetch calls for cookie transmission
- `UserMenu` dropdown in `DashboardLayout` header

**E2E (Phase 11)**:
- `auth.helpers.ts`: `getAuthToken()`, `loginViaApi(page)` for Playwright (cookie injection + silent refresh)
- `api.helpers.ts`: all API calls include Bearer token
- 5 new auth E2E tests + 37 existing tests adapted

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| Custom auth endpoints (not `MapIdentityApi`) | Full control over JWT response shape, refresh flow |
| Memory-only auth store (no persist) | HttpOnly cookie handles session persistence; Zustand persist removed entirely, eliminating React 19 hydration race |
| `AuthHydrationProvider` wrapping router | Silent refresh via HttpOnly cookie on mount, restores in-memory access token before children render |
| Deferred JWT bearer options | `AddOptions<JwtBearerOptions>().Configure<IOptions<JwtSettings>>()` avoids eager config read that breaks test factory overrides |
| `jti` claim on access tokens | Prevents identical tokens when issued within the same second |
| `loginViaApi` for E2E | Calls login API, injects HttpOnly cookie via Playwright `addCookies`, silent refresh restores session |

### Gotchas & Lessons

1. **Zustand 5 + React 19 infinite loop**: Object-returning selectors like `useStore(s => ({ a: s.a }))` create new references every call → "getSnapshot should be cached" → infinite re-render. Fix: split into separate primitive selectors or use `useShallow`.

2. **Zustand persist hydration race** (resolved by removal): Persist auto-hydration changes store mid-render, violating React 19's stricter `useSyncExternalStore` contract. Originally fixed with `skipHydration: true` + manual `await rehydrate()`. Later eliminated entirely by removing persist middleware — auth tokens now use HttpOnly cookies + memory-only Zustand store.

3. **JWT bearer eager config**: `builder.Configuration.Get<JwtSettings>()` in Program.cs runs before `CustomWebApplicationFactory.ConfigureAppConfiguration` overrides. Token signed with test key, validated with dev key → 401. Fix: deferred options pattern.

4. **`WebApplicationFactory.WithWebHostBuilder()`** returns base `WebApplicationFactory<Program>`, not `CustomWebApplicationFactory`. Instance methods aren't accessible. Fix: extension methods on `WebApplicationFactory<Program>`.

5. **TestServer HttpClient maintains a cookie jar**: `CreateClient()` enables `UseCookies = true` by default. Login responses set cookies, and subsequent requests from the same client include them. Tests needing a pristine "no cookie" client must use `HandleCookies = false`.

### Verification

| Check             | Result |
|-------------------|--------|
| Backend Build     | 9/9 projects, 0 warnings, 0 errors |
| Frontend Build    | Succeeds |
| Backend Tests     | 246 passed, 0 failed |
| Frontend Tests    | 80 passed, 0 failed |
| Frontend Lint     | Clean |
| E2E Tests         | 42 passed, 0 failed |
| **Total Tests**   | **368** |

---

## CP2 Security Hardening: HttpOnly Cookies & Medium Fixes

**Date**: 2026-02-15

### The Problem

After completing CP2's initial auth implementation, a security review identified several concerns:

1. **Refresh tokens stored in localStorage** — vulnerable to XSS. Any injected script can read localStorage and exfiltrate tokens. This was the most critical issue.
2. **PII in logs** — email addresses logged in plain text during login/register attempts.
3. **No CORS configuration** — required for production deployments and cookie-based auth.
4. **No HTTPS enforcement** — required for the `Secure` cookie flag.
5. **No security response headers** — missing standard protections (X-Frame-Options, CSP, etc.).
6. **No refresh token cleanup** — revoked/expired tokens accumulate forever in the database.

### The Architecture: Memory-Only Access Token + HttpOnly Refresh Cookie

A split-token architecture was adopted that eliminates localStorage entirely:

**Access token** — kept in JavaScript memory (Zustand store, NOT persisted). Lost on page refresh. Sent as `Authorization: Bearer` header on API calls. Short-lived (15 min).

**Refresh token** — set as an HttpOnly cookie by the server. Never accessible to JavaScript. Scoped to `Path=/api/auth` so it's only sent on auth endpoints. `SameSite=Strict` prevents CSRF. `Secure` flag enforced in production.

**Session restoration on page refresh**: `AuthHydrationProvider` makes a silent `POST /api/auth/refresh` on mount. The browser automatically sends the HttpOnly cookie. If successful, the server returns a new access token in the response body, which gets stored in Zustand memory. If the cookie is expired/invalid, the user lands on the login page.

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| HttpOnly cookie for refresh token (not localStorage) | XSS can't read HttpOnly cookies; eliminates the most dangerous token theft vector |
| SameSite=Strict (not Lax) | Cookie only sent on same-site requests; no CSRF protection needed |
| Path=/api/auth (not /) | Cookie not sent on `/api/tasks`, `/api/boards`, etc.; minimizes exposure surface |
| No CSRF tokens | SameSite=Strict + path-scoped cookie makes traditional CSRF impossible |
| Memory-only access token (not sessionStorage) | sessionStorage is also readable by XSS; pure JS variable is the only storage invisible to injected scripts |
| Silent refresh on page load | Restores session without user interaction; feels like "staying logged in" despite memory-only tokens |
| Removed Zustand persist entirely | No localStorage usage at all for auth — eliminates the attack surface completely |
| Email masking in logs (`u***@example.com`) | Prevents PII exposure in log aggregators, satisfies GDPR/HIPAA log requirements |
| Background cleanup service (6h interval) | Prevents unbounded RefreshTokens table growth from accumulated expired/revoked tokens |

### What Was Deferred

- **Token family detection**: Would detect refresh token reuse (stolen token scenario). Requires a `FamilyId` column on RefreshToken and more complex revocation logic. Deferred because it needs a DB migration and the current single-device-per-user model limits the attack surface.
- **HaveIBeenPwned password check**: Would reject passwords found in known breaches. Deferred because it requires an external API dependency and needs graceful degradation when the API is unavailable.

### Implementation Details

**Middleware pipeline order** (matters for security):
```
SecurityHeaders → CorrelationId → ErrorHandling → HSTS/HTTPS → CORS → RateLimiter → Auth → Authorization → Endpoints
```

**Security headers added**:
- `X-Content-Type-Options: nosniff` — prevents MIME-type sniffing
- `X-Frame-Options: DENY` — prevents clickjacking
- `Referrer-Policy: strict-origin-when-cross-origin` — limits referer leakage
- `X-XSS-Protection: 0` — disables browser's broken XSS filter (CSP is better)
- `Content-Security-Policy` — restricts script/style/image sources to `'self'`

**E2E test adaptation**: `loginViaStorage()` was renamed to `loginViaApi()`. Instead of injecting localStorage, it now calls the login API, extracts the `Set-Cookie` header, injects the refresh cookie via Playwright's `context.addCookies()`, and navigates to `/` where `AuthHydrationProvider` performs a silent refresh to restore the in-memory access token.

### Verification

| Check             | Result |
|-------------------|--------|
| Backend Build     | 9/9 projects, 0 warnings, 0 errors |
| Frontend Build    | 1947 modules, 585 KB JS + 53 KB CSS |
| Backend Tests     | 262 passed, 0 failed |
| Frontend Tests    | 84 passed, 0 failed |
| Frontend Lint     | Clean |

### Lessons Learned

1. **localStorage is not a secure token store.** Any XSS vulnerability (a single unescaped user input, a compromised npm package) can read localStorage. HttpOnly cookies are the only browser storage mechanism that JavaScript categorically cannot access.

2. **SameSite=Strict + path scoping makes CSRF tokens unnecessary.** The cookie is only sent on same-site navigation to `/api/auth/*` paths. Cross-origin requests and even same-origin requests to other paths never include it. This is a simpler security model than managing CSRF tokens.

3. **Memory-only state simplifies the frontend.** Removing Zustand's `persist` middleware eliminated the `skipHydration` workaround, the `_hydrated` flag, and the Zustand 5 + React 19 rehydration race condition. The new `AuthHydrationProvider` is simpler: one `fetch` call, one `setAuth` call, done.

4. **TestServer HttpClient maintains a cookie jar.** `WebApplicationFactory.CreateClient()` creates an `HttpClient` with `UseCookies = true` by default. Login responses set cookies, and subsequent requests from the same client include them. Tests that need a "no cookie" baseline must use `HandleCookies = false`.

---

## CP2 Security Hardening: E2E Test Stabilization

**Date**: 2026-02-15

### The Problem

After switching to cookie-based auth, E2E tests became flaky. Tests failed randomly (1-2 per run, non-deterministic) with 30-second timeouts waiting for the "View switcher" nav element:

```
Error: locator.waitFor: Test timeout of 30000ms exceeded.
- waiting for getByRole('navigation', { name: 'View switcher' }) to be visible
```

Manual testing showed the app worked perfectly — the issue was in the **test architecture**, not the application.

### Root Cause Analysis

The flakiness stemmed from **shared auth state across tests**:

1. **Every test called `loginViaApi(page)`** — 42 tests = 42 login calls
2. **Refresh token rotation on every silent refresh** — old token revoked, new token issued
3. **Shared `cachedToken` and `cachedCookie` across tests** — tests reused the same credentials
4. **Parallel-like execution** (workers: 1, but describe blocks interleaved) — one test's login invalidated another test's cookie mid-flight
5. **Cleanup via `deleteAllTasks()`** — didn't address auth state pollution

**Silent failure modes**:
- If `Set-Cookie` header extraction returned empty string (instead of throwing), cookie injection silently failed
- AuthHydrationProvider's refresh got 401, app stayed on login page, test timed out
- The unnecessary `/login` pre-navigation added complexity with no benefit

### The Solution: Unique Users + Serial Execution

**Architecture change**: Each `test.describe` block gets a **fresh user account** (unique email via timestamp + counter). No cleanup needed — users never see each other's data.

**Execution model**: `test.describe.serial` with shared page/context. Login once in `beforeAll`, tests within the block run sequentially and accumulate state.

**Changes**:

1. **`helpers/auth.helpers.ts`** — Complete rewrite:
   - `loginViaApi(page)` registers a unique user per call (no shared state)
   - `createTestUser()` for API-only describe blocks (no browser needed)
   - `extractRefreshToken()` throws immediately if Set-Cookie is missing (fail-fast)
   - `waitForResponse` verifies the silent refresh succeeded before proceeding
   - Removed `/login` pre-navigation — cookie injection works at context level

2. **`helpers/api.helpers.ts`** — Removed `deleteAllTasks()` (no longer needed)

3. **Centralized config** — `e2e.config.json` for ports/DB name, `e2e.config.ts` loader

4. **Pre-test cleanup** — `scripts/cleanup.mjs` kills stale processes + deletes DB files

5. **All 6 test spec files** — Restructured:
   - `test.describe.serial` instead of parallel `test.describe`
   - Login once in `beforeAll` (42 logins → 8 logins)
   - Removed all `beforeEach(deleteAllTasks)` cleanup
   - Tests build on prior tests within the same block (sequential by design)

### Results

| Metric | Before | After |
|--------|--------|-------|
| Test stability | 1-2 random failures per run | 3/3 consecutive green runs |
| Test duration | 60-90 seconds | 20-21 seconds |
| Total logins | 42 (one per test) | 8 (one per describe block) |
| Cleanup overhead | `deleteAllTasks()` every test | Zero (users auto-isolated) |

**Performance improvement**: 3x faster, 100% stable.

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| Unique user per describe block (not per test) | True data isolation without manual cleanup; each user starts with an empty board |
| Serial execution within describe blocks | Tests share page/context, accumulate state; mimics real user flows |
| Timestamp + counter for unique emails | Guarantees uniqueness even if tests run at the same millisecond |
| `createTestUser()` for API-only blocks | No browser overhead for pure API tests (Card Ordering, Orphaned Cards) |
| `waitForResponse` verification | Catches silent refresh failures immediately instead of timing out later on nav element |
| Removed `deleteAllTasks()` entirely | Cleanup was treating symptom (shared data) not cause (shared users); unique users eliminate the need |

### Lessons Learned

1. **Flaky tests indicate architecture problems, not test problems.** The app worked perfectly. The issue was that tests shared auth state (tokens, cookies) across concurrent executions. The fix wasn't "retry until it works" — it was restructuring test isolation.

2. **Unique data partitions > manual cleanup.** Creating a fresh user per describe block (30 ms overhead) is simpler and more reliable than deleting all tasks between tests (N×DELETE API calls + race conditions).

3. **Serial execution for stateful flows.** Task lifecycle tests build on each other (create → complete → uncomplete). Fighting this with cleanup is swimming upstream. Embracing it with `.serial` makes tests read like user stories.

4. **Fail-fast assertions prevent silent failures.** The original `extractRefreshToken()` silently returned `''` if the header was missing. Tests timed out 30 seconds later when auth failed. Throwing immediately surfaces the root cause in stack traces.

5. **Test architecture should match production architecture.** The new E2E auth flow (register → inject cookie → silent refresh → verify response → wait for UI) exactly mirrors production. The old flow (navigate to /login → inject → navigate to /) had no real-world equivalent.

---

## Release: v0.2.0 — Authentication & Authorization

**Date: February 15, 2026**

Tagged and released v0.2.0, covering Checkpoint 2 (Authentication & Authorization).

### What Shipped

- **ASP.NET Core Identity + JWT** — Register, Login, Logout, Refresh, GetCurrentUser
- **Cookie-based refresh tokens** — HttpOnly, SameSite=Strict, path-scoped, with background cleanup
- **User-scoped data isolation** — `ICurrentUserService` throughout all handlers
- **Security hardening** — SecurityHeadersMiddleware, account lockout, PII masking, CORS
- **Frontend auth** — Login/Register pages, Zustand auth store (memory-only), protected routes, user menu
- **Identity domain** — User entity, DisplayName and Email value objects
- **388 tests** — 262 backend + 84 frontend + 42 E2E, 100% passing

### Key Architecture Decisions

- HttpOnly cookie refresh tokens over localStorage (eliminates XSS risk)
- Memory-only access tokens in Zustand (no persist middleware for React 19 compatibility)
- `ValueObject<T>` base class + `IReconstructable` interface (reduced boilerplate across 11 VOs)
- Unique users per E2E describe block (eliminates flaky tests from shared state)

---

## Checkpoint 3: Rich UX & Polish

**Date: February 15, 2026**

All 10 CP3 items implemented across 10 atomic commits (1 backend + 9 frontend), adding 65 new frontend tests for a total of 149.

### 3.1 What Was Built

**Drag-and-Drop (CP3.1)**: @dnd-kit integration on the kanban board. Cross-column moves trigger `MoveCard` API (neighbor-based rank computation from CP1). Within-column reorder updates card rank without status change. Touch and keyboard accessible.

**Task Detail Sheet (CP3.3)**: Slide-over panel (`Sheet` from Shadcn/ui) for viewing and editing task details. Inline editing for title (blur-to-save), description (textarea), due date (Calendar popover), priority (select), and tags. `TaskDetailSheetProvider` context lets any component open the sheet by task ID. 12 tests covering loading, error, and edit states.

**Filters & Search (CP3.4)**: Two-layer approach — backend query parameters (`status`, `priority`, `search`, `tags`, `includeArchived`) added to `GET /api/tasks` for efficient server-side filtering, plus a client-side `filterTasks()` utility (12 tests) for instant local filtering. `FilterBar` widget provides search input, status/priority dropdowns, and tag toggles.

**Dark/Light Theme (CP3.5)**: `ThemeProvider` reads system preference via `prefers-color-scheme` media query, applies `dark` class to `<html>`. `ThemeToggle` button cycles through light/dark/system modes. Zustand store with `persist` middleware saves preference to localStorage (separate from auth store which deliberately avoids persistence). 8 tests for the store, 5 for the toggle.

**Responsive Design (CP3.6)**: `useMediaQuery` hook (3 tests) for breakpoint detection. Kanban columns use horizontal snap-scroll on mobile. List view adapts to compact layout. DashboardLayout adjusts header and navigation for small screens.

**Loading Skeletons (CP3.7)**: `BoardSkeleton` mimics 3-column kanban with pulsing card placeholders. `ListSkeleton` mimics grouped task list. Both used by routes during `isLoading` state.

**Empty States (CP3.8)**: `EmptyBoard` shows a friendly illustration with "Create your first task" CTA when the board has no tasks. `EmptySearchResults` appears when filters return nothing, with a "Clear filters" action.

**Toast Notifications (CP3.9)**: All CRUD mutation hooks (`useCreateTask`, `useCompleteTask`, `useMoveTask`, etc.) now show success/error toasts via sonner. `toast-helpers.ts` provides consistent formatting.

**Error Boundaries (CP3.10)**: `RouteErrorBoundary` component configured as `errorElement` on every route. Catches render errors, displays a recovery UI with "Try Again" (re-renders) and "Go Home" (navigates to `/`) buttons. 4 tests.

### 3.2 New Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `date-fns` | 4.1.0 | Date formatting and manipulation (tree-shakeable, functional) |
| `react-day-picker` | 9.13.2 | Calendar widget backing Shadcn Calendar component |

Note: `@dnd-kit/core`, `@dnd-kit/sortable`, `@dnd-kit/utilities` were already added in CP1.

### 3.3 New Shadcn/ui Components

| Component | Used By |
|-----------|---------|
| `Sheet` | TaskDetailSheet (slide-over panel) |
| `Calendar` | Due date picker in TaskDetailSheet |
| `Popover` | Calendar trigger in TaskDetailSheet |
| `Label` | Form labels in TaskDetailSheet |

### 3.4 Key Decisions

**Sheet over Dialog for task details**: A slide-over sheet keeps the board or list visible in the background, providing spatial context. Modals feel heavier and block the underlying view. On mobile, sheets support swipe-to-dismiss gestures naturally.

**Client-side + server-side filtering**: Filters are applied client-side for instant response (no network round-trip). Backend query parameters were also added so that when the dataset grows beyond what fits in a single page, server-side filtering is already wired up. This dual approach means the UX is fast today and the architecture is ready for pagination.

**Separate Zustand store for theme**: The auth store deliberately avoids `persist` middleware (security decision from CP2). Theme preference is non-sensitive UI state that should survive page refresh. A separate `useThemeStore` with its own persist configuration keeps the two concerns isolated.

**Per-route error boundaries**: A single global error boundary would unmount the entire app on any error. Per-route boundaries (using React Router's `errorElement`) contain failures to the affected route while leaving navigation and other routes functional.

**date-fns over dayjs/moment**: Tree-shakeable (only import what you use), pure functional API (no global mutation), excellent TypeScript support. At this usage level (formatting + relative dates), the bundle impact is minimal.

### 3.5 Architecture Patterns

**TaskDetailSheetProvider**: Uses React Context to provide `openSheet(taskId)` / `closeSheet()` to any descendant component. The provider sits at the page level, above the views but below the layout. This avoids prop-drilling the sheet state through the component tree.

**Filter state in Zustand**: `useTaskViewStore` was extended with filter fields (`searchQuery`, `statusFilter`, `priorityFilter`, `tagFilter`). The FilterBar reads and writes to this store. Views subscribe to the filter state and apply `filterTasks()` to the query results. This keeps filter state persistent across view switches (board ↔ list).

**Skeleton composition**: Skeletons mirror the exact layout of their loaded counterparts (same grid columns, card heights, spacing). This prevents layout shift when data arrives. Each skeleton is a separate component, not a loading prop on the real component — keeping the loaded component clean.

### 3.6 E2E Tests & Polish

Added 13 new E2E tests covering CP3 features:

- **Task Detail Sheet** (5 tests): open sheet by clicking card, verify fields, edit description (persists across close/reopen), inline title editing, delete task via sheet
- **Filter & Search** (5 tests): search by title (debounced), clear search, filter by priority, clear all filters, empty search results state
- **Theme Toggle** (3 tests): default dark class, cycle through themes, persist across navigation

Fixed 5 pre-existing E2E tests broken by CP3's EmptyBoard component:
- `auth.spec.ts`: Fresh users now see "Your board is empty" instead of column headings after login/register
- `navigation.spec.ts`: Seeded a task so columns render for navigation tests
- `task-board.spec.ts`: Updated empty state assertion from "No tasks" x3 to "Your board is empty"
- `card-ordering.spec.ts`: Wait for task text instead of column heading in cross-column test

Also added `animate-fade-in` to kanban SortableTaskCard for NFR-011.1 (task creation animation). TaskListView already had `animate-fade-in-up`.

### 3.7 Verification

| Check | Result |
|---|---|
| **Backend Build** | 9/9 projects, 0 warnings, 0 errors |
| **Frontend Build** | 691 KB JS + 66 KB CSS |
| **Backend Tests** | 262 passed, 0 failed, 0 skipped |
| **Frontend Tests** | 161 passed, 0 failed (25 test files) |
| **E2E Tests** | 55 passed, 0 failed |
| **Frontend Lint** | Clean, no issues |

---

## Release: v0.3.0 — Rich UX & Polish

**Date: February 15, 2026**

Third release, covering Checkpoint 3. The app now has dark mode, a filter bar, task detail sheet, loading skeletons, empty states, error boundaries, toast notifications, and micro-animations. The test count grew from 388 to 478 (262 backend + 161 frontend + 55 E2E).

### Release Stats

| Metric | Value |
|--------|-------|
| Backend tests | 262 |
| Frontend tests | 161 |
| E2E tests | 55 |
| Total tests | 478 |
| New frontend components | 14 (atoms, widgets, views, hooks, stores) |
| New Shadcn/ui primitives | 4 (Sheet, Calendar, Popover, Label) |
| Build warnings | 0 |

### Key Additions

- **Dark/light theme** with system preference detection and persisted Zustand store
- **Filter bar** with real-time search, status/priority/tag multi-select filters
- **Task detail sheet** (slide-over) for inline editing of all task fields
- **Loading skeletons** and **empty state components** for polished UX
- **Route error boundaries** with retry/home recovery UI
- **Toast notifications** for all CRUD mutations via Sonner
- **Backend filter/search API** query params on task listing endpoint
- **55 E2E tests** covering CP3 features plus fixes for existing tests

### Lessons Learned

1. **EmptyBoard changed the test contract**: Replacing "No tasks" columns with a single EmptyBoard component broke 5 existing E2E tests. Tests that expected column headings on fresh boards needed to seed a task first.
2. **Tag deduplication matters**: Case-insensitive duplicate prevention for tags improved data quality without user friction.
3. **Completed tasks shouldn't look overdue**: Suppressing overdue styling on done tasks was a small fix with big UX impact.

---

## Checkpoint 4: Production Hardening

**Date: February 16, 2026**

All 10 CP4 items implemented across 10 atomic commits on `feature/cp4-production-hardening`, adding 59 new backend tests and 3 new frontend tests.

### 4.1 What Was Built

**Serilog Structured Logging (CP4.2)**: Replaced built-in logging with Serilog. Added `PiiMaskingEnricher` that auto-masks properties named `Email`, `Password`, `DisplayName` etc. Correlation ID pushed to `LogContext` via middleware. Console + OpenTelemetry sinks configured.

**SystemAdmin Role (CP4.7)**: Third role tier with two authorization policies: `RequireAdminOrAbove` (Admin | SystemAdmin) and `RequireSystemAdmin` (SystemAdmin only). Roles seeded on startup alongside User and Admin.

**Audit Trail (CP4.4)**: New Administration bounded context with `AuditEntry` entity tracking security-relevant actions. Domain event handlers create audit entries for user registration, login, task creation/deletion/completion, role changes, and PII reveals. `IRequestContext` captures IP address and user agent per HTTP request. Paginated `SearchAuditLogQuery` with filters.

**Admin Panel (CP4.5)**: Backend admin endpoints for user management (list, search, assign/remove roles, deactivate/reactivate). Frontend `AdminLayout` with sidebar navigation, `UserManagementView` with paginated table, role badges, action dropdowns, and `RoleAssignmentDialog`. `AdminRoute` guard checks Admin/SystemAdmin roles.

**Data Encryption at Rest (CP4.10)**: `AesFieldEncryptionService` implementing AES-256-GCM with random 12-byte IV prepended to ciphertext. `EncryptedEmail` and `EncryptedDisplayName` columns added to `AspNetUsers` alongside Identity's existing columns (Identity uses `NormalizedEmail` for lookups, so no breaking changes). Key from configuration (`Encryption:FieldEncryptionKey`).

**PII Redaction (CP4.3)**: `PiiRedactor` utility masks emails (`j***@example.com`) and names in admin views by default. SystemAdmin can reveal real values via `POST /api/admin/users/{id}/reveal`, which decrypts encrypted columns and creates a `PiiRevealed` audit entry. UI shows revealed values with amber highlight and 30-second auto-hide.

**Audit Log Viewer (CP4.6)**: `GET /api/admin/audit` endpoint with date range, action type, actor, and resource type filters. Frontend `AuditLogView` with Shadcn Select/Input/Table components and color-coded action badges (green for creates, red for deletes, amber for role changes, purple for PII reveals).

**i18n (CP4.8)**: i18next with `i18next-browser-languagedetector` for automatic language detection. 158 translation keys in `en.json` and `pt-BR.json` organized by domain (`auth.login.title`, `tasks.filter.search`, `admin.users.title`, etc.). All 40+ frontend components updated with `useTranslation()` + `t()`. `LanguageSwitcher` dropdown in dashboard footer. Test setup uses dedicated `i18n-setup.ts` without browser detection.

**Frontend OTel (CP4.1b)**: W3C `traceparent` header (`00-{traceId(32hex)}-{parentId(16hex)}-01`) generated on every API request via `generateTraceparent()` in a dedicated `traceparent.ts` module. Uses native `crypto.getRandomValues()` — zero new npm dependencies.

### 4.2 New Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Serilog.AspNetCore` | latest | Structured logging framework |
| `Serilog.Sinks.Console` | latest | Console output with JSON formatting |
| `Serilog.Sinks.OpenTelemetry` | latest | OTel log pipeline integration |
| `i18next` | 25.x | Frontend internationalization framework |
| `react-i18next` | 16.x | React bindings for i18next |
| `i18next-browser-languagedetector` | 8.x | Browser language auto-detection |

### 4.3 Architecture Additions

**Administration Bounded Context**: New domain context (`LemonDo.Domain.Administration`) with `AuditEntry` entity, `AuditAction` enum, and `IAuditEntryRepository`. Application layer has event handlers (`AuditOnUserRegistered`, `AuditOnTaskCreated`, etc.) and `SearchAuditLogQuery`. This context is read-heavy (audit log search) with append-only writes (event handlers).

**IRequestContext**: Abstracts HTTP request metadata (UserId, IpAddress, UserAgent) for audit entries. Implemented by `HttpRequestContext` in the API layer, injected into command/event handlers.

**Field Encryption Service**: `IFieldEncryptionService` with `Encrypt(plaintext)` / `Decrypt(ciphertext)`. AES-256-GCM implementation with 12-byte random IV + 16-byte authentication tag. Output format: `Base64(IV[12] + Tag[16] + Ciphertext[N])`.

### 4.4 Key Decisions

| Decision | Rationale |
|----------|-----------|
| Separate encrypted columns (not replacing Identity columns) | Identity uses NormalizedEmail for lookups — encrypting the source column would break authentication |
| AES-256-GCM over AES-CBC | GCM provides authenticated encryption (tamper detection), CBC requires separate HMAC |
| Admin views show redacted PII by default | HIPAA-ready: minimum necessary access principle. Reveal is explicit and audited |
| i18next over react-intl | Simpler API, first-class namespace support, wider ecosystem (language detector, ICU) |
| Manual traceparent over OTel Browser SDK | OTel Browser SDK adds 200KB+ bundle; manual W3C header is 20 lines and correlates traces end-to-end |
| Administration as separate bounded context | Audit + admin concerns are orthogonal to Task/Board contexts; separate context prevents coupling |

### 4.5 Gotchas & Lessons

1. **AuthenticationTagMismatchException in .NET 10**: AES-GCM throws `AuthenticationTagMismatchException` (subclass of `CryptographicException`) for tampered data. MSTest 4's `Assert.ThrowsExactly<T>` checks exact type, so `ThrowsExactly<CryptographicException>` fails for tamper tests.

2. **i18next-browser-languagedetector (not languagedetection)**: The npm package name is `i18next-browser-languagedetector` (ends in "or", not "ion").

3. **AuditLogFilters type vs Record<string, ParamValue>**: TypeScript interfaces don't have index signatures. API client's `get()` expects `Record<string, ParamValue>`. Fix: type assertion at the call site.

4. **i18n test setup**: The production i18n config includes browser language detection, which fails in JSDOM (no `navigator.languages`). Tests use a dedicated `i18n-setup.ts` with English-only config and no detection plugins.

### 4.6 Verification

| Check | Result |
|---|---|
| **Backend Build** | 9/9 projects, 0 warnings, 0 errors (5.7s) |
| **Frontend Build** | 2954 modules, 780 KB JS + 68 KB CSS (3.6s) |
| **Backend Tests** | 321 passed, 0 failed, 0 skipped |
| **Frontend Tests** | 164 passed, 0 failed (26 test files) |
| **Frontend Lint** | Clean, no issues |

---

## PII/PHI Break-the-Glass Enhancement

> **Date**: 2026-02-16
> **Branch**: `develop`

### What Was Built

Added HIPAA-modeled break-the-glass controls to the PII reveal flow. Previously, SystemAdmins could reveal PII with a bare API call and no justification. Now the flow requires:

1. **Mandatory justification**: `PiiRevealReason` enum with 7 values (SupportTicket, LegalRequest, AccountRecovery, SecurityInvestigation, DataSubjectRequest, ComplianceAudit, Other). "Other" requires free-text details.
2. **Password re-authentication**: Admin must re-enter their account password before PII is revealed. Uses `UserManager.CheckPasswordAsync` (not sign-in) to avoid triggering lockout counters.
3. **Structured audit trail**: Audit details are stored as JSON (`PiiRevealAuditDetails` record) with reason, details, and comments — enabling compliance reporting and analytics.
4. **Time-limited secure viewer**: Revealed PII auto-hides after 30 seconds with a visual countdown (progress bar + seconds badge). "Hide" button for early dismissal.
5. **PHI-safe audit logging**: Task titles (potential PHI) stripped from `AuditOnTaskCreated` handler — only task ID and priority are logged.

### Key Decisions

- **Password re-auth over MFA**: MFA not yet implemented; password provides "something you know" as second factor beyond the session. MFA step-up added to roadmap.
- **Task titles as PHI**: Completely stripped from audit logs. Hashing isn't useful for audit review, and redaction patterns leak partial info. Task ID in the audit entry allows authorized lookup if needed.
- **Tags not PHI**: Categorical labels (e.g., "medical") don't identify a person — the association with a user's task creates PHI, already protected behind auth.
- **30s hardcoded timer**: Security policy should not be user-adjustable. Server is stateless (returns PII once); server-enforced TTL would require time-limited encrypted tokens — deferred.

### Gotchas

- **Rate limiter eager config read**: `builder.Configuration.GetValue()` in Program.cs reads before `WebApplicationFactory` config overrides are applied (same pattern as JWT deferred config issue). Fixed by switching from `AddFixedWindowLimiter` to `AddPolicy` with deferred `IConfiguration` read from `context.RequestServices`.
- **Radix Select in jsdom**: Radix UI Select portals don't work in jsdom (`target.hasPointerCapture is not a function`). Frontend tests avoid direct Select dropdown interaction — test form state and rendering instead.

---

---

## Identity/Domain Separation: PII Zero-Trust Architecture

**Date**: 2026-02-16
**Branch**: `feature/pii-zero-trust`

### The Problem

After completing CP4, the architecture had a critical flaw: ASP.NET Identity (`ApplicationUser`) owned ALL user data — `DisplayName`, `CreatedAt`, `IsDeactivated`, `EncryptedEmail`, and `EncryptedDisplayName` lived on the Identity entity. Meanwhile, the domain `User` entity was ephemeral — created during registration but never persisted. It raised `UserRegisteredEvent`, but the event was silently dropped because the entity never reached EF Core's change tracker.

This violated DDD's separation of concerns: Identity should handle **credentials and authorization only**, while the domain layer should own **user profile data and business state**.

### The Architecture Redesign

Responsibilities were split across two tables with a shared ID:

**AspNetUsers** (Identity — credentials only):
- `Id` (PK)
- `UserName` (stores SHA-256 email hash for lookups)
- `PasswordHash`, `SecurityStamp`, `LockoutEnd`, `AccessFailedCount` (Identity's built-in fields)
- All custom user data fields **removed**

**Users** (Domain — profile + PII):
- `Id` (PK, matches AspNetUsers.Id)
- `RedactedEmail`, `RedactedDisplayName` (stored, used for display)
- `EmailHash` (unique index, for hash-based lookups)
- `EncryptedEmail`, `EncryptedDisplayName` (EF shadow properties, AES-256-GCM ciphertext)
- `IsDeactivated`, `CreatedAt`, `UpdatedAt` (business state)

### Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **User entity stores redacted strings, not VOs** | `RedactedEmail: string` vs `Email: Email` — storing `"j***@example.com"` in an `Email` VO creates semantic confusion. VOs used during `Create()` for validation, then `.Redacted` values extracted and stored. |
| **UserRepository handles encryption transparently** | `AddAsync(user, email, displayName)` receives validated VOs, computes hash/redacted/encrypted forms, stores via EF shadow properties. Callers don't see encryption details. |
| **IAuthService becomes credential-focused** | Removed `RegisterAsync` (returned AuthResult with user data). Now: `CreateCredentialsAsync(userId, emailHash, password)` → no user profile. Handlers orchestrate Identity + domain User separately. |
| **AuthTokens record separates tokens from user data** | New `AuthTokens(AccessToken, RefreshToken, Roles)` record. Handlers load domain User, generate tokens, combine into `AuthResult` at the last mile. |
| **Identity.UserName stores email hash** | Identity's `FindByNameAsync(emailHash)` provides O(1) login lookup. No need for a separate `EmailHash` column on `AspNetUsers` — repurposes an existing indexed field. |
| **Admin search via exact email hash** | Admin search by partial string no longer works (was searching redacted values). Now: search by exact email (hashed) or partial redacted display name. |
| **ISensitivePii marker on VOs** | `Email` and `DisplayName` VOs implement `ISensitivePii` with a `Redacted` property. This makes PII awareness a domain-level concern, not an infrastructure detail. |
| **CreateDefaultBoardOnUserRegistered event handler** | Domain User is now persisted, so `UserRegisteredEvent` is dispatched via `SaveChangesAsync`. Event handler creates default board for new users. |

### Migration Strategy

**EF Migration**: `20260216125919_SeparateUserFromIdentity`
1. Drop custom columns from `AspNetUsers` (DisplayName, EncryptedEmail, EncryptedDisplayName, CreatedAt, IsDeactivated)
2. Create `Users` table with all domain user columns plus shadow properties
3. Add unique index on `Users.EmailHash`

**No data migration needed** — this was implemented fresh on the `feature/pii-zero-trust` branch before any production users existed. In a production rollout scenario, an app-startup migration service would:
1. Read plaintext columns from existing `AspNetUsers` rows
2. Encrypt them to populate `Users.EncryptedEmail/EncryptedDisplayName`
3. Compute `EmailHash` and redacted forms
4. Insert into `Users` table
5. Drop the old columns once migration is verified

### Implementation Phases

**Phase 1 (Domain)**: Enhanced `User` entity with `RedactedEmail`/`RedactedDisplayName` (strings), `Deactivate()`/`Reactivate()` methods, `Reconstitute()` for persistence. Created `IUserRepository` with `AddAsync(user, email, displayName)`. Added `ISensitivePii` interface to `Email`/`DisplayName` VOs with `Redacted` property. Created `PiiHasher.HashEmail()` utility.

**Phase 2 (Application)**: Split `IAuthService` into credential-focused methods. Created `AuthTokens` record. Rewrote `RegisterUserCommandHandler` (orchestrates Identity credentials + domain User + board creation), `LoginUserCommandHandler` (auth → load user → tokens), `RefreshTokenCommandHandler` (refresh → load user → tokens). Created `IPiiAccessService` for audited decryption.

**Phase 3 (Infrastructure)**: Stripped `ApplicationUser` to credential shell. Created `UserConfiguration` with shadow properties for `EmailHash`, `EncryptedEmail`, `EncryptedDisplayName`. Implemented `UserRepository` with transparent encryption. Rewrote `AuthService` for hash-based login (`FindByNameAsync(emailHash)`). Rewrote `AdminUserQuery` to join `Users` + Identity. Implemented `PiiAccessService` with `AccessForSystemAsync` (system decryption with audit) and `RevealForAdminAsync` (admin break-the-glass).

**Phase 4 (API + Migration)**: EF migration created. Updated `AuthEndpoints.GetMe` to load from `IUserRepository`. Updated dev seed in `Program.cs` to create dual records (Identity credentials + domain User).

**Phase 5 (Tests)**: Added 22 new tests covering all the new surface area:
- **P1 unit tests**: `Email.Redacted` (5), `DisplayName.Redacted` (5), `PiiHasher.HashEmail` (5), `RefreshTokenCommandHandler` (3), `CreateDefaultBoardOnUserRegistered` (2)
- **P2 scenario tests**: Deactivated user login rejection (1), PII reveal audit trail verification (1)
- **P3 E2E**: Admin PII reveal flow via browser (1 spec file)

Deleted 2 obsolete tests (`CreatedAtMigrationTests.cs`) for a migration workaround that no longer applies.

### Files Changed Summary

**49 files changed** (+2,125 insertions, -460 deletions):
- 13 new files (IPiiAccessService, IUserRepository, UserRepository, UserConfiguration, PiiAccessService, PiiHasher, ISensitivePii, SystemPiiAccessReason, CreateDefaultBoardOnUserRegistered, AuthTokens, 3 test files, 2 migration files)
- 24 modified source files (User entity, Email/DisplayName VOs, IAuthService, AuthService, command handlers, AdminUserService, ApplicationUser, JwtTokenService, etc.)
- 11 modified test files (UserTests, EmailTests, DisplayNameTests, RegisterUserCommandHandlerTests, LoginUserCommandHandlerTests, AdminEndpointsTests, AuthEndpointTests, CustomWebApplicationFactory, etc.)
- 1 deleted test file (CreatedAtMigrationTests.cs)

### Test Coverage

| Check | Result |
|-------|--------|
| **Backend Tests** | **355 passed** (up from 333), 0 failed |
| **Frontend Tests** | **187 passed**, 0 failed |
| **E2E Tests** | **56 passed** (up from 55), 0 failed |
| **Total** | **598 tests** |

### Lessons Learned

1. **Identity owns credentials, domain owns data** — ASP.NET Identity is designed for authentication, not business data. Overloading it with profile fields, PII encryption, and business flags creates coupling and violates the Single Responsibility Principle. The separation makes each layer clearer: Identity handles lockout/passwords/roles, domain User handles lifecycle/deactivation/profile.

2. **EF migration snapshots can interfere with EnsureCreated()** — In theory, `EnsureCreated()` uses the compiled model from `DbContext.OnModelCreating()`, not migration snapshots. In practice, stale migration snapshots caused 84 test failures with "NOT NULL constraint failed: AspNetUsers.CreatedAt" even though `ApplicationUser` had no `CreatedAt` property. Removing the stale migration via `dotnet ef migrations remove` and regenerating resolved it. This may be a .NET 10 behavior change.

3. **Hash-based admin search is a paradigm shift** — Admin search previously used partial plaintext matches (`?search=alice` matched `alice@example.com`). Hash-based lookup requires exact email match (`?search=alice@example.com`). Partial redacted display name search still works (`?search=Ali` matches `A***e`). Tests must store the exact registration email and use it for admin queries.

4. **Shadow properties isolate protected data implementation from domain** — The domain `User` entity has no knowledge of encryption, hashing, or protected data storage details. `UserRepository` uses EF shadow properties (`EmailHash`, `EncryptedEmail`, `EncryptedDisplayName`) to store sensitive data without coupling the domain model to encryption concerns. This preserves domain purity while enabling zero-trust protected data handling. The same pattern was later applied to `TaskRepository` with `EncryptedSensitiveNote`.

5. **IProtectedData makes sensitivity awareness domain-level** — Instead of infrastructure or application layers "knowing" which fields are sensitive and applying redaction/hashing/encryption ad-hoc, the domain VOs themselves declare "I am protected data" via the marker interface and provide their own redaction logic. This centralizes the policy and makes it impossible to forget redaction when adding new protected data fields (e.g., `SensitiveNote` on tasks).

---

## Terminology Rename: PII → ProtectedData

**Date**: 2026-02-16
**Branch**: `feature/pii-zero-trust`

### Motivation

The term "PII" (Personally Identifiable Information) was too narrow. The same encryption, redaction, and break-the-glass reveal system protects PII, PHI (Protected Health Information), and other sensitive data categories. Renaming to "ProtectedData" makes the system generic and extensible.

### Scope

Pure terminology rename with zero functional changes:

- **Domain**: `ISensitivePii` → `IProtectedData`, `PiiHasher` → `ProtectedDataHasher`, `PiiRevealReason` → `ProtectedDataRevealReason`, `SystemPiiAccessReason` → `SystemProtectedDataAccessReason`, `AuditAction.PiiRevealed` → `ProtectedDataRevealed`
- **Application**: `IPiiAccessService` → `IProtectedDataAccessService`, `RevealPiiCommand` → `RevealProtectedDataCommand`, `PiiRedactor` → `ProtectedDataRedactor`
- **Infrastructure**: `PiiAccessService` → `ProtectedDataAccessService`
- **API**: `PiiDestructuringPolicy` → `ProtectedDataDestructuringPolicy`, `PiiMaskingEnricher` → `ProtectedDataMaskingEnricher`
- **Frontend**: `PiiRevealDialog` → `ProtectedDataRevealDialog`, i18n keys `piiRevealDialog.*` → `protectedDataRevealDialog.*`
- **Tests**: All test files renamed to match
- **Docs**: CHANGELOG, GUIDELINES, DOMAIN, SCENARIOS updated
- **E2E**: `admin-pii-reveal.spec.ts` updated (file kept, content renamed)

Migration files left untouched (historical records).

---

## Task SensitiveNote Feature

**Date**: 2026-02-16
**Branch**: `feature/pii-zero-trust`

### Motivation

Tasks often contain sensitive information (medical notes, legal references, confidential details) that shouldn't be visible at rest. The existing ProtectedData infrastructure (AES-256-GCM encryption, break-the-glass reveals, audit trail) provides the perfect foundation for extending encryption to task-level content.

### Design Decisions

1. **Reuse existing encryption infrastructure** — `IFieldEncryptionService` already handles AES-256-GCM encryption/decryption. Tasks reuse the same service rather than duplicating encryption logic.

2. **Shadow property pattern** — Same as User protected data: the domain `Task` entity only stores `RedactedSensitiveNote` (the string `"[PROTECTED]"` or null). The actual encrypted content lives in an EF Core shadow property `EncryptedSensitiveNote`, managed entirely by `TaskRepository`.

3. **Two decryption paths** — Owner re-authentication (password required, audited) via `ViewTaskNoteCommand`, and admin break-the-glass (justification + password required, fully audited) via `RevealTaskNoteCommand`. Both produce `SensitiveNoteRevealed` audit entries.

4. **IProtectedData marker** — `SensitiveNote` implements `IProtectedData` like `Email` and `DisplayName`, making it automatically eligible for the redaction and logging policies.

5. **Frontend: explicit save for notes** — Unlike description (blur-to-save), the sensitive note uses an explicit "Save Note" button since writing encrypted content is a deliberate action. The note textarea is always empty (never shows `[PROTECTED]`) — users write new content to replace, or click "Clear" to remove.

### Implementation Highlights

- **Domain**: `SensitiveNote` value object (max 10K chars), `Task.UpdateSensitiveNote()` mutation, `ITaskRepository` extended with encryption parameters
- **Infrastructure**: `TaskRepository` encrypts/decrypts via shadow property, new `GetDecryptedSensitiveNoteAsync` method
- **Application**: `ViewTaskNoteCommand` (owner), `RevealTaskNoteCommand` (admin), updated create/update commands
- **API**: `POST /api/tasks/:id/view-note`, `POST /api/admin/tasks/:id/reveal-note`, updated create/update contracts
- **Frontend**: `TaskNoteRevealDialog` (password + 30s countdown), task detail sheet with note section, lock icon on cards
- **Tests**: 370 backend + 223 frontend = 593 total

---

## CP4 Infrastructure: Dual EF Core Migrations

**Date: February 16, 2026**

### The Problem

When SQL Server support was added in CP4, `EnsureCreatedAsync()` was used for SQL Server and `MigrateAsync()` for SQLite — a pragmatic hack. But `EnsureCreated` doesn't support incremental schema changes, so any future migration would break SQL Server deployments.

### The Solution: Separate Migration Assemblies

EF Core allows only one `ModelSnapshot` per `DbContext` per assembly, and SQLite and SQL Server produce different column types (e.g., `TEXT` vs `datetimeoffset`). Dedicated migration assemblies were created:

- **`LemonDo.Migrations.Sqlite`** — houses all 11 SQLite migrations (moved from Infrastructure)
- **`LemonDo.Migrations.SqlServer`** — houses a single `InitialCreate` migration covering the full schema

Each has its own `IDesignTimeDbContextFactory` for `dotnet ef` commands.

### Key Decisions

1. **Separate assemblies over shared migrations**: EF Core's `ModelSnapshot` is provider-specific. A single assembly would produce SQLite-typed snapshots when adding SQL Server migrations (or vice versa).

2. **Environment variable for SQL Server migration generation**: `dotnet ef` starts Program.cs which defaults to SQLite. Setting `DatabaseProvider=SqlServer` makes the runtime path select the correct provider. The design-time factory alone doesn't suffice because EF tools find both factories (from both assemblies loaded via Api's references) and fall back to the host builder.

3. **Avoid `!` in SQL Server passwords on Windows/MSYS2**: The `!` in `YourStrong!Passw0rd` causes login failures when the connection string is passed through bash scripts on Git Bash/MSYS2. Changed to `YourStr0ngPassw0rd` to avoid shell escaping issues entirely.

4. **EF Core Design package stays in Api**: The startup project needs `Microsoft.EntityFrameworkCore.Design` for `dotnet ef` to work, even though the migration projects also have it.

### Changes Summary

- **New**: `src/LemonDo.Migrations.Sqlite/` (csproj + design-time factory + 23 migration files moved from Infrastructure)
- **New**: `src/LemonDo.Migrations.SqlServer/` (csproj + design-time factory + generated InitialCreate)
- **Deleted**: `src/LemonDo.Infrastructure/Migrations/` (moved to Sqlite project)
- **Deleted**: `src/LemonDo.Infrastructure/Persistence/DesignTimeDbContextFactory.cs`
- **Modified**: `InfrastructureServiceExtensions.cs` — `MigrationsAssembly(...)` on both provider calls
- **Modified**: `Program.cs` — unconditional `MigrateAsync()` (removed `EnsureCreatedAsync` hack)
- **Modified**: Test factory — `MigrationsAssembly` on SQLite re-registration, removed `EnsureCreated`
- **Result**: 370 backend tests pass on both SQLite and SQL Server; 55/56 E2E pass (1 pre-existing failure)

---

## CP4 Infrastructure: Developer CLI (`./dev`)

**Date: February 16, 2026**

### The Problem

Every common operation required remembering long, provider-specific commands:
- `dotnet clean src/LemonDo.slnx -v quiet && dotnet build src/LemonDo.slnx`
- `export TEST_DATABASE_PROVIDER=SqlServer && export TEST_SQLSERVER_CONNECTION_STRING='...' && dotnet test --solution src/LemonDo.slnx`
- `DatabaseProvider=SqlServer dotnet ef migrations add <Name> --project src/LemonDo.Migrations.SqlServer --startup-project src/LemonDo.Api`

Connection string escaping (`!` in passwords triggers bash history expansion), multiple directory changes for frontend commands, and dual-provider migration generation all added friction.

### The Solution

A single `./dev` bash script at the project root that wraps all common operations:

- **`./dev build`** — clean + build all 11 projects
- **`./dev test backend:sql`** — automatically sets env vars, checks Docker container, runs tests
- **`./dev test e2e:sql`** — E2E with SQL Server backend, connection wired automatically
- **`./dev migrate add <Name>`** — generates migrations for BOTH providers in one command
- **`./dev verify`** — full verification gate (build + frontend build + all tests + lint)
- **`./dev docker up/down`** — manages the SQL Server container

### Design Decisions

1. **Bash over Makefile**: More portable on Windows (Git Bash), better error handling, colored output
2. **Subcommands with colon-separated variants**: `test backend:sql` reads naturally, avoids flag parsing complexity
3. **Auto-detect SQL Server**: `./dev docker up` checks if container already exists before starting
4. **Connection string defaults**: Hardcoded `sa/YourStr0ngPassw0rd` with `TEST_SQLSERVER_CONNECTION_STRING` override for CI/custom setups
5. **Pass-through args**: Extra arguments after the target are forwarded to the underlying tool (e.g., `./dev test backend -- --filter "TaskTitle"`)

---

### CP4 Infrastructure Completion

**Date**: 2026-02-16

Finalized the three remaining infrastructure tasks for CP4:

#### Terraform Azure Infrastructure (CP4.13)

Complete IaC with progressive enhancement across three deployment stages:

| Stage | Cost | What You Get |
|-------|------|-------------|
| **MVP** | ~$18/mo | B1 App Service, Basic SQL, Free Static Web App, App Insights |
| **Resilience** | ~$180/mo | S1 App Service + staging slot, S1 SQL + geo-backup, VNet + private endpoints, Front Door + WAF |
| **Scale** | ~$1.7K/mo | P2v3 App Service + auto-scale (2-10), P1 SQL + read replica, Premium Redis, CDN |

Nine reusable modules: `app-service`, `sql-database`, `key-vault`, `static-web-app`, `monitoring`, `networking`, `frontdoor`, `redis`, `cdn`. Bootstrap script initializes Azure remote state backend.

#### CI/CD Pipeline (CP4.14)

Six-job GitHub Actions workflow:
1. **backend-test** — Build + test on SQLite
2. **backend-test-sqlserver** — Test with SQL Server service container
3. **frontend-test** — pnpm install + lint + test + build
4. **docker-build** — Validates Dockerfile builds successfully
5. **deploy-staging** — On develop push, deploys API to App Service staging slot + frontend to Static Web App
6. **deploy-production** — On main push, deploys to production with environment approval gate

#### Dockerfile (CP4.15)

Multi-stage build:
- **Build stage**: SDK image, layer-cached restore, includes all migration assemblies
- **Runtime stage**: aspnet image, non-root user, curl healthcheck on `/alive`, port 8080

#### Bug Fixes

- **Password normalization**: `YourStrong!Passw0rd` → `YourStr0ngPassw0rd` across all files (deploy.yml, dev CLI, README, test infrastructure). The `!` character caused login failures when passed through Git Bash/MSYS2 scripts.
- **In-memory SQLite EnsureCreated**: Added `EnsureCreated()` safety net for in-memory SQLite test databases (ephemeral connections don't persist MigrateAsync schema).
- **Dockerfile migration assemblies**: Added missing `LemonDo.Migrations.Sqlite` and `LemonDo.Migrations.SqlServer` to Docker build (required by API project references).
- **Dockerfile curl**: Installed curl in runtime image for HEALTHCHECK command.

---

### CP4 Verification Results

| Check | Result |
|-------|--------|
| **Backend Build** | 11/11 projects, 0 warnings, 0 errors |
| **Backend Tests** | 370 passed, 0 failed, 0 skipped |
| **Frontend Tests** | 243 passed, 0 failed |
| **Frontend Lint** | Clean |
| **Frontend Build** | 4 files, 797 KB JS + 71 KB CSS |

**All 16 CP4 tasks complete.**

---

### Azure Deployment: App Service → Container Apps Migration

**Date**: 2026-02-16

#### The Quota Problem

After bootstrapping the Terraform state backend and deploying 12 of 14 resources successfully, the App Service Plan consistently failed — Azure reported **0 VM quota** for all tiers (Basic, Standard, and even Free) in East US 2. An upgrade from Free Trial to Pay-As-You-Go was attempted, but quota requests were denied.

#### The Solution: Azure Container Apps

Container Apps run on a consumption-based model that **doesn't require VM quotas**. Key advantages over B1 App Service for the MVP:

| Capability | App Service (B1) | Container Apps |
|------------|------------------|----------------|
| Auto-scaling | No (fixed 1 instance) | Yes (0-N replicas) |
| Health probes | Health check path | Liveness + Readiness + Startup |
| Zero-downtime deploy | Only with slots (Stage 2+) | Built-in via revisions |
| Cost | ~$13/month fixed | Pay-per-use (cheaper for MVP) |
| Observability | App Insights | App Insights + Log Analytics |

#### Infrastructure Changes

Created `infra/modules/container-app/` with:
- **Azure Container Registry (Basic)** — stores Docker images
- **Container App Environment** — managed Kubernetes with Log Analytics integration
- **Container App** — runs the API with secrets, health probes, and managed identity

Updated all three stages to use `container_app` module instead of `app_service`. Stage 2 and 3 have TODOs for networking module adaptation (Container Apps use VNet integration differently than App Service).

#### CI/CD Changes

Replaced ZIP deploy with Docker-based deployment:
1. `az acr login` — authenticate to Container Registry
2. `docker build + push` — build and push image tagged with commit SHA
3. `az containerapp update` — deploy new image to Container App

Deploy only triggers on push to `main` (develop only runs tests). Removed the staging deploy job since Stage 1 doesn't have a staging environment.

#### Developer CLI (`./dev infra`)

Added infrastructure management commands with dynamic Azure CLI detection:
- `./dev infra bootstrap` — one-time state backend setup
- `./dev infra init` / `plan` / `apply` / `destroy` — stage lifecycle
- `./dev infra output` / `status` / `unlock` — operational commands
- Automatic `MSYS_NO_PATHCONV=1` for Git Bash/MSYS2 path conversion fix

#### Deployed Resources (15 total)

| Resource | Name |
|----------|------|
| Container App | `ca-lemondo-mvp-eus2` |
| Container Registry | `crlemondomvpeus2` |
| Container App Environment | `cae-lemondo-mvp-eus2` |
| SQL Server | `sql-lemondo-mvp-eus2` |
| SQL Database | `sqldb-lemondo-mvp` |
| Key Vault | `kv-lemondo-mvp-eus2` |
| App Insights | `appi-lemondo-mvp-eus2` |
| Log Analytics | `log-lemondo-mvp-eus2` |
| Static Web App | `swa-lemondo-mvp-eus2` |
| Resource Group | `rg-lemondo-mvp-eus2` |

API verified healthy at `https://ca-lemondo-mvp-eus2.greenground-1ee8436d.eastus2.azurecontainerapps.io`.

#### Lessons Learned

1. **Azure Free Trial VM quotas are 0** for App Service in some regions — upgrading to Pay-As-You-Go doesn't immediately fix it
2. **Container Apps are a better MVP choice** — consumption pricing, no quota issues, built-in auto-scaling
3. **Push Docker image before Terraform apply** — Container Apps validates image existence during provisioning
4. **MSYS2 path conversion** — `/subscriptions/...` gets mangled to `C:/Program Files/Git/subscriptions/...`; fix with `MSYS_NO_PATHCONV=1`
5. **Failed Container Apps can't be imported** — must delete from Azure first, then re-create via Terraform

---

### Release v0.4.0

**Date**: 2026-02-16

Released Checkpoint 4 (Production Hardening) as v0.4.0 via gitflow.

**Release Highlights:**
- Observability: Serilog structured logging with PII masking, W3C traceparent propagation
- Security: AES-256-GCM field encryption, SystemAdmin role, protected data redaction
- Admin: User management panel + audit log viewer with filters and pagination
- i18n: English + Portuguese (Brazil) with 158 translation keys
- Task Sensitive Notes: encrypted free-text with break-the-glass admin reveal
- Infrastructure: Terraform Azure (Container Apps, ACR, SQL, Key Vault), CI/CD pipeline, Dockerfile
- Developer CLI: `./dev` unified script for all development commands
- 668 tests total (370 backend + 243 frontend + 55 E2E)

---

## Custom Domain Setup

**Date**: 2026-02-16
**Branch**: `hotfix/custom-domains` (off `main`)

### What Was Done

Configured custom domains for the Azure deployment:
- `api.lemondo.btas.dev` → Container App (API)
- `lemondo.btas.dev` → Static Web App (frontend)

Domain `btas.dev` is managed in Google Cloud DNS (project: `weaver-application`, zone: `btas-dev`).

### Three-Phase Deployment

**Phase 1 — Infrastructure prep**: Upgraded Static Web App from Free to Standard SKU (custom domains require Standard). Added `api_custom_domain` and `frontend_custom_domain` variables with empty defaults. Added Terraform outputs for DNS setup values (`container_app_ingress_fqdn`, `custom_domain_verification_id`, `static_web_app_hostname`). Applied to get the DNS target values.

**Phase 2 — DNS records**: Created three records in Google Cloud DNS:
1. TXT record: `asuid.api.lemondo.btas.dev` → Container App Environment verification ID
2. CNAME record: `api.lemondo.btas.dev` → Container App FQDN
3. CNAME record: `lemondo.btas.dev` → Static Web App default hostname

**Phase 3 — Domain binding**: Set custom domain variables in `terraform.tfvars` and applied. SWA custom domain uses `azurerm_static_web_app_custom_domain` with CNAME delegation. Container App custom domain uses `terraform_data` with Azure CLI `local-exec` provisioner.

### Key Decisions

| Decision | Rationale |
|----------|-----------|
| `terraform_data` + Azure CLI for Container App domains | `azapi_update_resource` does a full PUT requiring all secrets in the body, which fails with `ContainerAppSecretInvalid`. Azure CLI commands (`az containerapp hostname add/bind`, `az containerapp env certificate create`) are the recommended approach. |
| SWA Standard tier | Custom domains require Standard SKU ($9/mo). Free tier doesn't support them. |
| Dual CORS origins during transition | Primary: `https://lemondo.btas.dev`, Secondary: `https://<azure-default>.azurestaticapps.net`. Both origins work while DNS propagates. |
| `VITE_API_BASE_URL` env var | Frontend needs full API URL for cross-origin calls in production. Defaults to `''` so Vite dev proxy still works with relative URLs. |

### Frontend Cross-Origin Support

Added `API_BASE_URL` constant to `api-client.ts` (from `VITE_API_BASE_URL` env var, defaults to `''`). Prepended to all 6 fetch calls in `api-client.ts`, plus direct fetch calls in `token-refresh.ts` and `AuthHydrationProvider.tsx`. CI/CD pipeline sets `VITE_API_BASE_URL=https://api.lemondo.btas.dev` during frontend build.

### Cookie Auth Cross-Subdomain

The refresh token cookie uses `SameSite=Strict` and `Path=/api/auth`. Since `lemondo.btas.dev` and `api.lemondo.btas.dev` share the same registrable domain (`btas.dev`), they are "same-site" — cookies sent automatically. No cookie domain configuration changes needed.

### Gotchas

1. **`azapi_update_resource` does a full PUT**: Container App updates via AzAPI require ALL secrets in the request body, not just the changed fields. This makes it impossible to use for custom domain binding without duplicating all secret values. Azure CLI is the only viable automation path.
2. **Google Cloud DNS zone in different project**: The `btas.dev` zone was in GCP project `weaver-application`, not the default project. All `gcloud dns` commands needed `--project=weaver-application`.
3. **`./dev infra apply` is interactive**: Requires `yes` confirmation by default. Use `./dev infra apply stage1-mvp -auto-approve` for non-interactive runs.

### Files Changed (13 files)

**Terraform (8 files)**:
- `infra/stages/stage1-mvp/main.tf` — SWA Standard, CORS update, custom domain resources
- `infra/stages/stage1-mvp/variables.tf` — custom domain variables
- `infra/stages/stage1-mvp/outputs.tf` — DNS setup outputs
- `infra/stages/stage1-mvp/terraform.tfvars.example` — custom domain examples
- `infra/modules/container-app/main.tf` — secondary CORS env var
- `infra/modules/container-app/variables.tf` — `cors_origin_secondary`
- `infra/modules/container-app/outputs.tf` — container_app_id, environment_id, verification_id, ingress_fqdn

**Frontend (3 files)**:
- `src/client/src/lib/api-client.ts` — `API_BASE_URL` constant, prepended to all fetches
- `src/client/src/lib/token-refresh.ts` — import and use `API_BASE_URL`
- `src/client/src/app/providers/AuthHydrationProvider.tsx` — import and use `API_BASE_URL`

**CI/CD (1 file)**:
- `.github/workflows/deploy.yml` — `VITE_API_BASE_URL` env var on frontend build step

**Docs (1 file)**:
- `infra/README.md` — Custom Domains section with 3-phase guide

---

## Release: v0.4.1 — Custom Domains

**Date: February 16, 2026**

Hotfix release adding custom domain support for the Azure deployment. This is the first hotfix release, branching from `main` (not `develop`) per gitflow hotfix process.

### What Shipped

- Custom domains: `api.lemondo.btas.dev` (API) and `lemondo.btas.dev` (frontend)
- Managed TLS certificates via Azure (auto-provisioned and auto-renewed)
- Frontend cross-origin API support via `VITE_API_BASE_URL` env var
- Static Web App upgraded from Free to Standard SKU
- Dual CORS origins for seamless transition
- CI/CD pipeline updated with `VITE_API_BASE_URL` for production builds

### Post-Release Fixes

Two issues discovered after the v0.4.1 deploy:

1. **500 on POST /api/auth/register** — The `FieldEncryption:Key` was a hand-typed base64 string that decoded to 33 bytes instead of 32. AES-256-GCM requires exactly 32 bytes. Fix: generated a proper `RandomNumberGenerator.GetBytes(32)` key, updated Container App secret + Key Vault + tfvars.
2. **404 on SPA route refresh** — Azure Static Web Apps returns 404 for client-side routes (e.g. `/login`) because there's no physical file at that path. Fix: added `staticwebapp.config.json` with `navigationFallback` rewriting non-asset routes to `/index.html`.

### Hotfix Lessons

1. **Terraform `local-exec` on Windows defaults to `cmd.exe`** — bash-style `\` continuations break. Always set `interpreter = ["bash", "-c"]`.
2. **Azure Container App custom domains can't use `azapi_update_resource`** — it does a full PUT requiring all secrets. Azure CLI commands are the only viable automation path.
3. **Managed certificate provisioning needs polling** — cert goes from Pending to Succeeded in ~60 seconds; attempting to bind before that fails.
4. **Always use `RandomNumberGenerator.GetBytes(N)` for encryption keys** — never hand-type a base64 string; length is easy to get wrong.
5. **Azure Static Web Apps need `staticwebapp.config.json`** for SPA routing — without `navigationFallback`, refreshing on a client route returns 404.

---

## Checkpoint 5: Advanced & Delight

**Date: February 16, 2026**

### 5.1 Mobile Responsiveness Overhaul

A thorough mobile audit (iPhone X viewport, 375×812) revealed several usability issues. All fixes follow a mobile-first pattern using Tailwind's `sm:` breakpoint.

**Bottom-anchored task input**: On mobile, the QuickAddForm moves from the top of the page to a fixed bottom bar, matching native mobile app conventions. Uses `env(safe-area-inset-bottom)` with `max()` for proper spacing on notched devices (iPhone X+). The `viewport-fit=cover` meta tag enables safe area CSS variables.

**Native touch scrolling for kanban**: Radix UI's `ScrollArea` component intercepted native touch scroll events, preventing horizontal swipe between columns. Replaced with native `overflow-x-auto` + CSS `snap-x snap-mandatory` for natural touch scrolling. Each column has `min-w-[85vw] snap-center` for a one-column-at-a-time swipe experience.

**Admin table → card layout**: Admin tables (UserManagement, AuditLog) were unusable on mobile — columns truncated to unreadable widths. Created `UserCard` and `AuditLogCard` components that render on mobile (`sm:hidden` / `hidden sm:block` pattern). Cards show the same data in a stacked layout with role badges and action menus.

**Touch targets**: All interactive header elements (LanguageSwitcher, ThemeToggle, UserMenu, AdminLink) enlarged to minimum 44×44px on mobile via `size-9 sm:size-8` pattern. Icons scaled proportionally.

**Toolbar overflow**: ListViewToolbar "Split done" button text hidden on mobile (icon-only). FilterBar inputs use `basis-full sm:basis-auto` for responsive wrapping.

### 5.2 Lemon.DO Branding

Applied brand assets (cartoon lemon icon + "Lemon.DO" logo) across the entire frontend.

**Typography**: The logo uses Nunito (Black weight, 900) — identified by its rounded terminals and thick strokes. Added via Google Fonts with `--font-brand` CSS custom property. Brand text uses `font-[var(--font-brand)]` in Tailwind.

**Icon placement**: Lemon mascot icon appears in:
- Dashboard header: `size-7 sm:size-8` alongside brand text
- Admin header: same as Dashboard
- Auth pages (Login/Register): `size-24` centered above brand text

**Brand text rendering**: "Lemon." in `text-foreground` + "DO" in `text-lemon` across all layouts. i18n keys updated: `brand.lemon` = `"Lemon."`, `brand.do` = `"DO"`.

**Favicons & manifest**: Full set of icons generated from the lemon mascot — ICO, PNG 16/32, Apple Touch Icon 180, Android Chrome 192/512. Web manifest updated with `"Lemon.DO"` name and black theme color.

### 5.3 Other CP5 Features (Prior Commits)

**PWA (CP5.1)**: Service worker for offline caching, web manifest for installability, install prompt, and update notification toast.

**Spanish language (CP5.7)**: Third language option (`es.json`) with full translation coverage, added to LanguageSwitcher.

**Password strength meter (CP5.11)**: Delightful registration UX with animated strength feedback. Pure `evaluatePasswordStrength()` function mirrors backend ASP.NET Identity rules exactly: min 8 chars, uppercase, lowercase, digit (all required), plus special character and 12+ chars as bonus. Five strength levels (Too weak → Very strong) displayed with a color-coded progress bar transitioning from destructive red through orange/amber/green to the brand lime. Each requirement is a checklist item with a small dot that becomes an animated checkmark SVG (reusing existing `draw-check` keyframe) when satisfied. Requirements split into "Requirements" (4 mandatory) and "Bonus" (2 optional) sections. Submit button disables until all required checks pass, preventing frustrating server-side 400 errors. Show/hide password toggle with eye icon. Accessible via `aria-live="polite"` + `role="status"`. No external library — the pure function is ~30 lines vs 400KB+ for zxcvbn. 24 new tests: unit tests for each check, level classification, 4 fast-check property tests (score = passed count, score in [0,6], valid level, monotonic prefix score), and component rendering tests.

**Analytics infrastructure (CP5.4)**: Port/adapter pattern with `IAnalyticsService` and domain event handlers for privacy-first event tracking (task created/completed, user registered/logged in).

### 5.4 Key Decisions

| Decision | Rationale |
|----------|-----------|
| Native overflow over Radix ScrollArea | ScrollArea intercepts touch events; native overflow works with CSS snap for smooth column-by-column swiping |
| `env(safe-area-inset-bottom)` for bottom bar | Handles iPhone notch/home indicator without hardcoded pixel offsets |
| `hidden sm:block` / `sm:hidden` for responsive tables | Clean swap between table and card layouts at the `sm` breakpoint; avoids complex CSS-only responsive tables |
| Nunito for brand font | Matches logo letterforms (rounded terminals, thick strokes); loaded from Google Fonts at weights 700/800/900 |
| `--font-brand` CSS variable | Single source of truth for brand font; Tailwind uses `font-[var(--font-brand)]` syntax |
| Client-side password strength over backend-only | Instant feedback as user types; backend remains the authority; frontend is advisory |
| No zxcvbn | 30-line pure function vs 400KB+ library; backend doesn't use dictionary checks so frontend shouldn't either |
| Disable submit until requirements met | Prevents frustrating 400 errors; enables as soon as 4 required checks pass; bonus checks improve score only |

### 5.5 Initial Verification

| Check | Result |
|---|---|
| **Backend Tests** | 254 passed, 0 failed |
| **Frontend Tests** | 278 passed, 0 failed (24 new for password strength) |
| **Frontend Build** | Clean |
| **Frontend Lint** | Clean |

---

### 5.6 Onboarding Flow (CP5.3)

**Two commits**: backend migration + frontend tooltip tour.

**Backend**: Added `OnboardingCompletedAt` (nullable `DateTimeOffset?`) to the domain `User` entity with `CompleteOnboarding()` method (sets timestamp, raises `OnboardingCompletedEvent`, idempotent). Dual-provider migration (`AddOnboardingCompletedAt`) with data migration that sets existing users' `OnboardingCompletedAt` to `CURRENT_TIMESTAMP` so they skip the tour. API endpoints: `GET /api/onboarding/status` and `POST /api/onboarding/complete`.

**Frontend**: 3-step tooltip tour with auto-advance:
1. "Create your first task" — highlights QuickAddForm
2. "Complete it by clicking the checkbox" — auto-advances via MutationObserver watching for `data-onboarding="task-card"` attribute
3. "Explore your board!" — celebration animation on completion

The `OnboardingTooltip` positions itself relative to target elements via `getBoundingClientRect()` + `position: fixed`. `CelebrationAnimation` shows a checkmark burst for 2 seconds. Skip button calls the complete API immediately.

### 5.7 Notification System (CP5.5)

**Three commits**: entity + migration, backend infrastructure, frontend UI.

**Backend**: New Notifications bounded context with `Notification` entity (Id, UserId, Type, Title, Body, IsRead, CreatedAt, ReadAt). `NotificationType` enum: DueDateReminder, TaskOverdue, Welcome. `INotificationRepository` with user-scoped queries. `PushSubscriptionEntity` for Web Push (VAPID). `DueDateReminderService` (BackgroundService) runs every 6 hours, checks tasks due within 24h, creates notifications + sends push. `WelcomeNotificationOnUserRegistered` event handler creates a welcome notification on registration.

**CQRS handlers**: ListNotificationsQuery (paginated), GetUnreadCountQuery, MarkNotificationReadCommand, MarkAllNotificationsReadCommand, SubscribePushCommand, UnsubscribePushCommand.

**API endpoints**: `GET /api/notifications`, `GET /api/notifications/unread-count`, `POST /api/notifications/{id}/read`, `POST /api/notifications/read-all`, `POST /api/push/subscribe`, `DELETE /api/push/subscribe`, `GET /api/push/vapid-key`.

**Frontend**: NotificationBell (bell icon + unread count badge, 30s polling), NotificationDropdown (list with mark-read, mark-all-read, empty state), NotificationItem (icon by type, relative time display). Push subscription via `serviceWorkerRegistration.pushManager.subscribe()` with VAPID key.

### 5.8 Offline Support (CP5.2 + CP5.8)

**Three commits**: read support, mutation queue, hook wiring.

**Offline Read (CP5.2)**: Enhanced Workbox runtime caching in vite.config.ts — NetworkFirst for `/api/tasks*` and `/api/boards/*` (cache "api-data", maxEntries 100, maxAge 24h), excluding auth/analytics/push endpoints. TanStack Query set to `networkMode: 'offlineFirst'` with extended `gcTime`. AuthHydrationProvider checks `navigator.onLine` before silent refresh. OfflineBanner shows differentiated messages: "Viewing cached data" when offline with cache vs "You are offline" without.

**Offline Mutations (CP5.8)**: IndexedDB-backed FIFO queue (`offline-queue.ts`) with schema `{ id, timestamp, method, url, body, status }`. `enqueue()`, `drain()`, `clear()`, `getPendingCount()` operations. Zustand store (`use-offline-queue-store.ts`) tracks pendingCount and isSyncing state. On `online` event: triggers silent refresh first (token may be expired), then drains queue in FIFO order. 409 conflicts: toast notification, mutation discarded, TanStack Query caches invalidated. SyncIndicator component shows "X changes pending" → "Syncing..." → "All synced".

### 5.9 Description Auto-Save Bug Fix

**Commit**: `fix(tasks): add debounced auto-save for task description with flush on unmount`

Users reported that typing in the task description and quickly closing the sheet lost changes. Root cause: description changes required clicking away (blur event) to save. Fast exits bypassed the blur handler entirely.

**Fix**: Added debounced auto-save (1s delay) using `useRef` to track the draft, with `useEffect` cleanup that flushes any pending save on unmount. A save indicator shows "Saving..." → "Saved" → fades to transparent. The `lastSavedDescRef` prevents re-saving when the server response matches the draft.

### 5.10 Additional UX Enhancements

**Dev Account Password Auto-Fill**: In development mode, the ProtectedDataRevealDialog pre-fills the password field with the dev account's password, making demo workflows frictionless.

**Self-Reveal for User's Own Profile**: Users can now see their own redacted email and display name without SystemAdmin intervention. The reveal dialog checks if the target user matches the current user and uses password re-authentication (not admin break-the-glass).

**"How I Built This" Story Page**: An interactive engineering narrative at `/story` that walks through the project's development journey, architecture decisions, and technical challenges.

**Custom Domains**: Terraform configuration for `api.lemondo.btas.dev` (API Container App) and `lemondo.btas.dev` (frontend Static Web App) with managed certificates via Azure and Google Cloud DNS.

### 5.11 E2E Tests & Visual Regression (CP5.6 + CP5.6b)

**Updated `playwright.config.ts`**: Added Firefox and WebKit projects (conditionally excluded in CI via `process.env.CI`). Added device emulation: iPhone 14, iPad Mini, Pixel 7 (local only). Configured `toHaveScreenshot` with `maxDiffPixelRatio: 0.01`.

**New E2E specs** (41 tests total):
- `language.spec.ts` (5): Default English, switch to Spanish (verify "Titulo de la nueva tarea"), persist across nav, Portuguese, back to English
- `onboarding.spec.ts` (7): New user sees step 1, creating task auto-advances to step 2, completing task advances to step 3, Finish triggers celebration, tour gone on reload, skip button
- `notifications.spec.ts` (9): Welcome notification exists, unread count, bell badge, dropdown open, mark-all-read, empty state, API mark-read tests
- `offline-sync.spec.ts` (6): No banner when connected, banner on offline, banner disappears on reconnect, cached data visible offline
- `pwa.spec.ts` (4): Manifest link, theme-color meta, valid manifest JSON, SW registration
- `visual-regression.spec.ts` (10): Board/list/auth views in light + dark themes at 1280x720 with `reducedMotion: 'reduce'`

**New helpers**: `notification.helpers.ts` (list, unread count, mark read), `onboarding.helpers.ts` (status, complete).

### 5.12 Lint & Type Fixes

Several React 19 and ESLint strictness issues surfaced during verification:

- **react-hooks/refs**: React 19 requires ref `.current` assignments inside `useEffect`, not during render. Fixed in TaskDetailSheet.tsx by wrapping each `ref.current = value` in `useEffect(() => { ref.current = value; }, [value])`.
- **react-hooks/set-state-in-effect**: Direct `setState` in effect body violates React 19 rules. Fixed with `queueMicrotask()` in OnboardingTour.tsx and CelebrationAnimation.tsx.
- **React 19 useRef types**: `useRef<T>()` without argument requires explicit `undefined` initial value. Fixed `useRef<ReturnType<typeof setTimeout> | undefined>(undefined)`.
- **react-refresh/only-export-components**: Files exporting both components and non-component values need eslint-disable. Applied to DevAccountSwitcher and PasswordStrengthMeter.

### 5.13 Final Verification

| Check | Result |
|---|---|
| **Backend Build** | 11/11 projects, 0 warnings, 0 errors (3.05s) |
| **Frontend Build** | 3406 modules (3.90s) |
| **Backend Tests** | 375 passed, 0 failed (6.74s) |
| **Frontend Tests** | 337 passed, 0 failed (8.65s) |
| **Frontend Lint** | Clean, no issues |
| **E2E Tests** | 96 total (55 existing + 41 new) |
| **Total Tests** | **808** (375 backend + 337 frontend + 96 E2E) |

### 5.14 CP5 Lessons Learned

1. **React 19's stricter ref rules catch real bugs.** Moving ref assignments to `useEffect` prevents subtle issues where stale closures capture outdated ref values during render. The lint rules are protective, not pedantic.

2. **queueMicrotask is the escape hatch for setState in effects.** When you need to immediately update state based on a synchronous DOM check in an effect, `queueMicrotask(() => setState(...))` defers the update past React's effect phase without introducing a visual delay.

3. **MutationObserver is a clean alternative to prop drilling for onboarding.** Instead of threading an `onTaskCreated` callback through 5 levels of components, the onboarding tour watches for DOM changes on elements with `data-onboarding` attributes. This decouples the tour from the task creation flow entirely.

4. **IndexedDB for offline queues is worth the complexity.** localStorage has a 5MB limit and synchronous API. IndexedDB supports structured data, cursors for FIFO ordering, and much larger storage — critical for an offline mutation queue that may accumulate many changes before reconnecting.

5. **409 conflict handling with toast + discard is pragmatic.** Full conflict resolution (3-way merge, user choice UI) adds significant complexity for a rare scenario. Toast notification + cache invalidation keeps the user informed and refetches server truth.

6. **Debounced auto-save with flush-on-unmount is the right pattern for text fields.** Pure blur-to-save misses fast exits (close button, navigation). The combination of debounced saves during editing + cleanup flush on unmount covers all exit paths.

---

### CP5 Commits Summary

| Hash | Message |
|------|---------|
| 67b731d | feat(infra): add custom domain support for api.lemondo.btas.dev and lemondo.btas.dev |
| 548c97e | feat(client): improve mobile responsiveness and apply Lemon.DO branding |
| 19c3d0c | feat(i18n): add Spanish (es) language support |
| 88ffe53 | feat(pwa): add service worker, manifest, install and update prompts |
| dd16176 | feat(auth): add password strength meter to registration form |
| e69dd3c | feat(analytics): add backend analytics port/adapter with domain event handlers |
| 2a962c9 | feat(analytics): add frontend event tracking with batching |
| 847ab45 | docs: update changelog, journal, and tasks for CP5 mobile and branding changes |
| 1758924 | feat(onboarding): add OnboardingCompletedAt to User entity with dual-provider migration |
| 4fd7733 | feat(client): add landing page with scroll animations and route restructure |
| c8ca5ff | feat(onboarding): add tooltip tour with celebration animation |
| 09202c0 | feat(notifications): add Notification entity, repository, endpoints, and due date reminders |
| 3d7654a | feat(notifications): add notification bell, dropdown, and push subscription frontend |
| 7c4e197 | feat(offline): add offline read support with Workbox caching and offlineFirst query mode |
| 83aef47 | fix(tasks): add debounced auto-save for task description with flush on unmount |
| 629798c | feat(auth): add dev-only password auto-fill for demo accounts in reveal dialogs |
| f20170a | feat(auth): add self-reveal for user's own redacted profile data |
| cb89dbe | feat(client): add "How I Built This" story page at /story |
| d721908 | feat(offline): add IndexedDB mutation queue with FIFO drain and conflict handling |
| 4a46301 | fix(lint): resolve all ESLint errors across CP5 files |
| a96fadf | test(e2e): add CP5 E2E specs, multi-browser config, and visual regression |
| 94b6463 | docs(roadmap): add frontend bundle optimization to Tier 9 |

---

## Release: v1.0.0 — First Stable Release

**Date: February 16, 2026**

### What Shipped

All 5 checkpoints complete. This is the first stable release, promoted from the originally planned v0.5.0 to v1.0.0 to signal production readiness.

- **CP1**: Core Task Management — DDD domain, kanban board, list view
- **CP2**: Auth & Authorization — JWT + cookie refresh, user-scoped data
- **CP3**: Rich UX — dark mode, filters, task detail sheet, loading skeletons, error boundaries
- **CP4**: Production Hardening — Serilog, audit trail, admin panel, AES-256-GCM encryption, i18n (en + pt-BR), Azure deployment, CI/CD
- **CP5**: Advanced & Delight — PWA, offline (read + mutation queue), onboarding tour, notifications (in-app + Web Push), analytics, Spanish i18n, password strength meter, landing page, mobile responsiveness, Lemon.DO branding, multi-browser E2E, visual regression

### Final Numbers

- **808 tests**: 375 backend + 337 frontend + 96 E2E
- **5 bounded contexts**: Tasks, Boards, Identity, Administration, Notifications
- **3 languages**: English, Portuguese (Brazil), Spanish
- **Cloud**: Azure Container Apps + Static Web App, custom domains with managed TLS
- **Infrastructure**: Terraform (10 modules, 3 stages), GitHub Actions CI/CD, Docker

---

## What's Next

See `docs/ROADMAP.md` for future capability tiers.
