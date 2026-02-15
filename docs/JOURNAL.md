# LemonDo - Development Journal

> **Date**: 2026-02-13
> **Status**: Active
> **Purpose**: Captures our complete thought process, from inception to production - every decision, phase, and lesson learned.

---

## The Starting Point

**Date: February 13, 2026**

We started with an empty folder and a clear vision: build a production-grade task management platform that proves you can have great UX AND compliance. Not one or the other.

Our constraints:
- .NET Aspire for cloud-native orchestration
- React + Shadcn/ui for a premium frontend
- Strict TDD methodology
- DDD architecture throughout
- HIPAA-level data protection

## Phase 1: Planning Before Code

We believe planning is not wasted time. It's compressed debugging. We created five foundational documents before writing a single line of code:

### 1.1 Product Requirements Document (PRD)

Our first document ([PRD.draft.md](./PRD.draft.md)) captured everything we knew we needed:
- 10 functional requirement groups (FR-001 through FR-010)
- 10 non-functional requirement groups (NFR-001 through NFR-010)
- Success metrics with concrete targets
- Risk assessment with mitigations
- Clear "out of scope" boundaries to prevent scope creep

**Decision**: We chose Scalar over Swagger for API documentation. Starting with .NET 9, Scalar became the modern default. It loads faster, has better search, and its dark mode matches our premium UI goals.

**Decision**: SQLite for MVP. Some might call this controversial for a "HIPAA-compliant" app. Our reasoning: SQLite is more than capable for our MVP scale, the repository pattern makes swapping to PostgreSQL a one-file change, and it eliminates infrastructure complexity during development.

### 1.2 Technology Research

We researched every technology we'd use ([RESEARCH.md](./RESEARCH.md)), verifying:
- Latest stable versions (not bleeding edge, not outdated)
- Compatibility between all pieces of the stack
- Features relevant to our requirements

Key findings:
- **.NET 10** is the current LTS (3-year support). We're using 10.0.103.
- **Aspire 13** dropped the ".NET" prefix and added `AddJavaScriptApp` which auto-generates Dockerfiles for our React frontend.
- **Vite 7** is the latest major version (7.3.1). Vite 6 is now in maintenance.
- **React 19.2** brought the React Compiler for automatic memoization.
- **Shadcn/ui** added component styles (Vega, Nova, etc.) and Base UI support in February 2026.

### 1.3 User Scenarios

This is where our planning leveled up. Instead of jumping to domain design, we wrote detailed storyboards ([SCENARIOS.md](./SCENARIOS.md)) from the USER's perspective:

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

### 1.4 Revised PRD

After scenarios, we created [PRD.md](./PRD.md) - our official requirements document, incorporating everything we learned. Key changes:
- Quick-add promoted to P0
- Onboarding celebrations upgraded from P1 to P0
- Offline CRUD (not just viewing) became a requirement
- PII default-redacted in admin views (not opt-in redaction, but opt-in reveal)
- New NFR section for micro-interactions and UX polish

We kept the original PRD intact to show our evolution.

### 1.5 Domain Design

With requirements solid, we designed our domain ([DOMAIN.md](./DOMAIN.md)):

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

Before touching code, we established our rules of engagement ([../GUIDELINES.md](../GUIDELINES.md)):

- **Strict TDD**: RED-GREEN-VALIDATE. No production code without a failing test.
- **Frontend Architecture**: Two orthogonal systems - Architecture Tiers (Routing -> Pages & Layouts -> State Management -> Components) for separation of concerns, and Component Taxonomy (Design System -> Domain Atoms -> Domain Widgets -> Domain Views) for composition granularity.
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

After running review agents against our documentation, we faced a hard truth: we were planning to build everything at once. Six bounded contexts, HIPAA compliance, three languages, offline CRUD, analytics - all marked as P0. The reviewers flagged this as over-engineering, and they were right.

But the ambition isn't wrong - the delivery order is. We restructured into **checkpoint-based delivery**: five incremental checkpoints where each one produces a complete, runnable application. If we stop at any checkpoint, we have something presentable that demonstrates real architecture and thought process.

**The key insight**: Checkpoint 1 addresses *every* core evaluation criteria (API design, data structures, component design, F/E↔B/E communication, clean code). Later checkpoints layer on production features progressively.

| Checkpoint | Focus | Key Trade-off |
|---|---|---|
| **CP1: Core Tasks** | Working full-stack CRUD | Single-user mode (no auth yet) |
| **CP2: Auth** | User accounts, JWT, RBAC | Two roles (User, Admin), not three |
| **CP3: Rich UX** | Drag-drop, theme, quick-add, polish | Theme before i18n |
| **CP4: Production** | Observability, PII, audit, i18n | "HIPAA-Ready" not certified |
| **CP5: Advanced** | PWA, onboarding, analytics, E2E | Lightweight implementations proving architecture |

**Decision**: Tasks before Auth. A bold but deliberate choice. CP1 runs in single-user mode so we can demonstrate clean architecture end-to-end without auth complexity. The repository pattern means adding user-scoped queries in CP2 is a one-line change - proving the architecture's extensibility.

**Decision**: HIPAA downgraded from P0 to "HIPAA-Ready infrastructure." Full HIPAA compliance requires BAAs, legal review, workforce training, and incident response procedures - that's a business framework, not a codebase feature. We implement the *technical controls* (encryption, audit trails, PII redaction) and document what's needed for full certification.

See [../TASKS.md](../TASKS.md) for the complete checkpoint breakdown with every task.

---

## Phase 3: Codebase Bootstrap

**Date: February 14, 2026**

This phase brought our planning to life. We scaffolded the entire .NET Aspire solution, React frontend, test infrastructure, and wired everything together. The build is clean (9/9 projects, 0 warnings) and all smoke tests pass.

### 3.1 .NET Aspire Solution

We discovered several .NET 10 changes during bootstrapping:

- **`.slnx` format**: `dotnet new sln` now creates `.slnx` (XML-based) by default, not the legacy `.sln`. Lighter and cleaner.
- **Aspire workload deprecated**: Aspire is now distributed as NuGet packages. We install templates via `dotnet new install Aspire.ProjectTemplates` (v13.1.1).
- **`dotnet test --solution` syntax**: .NET 10 requires the `--solution` flag for solution paths; positional arguments no longer work.

The solution follows strict DDD layering: Domain ← Application ← Infrastructure ← Api ← AppHost. ServiceDefaults provides health checks (`/health`, `/alive`), OpenTelemetry, and resilience. Scalar serves API docs at `/scalar/v1`.

### 3.2 React Frontend

Vite 7 + React 19 + TypeScript 5.9 with Tailwind CSS 4 (CSS-first, `@tailwindcss/vite` plugin - no PostCSS). Shadcn/ui initialized with path aliases (`@/*` → `./src/*`). All state management libraries installed: TanStack Query 5, Zustand 5, react-i18next.

Frontend folder structure follows our Architecture Tiers + Component Taxonomy from GUIDELINES.md: `app/` (routing, pages, layouts, providers), `domains/` (feature modules), `ui/` (design system), `lib/` (utilities).

### 3.3 The MSTest Decision

**This was our most important technical decision during bootstrap.**

We initially chose xUnit v3, following community convention. But during setup, the xUnit v3 template defaulted to `net8.0` instead of auto-detecting our .NET 10 SDK. This caused immediate compatibility errors when adding project references.

Rather than force-fixing the target framework, we stopped and asked: *"If we're fighting the tooling at bootstrap, did we pick the wrong tool?"*

Research confirmed our instincts:
- **xUnit v3 has an active .NET 10 bug** (GitHub issue #3413) - "catastrophic failure" in CI environments
- **xUnit v3 templates don't auto-detect SDK version** - they hardcode `net8.0`
- **MSTest is first-party** - maintained by Microsoft, ships with every .NET SDK, guaranteed same-day compatibility

We switched to **MSTest 4.0.1 with Microsoft.Testing.Platform (MTP)**:
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

7. **WipLimit renamed to MaxTasks**. The "WIP" terminology had no place in our domain vocabulary.

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

2. **Entity renamed from BoardTask to Task**. Tasks exist independently of boards. The `BoardTask` name from the previous redesign implied tasks were owned by boards, which is no longer true. For `System.Threading.Tasks.Task` collisions, we use qualified names (`using TaskEntity = LemonDo.Domain.Tasks.Task`).

3. **Board is conformist to Task context**. The Board context imports `TaskId` and `TaskStatus` directly from the Task context. No anti-corruption layer needed -- the conformist relationship keeps things simple since we own both contexts.

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

3. **The conformist relationship keeps things simple.** Since we own both contexts and they live in the same process, Board just imports types from Task directly. No anti-corruption layer, no translation, no mapping -- just direct dependency. This is the right trade-off for a monolith.

---

## Bug Fix: Card Ordering System Redesign

**Date: February 15, 2026**

### The Problem

After completing the bounded context split, manual testing revealed two interrelated bugs in the kanban board:

1. **Position drift**: Dragging a task card to reorder within the same column sometimes placed it one position lower than expected. The issue was intermittent and worsened after multiple consecutive moves.

2. **Orphaned cards**: The GET `/api/boards/default` endpoint returned 17 card items while only 5 tasks existed on the board.

### Investigation

We traced the full execution path: frontend drag handler → API → domain `Board.MoveCard()` → EF Core persistence → query/DTO response → frontend sort.

**Root cause of position drift** (`Board.MoveCard()` in `Board.cs:210-232`): The frontend sent the visual array index (0, 1, 2...) as the position. The backend stored it verbatim on the new `TaskCard` without reindexing other cards in the column. After multiple moves, position collisions accumulated — two cards with position=1 produced an unstable sort, making cards appear in the wrong order on the next load.

Example: Column has A(pos=0), B(pos=1), C(pos=2). Drag C between A and B → frontend sends position=1 → DB state becomes A(pos=0), B(pos=1), C(pos=1). On reload, B and C are non-deterministically ordered.

**Root cause of orphaned cards** (`DeleteTaskCommandHandler` in `DeleteTaskCommand.cs:14-30` and `ArchiveTaskCommandHandler` in `ArchiveTaskCommand.cs:12-30`): Neither handler called `Board.RemoveCard()`. When tasks were soft-deleted or archived, their `TaskCard` entries remained on the board. Over time, 12 deleted tasks left orphaned cards, inflating the count from 5 to 17. The frontend filtered them out visually (cards for non-existent tasks got dropped in the render loop), but the stale data was still returned by the API.

### Solutions Considered

#### For the ordering problem

We evaluated four strategies to replace the broken dense-integer positioning:

**1. Sparse numeric ranks** — Store ranks as 1000, 2000, 3000. Insert between A and B by computing `(rankA + rankB) / 2`. Only the moved row is updated. Rebalance a local window when gaps become too small. Use DECIMAL, not float (precision drift). *Simple, well-understood, good default.*

**2. Fractional indexing / LexoRank-style strings** — Use lexicographically sortable base-36/base-62 strings. Insert between "a0" and "a8" → "a4". Mostly O(1) writes, avoids float precision entirely, better for high-frequency reorders. Occasional compaction needed. *Scales better but adds string complexity.*

**3. Linked list pointers (prev_id, next_id)** — Each card stores references to its neighbors. A move updates old neighbors + new neighbors + moved card (handful of rows, never whole-list). *Reads and pagination are more complex; harder to query efficiently for sorted lists.*

**4. CRDT sequence identifiers** — Logoot/LSEQ/RGA variants for concurrent collaborative editing. *Massive overkill for a single-user todo app.*

**Decision**: Sparse numeric ranks with `decimal`. It's the simplest strategy that eliminates the bug class, it only updates one row per move, and decimal arithmetic avoids float precision drift. Rebalancing is a rare local operation. LexoRank was tempting but adds unnecessary complexity for our scale.

#### For the API contract

The next question was how the frontend communicates the desired position to the backend. Three options:

**Option A: Frontend sends array index** (current approach) — The frontend sends `position: 2` meaning "put this at index 2 in the column." The backend must load all cards in the column, sort them by rank, find the neighbors at that index, and compute the midpoint rank. *Frontend stays simple but backend does an extra read-to-sort on every move.*

**Option B: Frontend sends the rank directly** — The frontend knows the ranks of neighboring cards (from the board DTO) and computes the midpoint itself. The backend just stores it. *Leaks the ranking system into the frontend. If we change strategies, the frontend must change too.*

**Option C: Frontend sends neighbor card IDs** — The frontend sends `previousTaskId` and `nextTaskId` — the cards directly above and below the drop target. The backend looks up those two cards' ranks (O(1) each) and computes the midpoint. Null values handle edge cases: `previous=null` means top of column; `next=null` means bottom; both null means only card. *Frontend stays dumb, backend avoids read-to-sort, API contract is explicit about intent.*

**Decision**: Option C — neighbor-based API. This is the cleanest separation of concerns:
- The frontend already knows which cards are above and below from its visual state (the `columnItems` array). Extracting neighbor IDs at the drop index is trivial.
- The backend doesn't need to load and sort all column cards — just two O(1) lookups by TaskId.
- The API contract is unambiguous: "place this card between these two cards" has exactly one correct interpretation, unlike "position 2" which is meaningless when positions are corrupted.
- If we later switch from sparse numeric to LexoRank or any other strategy, only the backend rank-computation changes. The API contract and frontend are unaffected.

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

During E2E test authoring for card ordering, we discovered that `Task.Archive()` required the task to be in `Done` status. This forced a `completeTask()` → `archiveTask()` sequence in tests, and more importantly, it was a design flaw: archiving is a visibility/organizational concept, not a lifecycle one.

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

We adopted **pre-1.0 SemVer** (`0.MINOR.PATCH`) for initial development. v0.1.0 signals "first feature-complete checkpoint" rather than production stability. The `1.0.0` release will come when the app has authentication, polish, and production hardening (likely after CP3 or CP4).

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

We deliberately did **not** create an `ICreatable<TSelf, TInput>` interface to standardize the `Create()` factory pattern. The reason: `Create()` methods are inherently non-uniform:

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

We adopted a split-token architecture that eliminates localStorage entirely:

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

## What's Next

### Checkpoint 3: Rich UX & Polish

*Planned: Kanban drag-and-drop, quick-add, task detail editing, filters/search, dark/light theme, responsive design, loading skeletons, empty states, toast notifications, error boundaries.*

### Checkpoint 4: Production Hardening

*Planned: OpenTelemetry + Serilog observability, PII redaction in admin views, audit trail, admin panel, SystemAdmin role, i18n (en + pt-BR), data encryption at rest.*

### Checkpoint 5: Advanced & Delight

*Planned: PWA with offline support, onboarding flow, analytics event tracking, in-app notifications, Playwright E2E tests, Spanish language, offline mutation queue.*
