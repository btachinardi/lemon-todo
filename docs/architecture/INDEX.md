# Architecture

> Technical architecture, patterns, conventions, and decision records for LemonDo.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [backend.md](./backend.md) | DDD layers, project structure, naming conventions, error handling, security, git workflow | Active |
| [frontend.md](./frontend.md) | Architecture Tiers, Component Taxonomy, state management, import rules, i18n, accessibility | Active |
| [testing.md](./testing.md) | TDD methodology, testing pyramid, coverage targets | Active |
| [security.md](./security.md) | Security guidelines and authentication/security trade-off analysis | Active |
| [infrastructure.md](./infrastructure.md) | Technology choices, deployment, offline/PWA, scalability considerations | Active |
| [decisions/](./decisions/) | Architecture decision log and trade-off tables | — |

---

## Summary

LemonDo is built on Domain-Driven Design with a strict separation between the backend .NET solution and the React frontend. Both stacks follow the same organizing principle: keep business logic in the domain layer, keep the presentation layer thin, and make infrastructure replaceable.

**Backend** uses a four-layer DDD structure (Domain, Application, Infrastructure, API) with ASP.NET Core Minimal APIs. The domain layer is pure — no framework dependencies — and returns `Result<T, DomainError>` rather than throwing exceptions for business logic failures. The Application layer houses use cases as command/query handlers. The Infrastructure layer owns EF Core, SQLite (swappable to SQL Server via a config key), and all external service adapters.

**Frontend** is organized along two orthogonal dimensions. Architecture Tiers (Routing → Pages/Layouts → State Management → Components) define data flow and separation of concerns. Component Taxonomy (Domain Views → Domain Widgets → Domain Atoms → Design System) defines visual composition granularity and domain awareness. State is split cleanly: TanStack Query owns all server data; Zustand owns all client-only state; React Context is reserved for low-frequency cross-cutting providers only.

**Testing** follows TDD with a RED-GREEN-VALIDATE cycle. The testing pyramid runs property tests and unit tests at the base, integration tests at mid level, and Playwright E2E at the top. Coverage targets are 90% for domain entities, 80% for use cases and frontend components, 100% for API integration paths.

**Security** uses a three-form protected data strategy: redacted strings for display/logs, SHA-256 hashes for O(1) exact-match lookup, AES-256-GCM encrypted values as the source of truth. Auth uses HttpOnly cookie refresh tokens with memory-only access tokens, eliminating XSS risk and making CSRF tokens unnecessary.

**Infrastructure** decisions are documented in full with alternatives considered and rationale. The most impactful: SQLite with repository pattern abstraction, Azure Container Apps over App Service (VM quota constraints), Docker + GitHub Actions CI/CD, and Terraform for staged infrastructure-as-code deployment.
