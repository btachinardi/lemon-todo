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
- **3-Layer Frontend**: L1 (Data/Routing) -> L2 (Domain UI) -> L3 (Design System). L2 cannot use native HTML tags. L3 cannot reference domain types.
- **Gitflow**: main + develop + feature branches. Conventional commits. Atomic commits.
- **Security**: PII redaction in logs, OWASP Top 10 compliance, rate limiting.
- **Accessibility**: WCAG 2.1 AA minimum, Radix primitives for built-in a11y.

### Interlude: The State Management Gap

During our checkpoint review of GUIDELINES.md, we realized we had a significant blind spot: **no explicit state management strategy for the frontend**. Our original 3-layer component architecture (L1/L2/L3) described what components render, but not how they get their data.

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

This gave us a **4-layer frontend architecture**:
```
L1 (Routes) -> State Layer (Zustand + TanStack Query) -> L2 (Domain UI) -> L3 (Design System)
```

The key rule: **TanStack Query owns all server data, Zustand owns all client state, React Context is only for low-frequency cross-cutting providers.** Components never mix `fetch` calls with rendering.

### Phase 3: Codebase Bootstrap

*Coming next: We'll initialize the .NET Aspire solution, scaffold the React frontend, wire up the test infrastructure, and get health checks passing.*

### Phase 4: Auth Domain (TDD)

*Planned: TDD implementation of the Identity bounded context - API and Frontend together.*

### Phase 5: Tasks Domain (TDD)

*Planned: TDD implementation of the Task Management bounded context - Kanban and list views.*

### Phase 6: Cross-Cutting Concerns

*Planned: Onboarding, admin panel, HIPAA compliance, analytics, i18n, PWA, themes, observability.*

### Phase 7: Full E2E Validation

*Planned: Playwright tests covering every scenario from SCENARIOS.md across the entire stack.*

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

## Contributing

1. Read [GUIDELINES.md](./GUIDELINES.md) first
2. Create a feature branch from `develop`: `git checkout -b feature/your-feature`
3. Follow TDD: write failing test first
4. Use conventional commits
5. Open a PR to `develop`

---

## License

[MIT](./LICENSE)
