<p align="center">
  <img src="src/client/public/lemondo-logo.png" alt="Lemon.DO" width="400" />
</p>

<p align="center">
  A task management platform that combines consumer-grade UX with enterprise-grade compliance.
</p>

<p align="center">
  <a href="https://github.com/btachinardi/lemon-todo/actions/workflows/deploy.yml"><img src="https://github.com/btachinardi/lemon-todo/actions/workflows/deploy.yml/badge.svg?branch=main" alt="CI/CD" /></a>
  <a href="https://github.com/btachinardi/lemon-todo/releases/tag/v0.4.0"><img src="https://img.shields.io/badge/version-0.4.0-brightgreen" alt="Version" /></a>
  <img src="https://img.shields.io/badge/tests-808%20passing-brightgreen" alt="Tests" />
  <a href="./LICENSE"><img src="https://img.shields.io/badge/License-MIT-green.svg" alt="License: MIT" /></a>
</p>

<p align="center">
  <a href="https://lemondo.btas.dev"><img src="https://img.shields.io/badge/web-lemondo.btas.dev-blue" alt="Web App" /></a>
  <a href="https://api.lemondo.btas.dev/scalar/v1"><img src="https://img.shields.io/badge/api-api.lemondo.btas.dev-blue" alt="API" /></a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/.NET-10.0-purple" alt=".NET" />
  <img src="https://img.shields.io/badge/React-19-61DAFB" alt="React" />
  <img src="https://img.shields.io/badge/Aspire-13-orange" alt="Aspire" />
  <img src="https://img.shields.io/badge/TypeScript-5.9-3178C6" alt="TypeScript" />
  <img src="https://img.shields.io/badge/Tailwind-4-06B6D4" alt="Tailwind" />
  <img src="https://img.shields.io/badge/Playwright-E2E-2EAD33" alt="Playwright" />
</p>

## What is LemonDo?

LemonDo is a full-stack task management application built with .NET Aspire and React. It features a Kanban board with drag-and-drop, list view with time grouping, multi-user auth, admin panel, audit trail, field-level encryption, dark mode, i18n, PWA with offline support, and onboarding — all following DDD architecture with two bounded contexts.

### Highlights

- **Kanban + List views** with drag-and-drop, filters, search, and task detail sheet
- **Auth & RBAC** — JWT + HttpOnly cookie refresh, 3-tier role hierarchy (User/Admin/SystemAdmin)
- **Admin panel** — user management, audit log, protected data redaction with break-the-glass reveal
- **AES-256-GCM encryption** for emails, display names, and sensitive task notes at rest
- **PWA + offline** — service worker caching, offline reads, mutation queue with sync-on-reconnect
- **Dark/light theme**, **i18n** (English, Portuguese, Spanish), and **onboarding** flow
- **Notifications** — in-app + Web Push with due date reminders
- **808 tests** — unit, property (FsCheck/fast-check), integration, component, and E2E (Playwright)
- **CI/CD** — GitHub Actions with 4-job test matrix, Docker build, Azure deploy on push to main

## Checkpoints

| CP | Name | Version | Key Deliverables |
|----|------|---------|------------------|
| 1 | Core Task Management | [v0.1.0](https://github.com/btachinardi/lemon-todo/releases/tag/v0.1.0) | DDD domain (Task + Board contexts), 18 API endpoints, Kanban board, list view |
| 2 | Auth & Authorization | [v0.2.0](https://github.com/btachinardi/lemon-todo/releases/tag/v0.2.0) | ASP.NET Identity + JWT, cookie-based refresh, user-scoped data, login/register UI |
| 3 | Rich UX & Polish | [v0.3.0](https://github.com/btachinardi/lemon-todo/releases/tag/v0.3.0) | Dark mode, filter bar, task detail sheet, skeletons, empty states, toasts, 55 E2E tests |
| 4 | Production Hardening | [v0.4.0](https://github.com/btachinardi/lemon-todo/releases/tag/v0.4.0) | Serilog, audit trail, admin panel, AES encryption, i18n, dual DB, Azure infra, CI/CD |
| 5 | Advanced & Delight | Unreleased | PWA + offline mutations, onboarding, notifications, analytics, landing page, Spanish i18n |

## Getting Started

```bash
git clone https://github.com/btachinardi/lemon-todo.git
cd lemon-todo
cd src/client && pnpm install && cd ../..
./dev start
```

See the **[Development Guide](./docs/DEVELOPMENT.md)** for CLI reference, test accounts, SQL Server setup, migrations, and troubleshooting.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 10, ASP.NET Core, EF Core 10 |
| Orchestration | Aspire 13 |
| Database | SQLite (dev) / Azure SQL (prod) |
| Frontend | React 19, TypeScript 5.9, Vite 7 |
| UI | Shadcn/ui, Radix, Tailwind CSS 4 |
| State | TanStack Query 5 (server) + Zustand 5 (client) |
| Drag & Drop | @dnd-kit |
| Backend Tests | MSTest 4 + FsCheck 3 |
| Frontend Tests | Vitest + fast-check |
| E2E Tests | Playwright 1.58 (Chromium, Firefox, WebKit) |
| Observability | OpenTelemetry + Serilog (Aspire Dashboard) |
| Infrastructure | Terraform + Azure (Container App, Static Web App, SQL, Key Vault) |

## Project Structure

```
lemon-todo/
├── .github/workflows/         # CI/CD pipeline
├── docs/                      # Design & operational docs
├── infra/                     # Terraform (3 stages, 10 modules)
├── src/
│   ├── LemonDo.slnx           # .NET solution (11 projects)
│   ├── LemonDo.AppHost/       # Aspire orchestrator
│   ├── LemonDo.Api/           # ASP.NET Core minimal API
│   ├── LemonDo.Application/   # Commands + Queries (CQRS)
│   ├── LemonDo.Domain/        # Pure domain (entities, VOs, events)
│   ├── LemonDo.Infrastructure/ # EF Core, Identity, encryption
│   ├── LemonDo.Migrations.*/  # Dual-provider migrations
│   └── client/                # React frontend (Vite + Tailwind)
├── tests/
│   ├── LemonDo.*.Tests/       # Backend test projects (3)
│   └── e2e/                   # Playwright E2E tests
├── CHANGELOG.md               # Release history
└── GUIDELINES.md              # Development standards
```

## Architecture

**Backend** — strict DDD layer dependency:

```
Domain ← Application ← Infrastructure ← Api ← AppHost
```

**Two bounded contexts**: Task (upstream, owns lifecycle) and Board (downstream conformist, owns spatial placement). Cross-context coordination happens at the Application layer.

See [DOMAIN.md](./docs/DOMAIN.md) for the full domain model, and [GUIDELINES.md](./GUIDELINES.md) for TDD methodology and code quality standards.

## Documentation

| Document | Description |
|----------|-------------|
| [Development Guide](./docs/DEVELOPMENT.md) | CLI, test accounts, SQL Server, migrations, troubleshooting |
| [Deployment Guide](./docs/DEPLOYMENT.md) | CI/CD, Azure infrastructure, Docker, production URLs |
| [Domain Model](./docs/DOMAIN.md) | Bounded contexts, entities, value objects, events |
| [Product Requirements](./docs/PRD.md) | Official PRD with user stories |
| [User Scenarios](./docs/SCENARIOS.md) | Personas and storyboards |
| [Development Journal](./docs/JOURNAL.md) | Decisions, phases, and lessons learned |
| [Technology Research](./docs/RESEARCH.md) | Stack versions and compatibility notes |
| [Trade-offs](./docs/TRADEOFFS.md) | Assumptions and architectural trade-offs |
| [Roadmap](./docs/ROADMAP.md) | Future capability tiers (AI, integrations, collaboration) |
| [Release Process](./docs/RELEASING.md) | Gitflow release steps |
| [Changelog](./CHANGELOG.md) | Release history (Keep a Changelog) |
| [Guidelines](./GUIDELINES.md) | TDD, DDD, code quality, and git workflow |

## Contributing

1. Read [GUIDELINES.md](./GUIDELINES.md)
2. Create a feature branch from `develop`
3. Follow TDD (write failing test first)
4. Use [Conventional Commits](https://www.conventionalcommits.org/)
5. Open a PR to `develop`

## License

[MIT](./LICENSE)
