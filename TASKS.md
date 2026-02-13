# LemonDo - Project Task Tracker

> This document tracks the complete planning, design, and development lifecycle of the LemonDo project.
> Each phase builds on the previous one, and every decision is documented for traceability.

---

## Phase 0: Project Initialization

| # | Task | Status | Notes |
|---|------|--------|-------|
| 0.1 | Create TASKS.md tracking document | DONE | This file |
| 0.2 | Initialize git repository with gitflow | PENDING | |
| 0.3 | Create initial project structure | PENDING | |

## Phase 1: Product Requirements

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.1 | Write initial PRD (docs/PRD.md) | PENDING | Full functional + non-functional requirements |
| 1.2 | Technology research (docs/RESEARCH.md) | PENDING | Latest versions, capabilities, compatibility |
| 1.3 | User scenarios and storyboards (docs/SCENARIOS.md) | PENDING | Personas, JTBD, value proposition, north star metric |
| 1.4 | Revised PRD (docs/PRD.reviewed.md) | PENDING | Polished after scenarios analysis |

## Phase 2: Domain Design

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.1 | Domain modeling (docs/DOMAIN.md) | PENDING | Bounded contexts, aggregates, entities, events |
| 2.2 | Development guidelines (GUIDELINES.md) | PENDING | TDD, clean code, architecture rules |
| 2.3 | Professional README.md | PENDING | Also serves as "how we built this" article |
| 2.4 | LICENSE file | PENDING | |

## Phase 3: Codebase Bootstrap

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.1 | Initialize .NET Aspire solution | PENDING | AppHost, ServiceDefaults, API project |
| 3.2 | Initialize Vite + React frontend | PENDING | TypeScript, Tailwind, Shadcn/ui |
| 3.3 | Configure test infrastructure | PENDING | xUnit, FsCheck, Vitest, Playwright |
| 3.4 | Configure CI/CD (GitHub Actions) | PENDING | Build, test, deploy pipelines |
| 3.5 | Configure Docker + Terraform | PENDING | Containerization + Azure IaC |
| 3.6 | Health checks and smoke tests passing | PENDING | Verify empty shell works |

## Phase 4: Auth Domain (TDD)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 4.1 | Auth domain entities + value objects | PENDING | User, Role, Permission, Email VO |
| 4.2 | Auth use cases + command/query handlers | PENDING | Register, Login, AssignRole, etc. |
| 4.3 | Auth API endpoints | PENDING | REST endpoints with OpenAPI |
| 4.4 | Auth integration tests | PENDING | Full API integration tests |
| 4.5 | Auth frontend - routing + pages | PENDING | Login, Register, Profile |
| 4.6 | Auth frontend - components + hooks | PENDING | Auth forms, guards, context |
| 4.7 | Auth frontend - Vitest specs | PENDING | Component + hook tests |
| 4.8 | Auth E2E smoke test | PENDING | Playwright basic auth flow |

## Phase 5: Tasks Domain (TDD)

| # | Task | Status | Notes |
|---|------|--------|-------|
| 5.1 | Task domain entities + value objects | PENDING | TaskItem, Board, Column, Priority |
| 5.2 | Task use cases + command/query handlers | PENDING | CRUD, move, complete, archive |
| 5.3 | Task API endpoints | PENDING | REST endpoints with OpenAPI |
| 5.4 | Task integration tests | PENDING | Full API integration tests |
| 5.5 | Task frontend - Kanban board | PENDING | Drag-and-drop Kanban view |
| 5.6 | Task frontend - List view | PENDING | Alternative list view |
| 5.7 | Task frontend - Vitest specs | PENDING | Component + hook tests |
| 5.8 | Task E2E smoke test | PENDING | Playwright basic task flow |

## Phase 6: Cross-Cutting Concerns

| # | Task | Status | Notes |
|---|------|--------|-------|
| 6.1 | Onboarding system | PENDING | Registration flow, first task guide |
| 6.2 | Admin panel + auditing | PENDING | System admin views, audit logs |
| 6.3 | HIPAA compliance layer | PENDING | Data redaction, encryption, audit |
| 6.4 | Product analytics | PENDING | Funnels, events, conversion tracking |
| 6.5 | i18n setup (frontend + backend) | PENDING | Multi-language support |
| 6.6 | PWA configuration | PENDING | Service worker, offline, install |
| 6.7 | Theme system (light/dark) | PENDING | Dual theme with system detection |
| 6.8 | Observability + instrumentation | PENDING | OpenTelemetry, structured logging |

## Phase 7: Full E2E Validation

| # | Task | Status | Notes |
|---|------|--------|-------|
| 7.1 | Playwright E2E - Auth scenarios | PENDING | All auth storyboards |
| 7.2 | Playwright E2E - Task scenarios | PENDING | All task management storyboards |
| 7.3 | Playwright E2E - Onboarding scenarios | PENDING | Complete onboarding flow |
| 7.4 | Playwright E2E - Admin scenarios | PENDING | Admin audit storyboards |
| 7.5 | Playwright E2E - Responsive + PWA | PENDING | Mobile, desktop, offline |
| 7.6 | Full test suite green | PENDING | All tests passing |

---

## Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-02-13 | .NET 10 LTS + Aspire 13 | Latest LTS with best cloud-native support |
| 2026-02-13 | Vite 7 + React 19 | Latest stable, best DX and performance |
| 2026-02-13 | Scalar over Swagger | Modern API docs UI, .NET 9+ default |
| 2026-02-13 | SQLite for MVP | Simple, zero-config, easy to swap later |
| 2026-02-13 | Terraform over Bicep | Multi-cloud portability, team familiarity |
| 2026-02-13 | Azure Container Apps | Aspire-native deployment target |

---

## Progress Summary

- **Planning**: IN PROGRESS
- **Design**: NOT STARTED
- **Implementation**: NOT STARTED
- **Testing**: NOT STARTED
- **Deployment**: NOT STARTED
