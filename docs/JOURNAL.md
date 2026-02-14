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

## What's Next

### Checkpoint 1: Core Task Management

*Next up: Full-stack task CRUD in single-user mode. Backend: TaskItem/Board/Column entities with TDD (MSTest + FsCheck), EF Core + SQLite, minimal API endpoints. Frontend: Design System setup, Domain Components (Atoms/Widgets/Views), TanStack Query hooks, Zustand stores, React Router. Integration tests for all endpoints. Working kanban board and list view.*

### Checkpoint 2: Authentication & Authorization

*Planned: ASP.NET Core Identity with JWT tokens, register/login/logout endpoints, user-scoped task queries, React auth pages with route guards and redirects, token refresh handling.*

### Checkpoint 3: Rich UX & Polish

*Planned: Kanban drag-and-drop, quick-add, task detail editing, filters/search, dark/light theme, responsive design, loading skeletons, empty states, toast notifications, error boundaries.*

### Checkpoint 4: Production Hardening

*Planned: OpenTelemetry + Serilog observability, PII redaction in admin views, audit trail, admin panel, SystemAdmin role, i18n (en + pt-BR), rate limiting, data encryption at rest.*

### Checkpoint 5: Advanced & Delight

*Planned: PWA with offline support, onboarding flow, analytics event tracking, in-app notifications, Playwright E2E tests, Spanish language, offline mutation queue.*
