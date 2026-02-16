# LemonDo

> A task management platform that combines consumer-grade UX with enterprise-grade compliance.

[![Version](https://img.shields.io/badge/version-0.1.0-brightgreen)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)]()
[![React](https://img.shields.io/badge/React-19-blue)]()
[![Aspire](https://img.shields.io/badge/Aspire-13-orange)]()
[![Tests](https://img.shields.io/badge/tests-287%2B%20passing-brightgreen)]()

## What is LemonDo?

LemonDo is a full-stack task management platform built with .NET Aspire and React. It features a Kanban board, list view, HIPAA-Ready data handling, role-based access control, and a delightful onboarding experience. It's designed for individuals and small teams in regulated industries who need simplicity without sacrificing compliance.

### Key Features (v0.1.0)

- **Kanban Board** - Drag-and-drop cards between columns with sparse rank ordering
- **List View** - Time-based grouping (today, this week, older) with completed task splitting
- **DDD Architecture** - Two bounded contexts (Task + Board), Result pattern, domain events
- **18 API Endpoints** - Task CRUD, board management, bulk operations
- **Full Observability** - OpenTelemetry traces, metrics, correlation IDs, structured logs
- **287+ Tests** - Unit, property, integration, component, and E2E tests

### Development Checkpoints

LemonDo follows a checkpoint-based delivery model — each checkpoint produces a complete, runnable application.

| CP | Name | Status | Key Deliverables |
|----|------|--------|------------------|
| 1 | **Core Task Management** | Done (v0.1.0) | DDD domain (Task + Board contexts), 18 API endpoints, Kanban board with drag-and-drop, list view with time grouping, 287+ tests |
| 2 | **Auth & Authorization** | Done | ASP.NET Identity + JWT, 5 auth endpoints, user-scoped data, login/register UI, route guards, 368 tests |
| 3 | **Rich UX & Polish** | Not Started | Quick-add (P0), task detail editing, filters & search, dark/light theme, responsive design, loading skeletons, empty states, toasts |
| 4 | **Production Hardening** | Not Started | OpenTelemetry (frontend + backend), structured logging, PII redaction, audit trail, admin panel, i18n (en + pt-BR), rate limiting |
| 5 | **Advanced & Delight** | Not Started | PWA + offline read, onboarding flow, in-app notifications, analytics events, E2E cross-browser tests, visual regression, offline mutations |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 23+](https://nodejs.org/)
- [pnpm](https://pnpm.io/)
- [Docker](https://www.docker.com/) (optional, for containerized development)

### Quick Start

```bash
git clone https://github.com/btachinardi/lemon-todo.git
cd lemon-todo

# Install frontend dependencies
cd src/client && pnpm install && cd ../..

# Run with Aspire (orchestrates API + frontend together)
./dev start
```

The Aspire Dashboard URL (with login token) will appear in the console output. From there you can see all service URLs, logs, traces, and metrics.

### Developer CLI

All common commands are available via the `./dev` script:

```bash
./dev help                  # Show all commands

# Build
./dev build                 # Clean + build all 11 projects

# Tests
./dev test                  # Backend (SQLite) + Frontend
./dev test backend          # Backend only (SQLite)
./dev test backend:sql      # Backend only (SQL Server)
./dev test backend:both     # Backend on both providers
./dev test frontend         # Frontend (Vitest)
./dev test e2e              # E2E (Playwright, SQLite backend)
./dev test e2e:sql          # E2E (Playwright, SQL Server backend)
./dev test e2e:headed       # E2E with browser visible
./dev test e2e:ui           # Playwright UI mode

# Lint
./dev lint                  # Frontend ESLint

# Start services
./dev start                 # Full stack via Aspire
./dev start api             # API only
./dev start frontend        # Frontend dev server only

# Migrations (adds to BOTH providers at once)
./dev migrate add <Name>    # Add migration to SQLite + SQL Server
./dev migrate list sqlite   # List SQLite migrations
./dev migrate list sql      # List SQL Server migrations

# SQL Server Docker
./dev docker up             # Start SQL Server container
./dev docker down           # Stop and remove container

# Azure Infrastructure (Terraform)
./dev infra plan            # Plan stage1-mvp (default)
./dev infra apply           # Apply stage1-mvp
./dev infra output          # Show deployed resource URLs
./dev infra status          # List resources in state
./dev infra destroy         # Tear down infrastructure

# Full verification gate (build + all tests + lint)
./dev verify
```

### Development Test Accounts

In **Development** mode, three test accounts are seeded automatically (one per role):

| Role | Email | Password |
|------|-------|----------|
| User | `dev.user@lemondo.dev` | `User1234` |
| Admin | `dev.admin@lemondo.dev` | `Admin1234` |
| SystemAdmin | `dev.sysadmin@lemondo.dev` | `SysAdmin1234` |

> These accounts are **only** created when `ASPNETCORE_ENVIRONMENT=Development` (the default for `dotnet run`). They are never seeded in production.

### Running Services Individually

If Aspire has issues, start services separately:

```bash
./dev start api        # Terminal 1
./dev start frontend   # Terminal 2
```

> **Note**: When running via Aspire, ports are dynamically assigned. When running individually, the API defaults to `http://localhost:5155` and the frontend to `http://localhost:5173`.

### Testing with SQL Server

The `./dev` CLI handles all SQL Server connection wiring automatically:

```bash
# Start SQL Server (if not already running)
./dev docker up

# Run backend tests against SQL Server
./dev test backend:sql

# Run E2E tests against SQL Server
./dev test e2e:sql

# Run backend on both providers sequentially
./dev test backend:both

# Tear down
./dev docker down
```

> The default connection uses `sa/YourStr0ngPassw0rd` on `localhost:1433`. Override with `TEST_SQLSERVER_CONNECTION_STRING` env var.

### Database Migrations

The project uses separate migration assemblies per provider (`LemonDo.Migrations.Sqlite` + `LemonDo.Migrations.SqlServer`), enabling `MigrateAsync()` for both.

```bash
# Add a migration to BOTH providers at once
./dev migrate add AddNewColumn

# List migrations
./dev migrate list sqlite
./dev migrate list sql

# Remove last migration (one provider at a time)
./dev migrate remove sqlite
./dev migrate remove sql
```

### Azure Infrastructure

The project includes a 3-stage Terraform infrastructure for Azure deployment. Stage 1 (MVP) is the default.

**Prerequisites**: Azure CLI (`az`) and Terraform (>= 1.5) — both are auto-detected from PATH or common install locations.

```bash
# First-time setup: bootstrap the Terraform state backend
az login
./dev infra bootstrap

# Initialize and deploy Stage 1 MVP (~$55/month)
./dev infra init
./dev infra plan
./dev infra apply

# View deployed resource URLs
./dev infra output

# Other stages: stage2-resilience, stage3-scale
./dev infra plan stage2-resilience
```

The CI/CD pipeline (`.github/workflows/deploy.yml`) runs tests on all pushes and deploys to Azure on push to `main`.

### API Documentation

When the API is running, visit (port may vary with Aspire):
- **Scalar UI**: `http://localhost:5155/scalar/v1`
- **OpenAPI JSON**: `http://localhost:5155/openapi/v1.json`
- **Health Check**: `http://localhost:5155/health`
- **Liveness**: `http://localhost:5155/alive`

### Troubleshooting

**Browser shows certificate warning / ERR_CERT_AUTHORITY_INVALID**

The .NET development server uses a self-signed HTTPS certificate. If your browser doesn't trust it:

```bash
# Trust the ASP.NET Core development certificate (run once)
dotnet dev-certs https --trust
```

On Windows, this adds the cert to the Trusted Root store and you'll see a system prompt to confirm. On macOS, it adds to Keychain. On Linux, you may need to manually add the cert to your browser or use HTTP instead.

If you still have issues after trusting, try resetting the certificate:

```bash
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

**Aspire Dashboard certificate warning**

The Aspire Dashboard runs on HTTPS with its own self-signed certificate. If your browser blocks it, click "Advanced" > "Proceed to localhost (unsafe)" or use the login token URL from the console output directly.

**SQLite database locked errors**

If you see "database is locked" errors, ensure only one instance of the API is running. The E2E tests use a separate database (`lemondo-e2e.db`) to avoid conflicts with development data.

**Port conflicts**

If the default ports are in use, Aspire will automatically assign different ones. Check the Aspire Dashboard or console output for the actual URLs. When running individually, set the port via environment variables or `launchSettings.json`.

## Current Release: v0.1.0

Checkpoint 1 — Core Task Management. See [CHANGELOG.md](./CHANGELOG.md) for details.

**What's included**: Full-stack DDD task management with kanban board (drag-and-drop), list view (time-based grouping), 18 API endpoints, 287+ tests, lemon.io-inspired design, and full-stack observability.

## Development Journal

Our complete thought process — every decision, phase, and lesson learned — is documented in the **[Development Journal](./docs/JOURNAL.md)**.

Highlights:
- **Phase 1**: Product requirements, technology research, user scenarios (3 personas, 10 storyboards), revised PRD
- **Phase 2**: Development guidelines, state management strategy (TanStack Query + Zustand), Architecture Tiers + Component Taxonomy
- **Delivery Strategy**: 5 incremental checkpoints, each producing a complete runnable application
- **Key Decisions**: `TaskItem` naming, `Result<T,E>` over exceptions, tasks before auth, HIPAA-Ready over certified

## Tech Stack

| Layer | Technology | Version |
|-------|-----------|---------|
| **Backend** | .NET 10 LTS | 10.0 |
| **Orchestration** | Aspire 13 | 13.1 |
| **ORM** | Entity Framework Core 10 | 10.0 |
| **Database** | SQLite (dev) / Azure SQL (cloud) | 3.x / 12.x |
| **API Docs** | Scalar | 2.x |
| **Frontend** | React 19 + TypeScript 5.9 | 19.2 |
| **Build** | Vite 7 | 7.3 |
| **UI** | Shadcn/ui + Radix | Latest |
| **Styling** | Tailwind CSS 4 | 4.1 |
| **Drag & Drop** | @dnd-kit | 6.x / 10.x |
| **Routing** | React Router | 7.13 |
| **Server State** | TanStack Query 5 | 5.90 |
| **Client State** | Zustand 5 | 5.0 |
| **Backend Tests** | MSTest 4 + FsCheck | 4.0 / 3.3 |
| **Frontend Tests** | Vitest + fast-check | 4.0 / 4.5 |
| **E2E Tests** | Playwright | 1.58 |
| **Observability** | OpenTelemetry (Aspire) | Built-in |

## Project Structure

```
lemon-todo/
├── .github/workflows/             # CI/CD pipeline
│   └── deploy.yml                 # Build, test, deploy (SQLite + SQL Server)
├── docs/                          # Design documents
│   ├── PRD.md                     # Product requirements (official)
│   ├── PRD.draft.md               # Initial draft PRD (historical)
│   ├── RESEARCH.md                # Technology research
│   ├── SCENARIOS.md               # User storyboards and analytics
│   ├── DOMAIN.md                  # DDD domain design
│   ├── JOURNAL.md                 # Development journal
│   ├── RELEASING.md               # Release process guide
│   ├── ROADMAP.md                 # Future roadmap (Tiers 1-9)
│   └── TRADEOFFS.md               # Trade-offs and assumptions
├── infra/                         # Terraform Azure infrastructure
│   ├── bootstrap/                 # One-time state backend setup
│   ├── modules/                   # Reusable Terraform modules (9)
│   ├── stages/                    # MVP → Resilience → Scale
│   └── scripts/                   # Bootstrap, deploy, cost-estimate
├── src/                           # Source code
│   ├── Directory.Build.props      # Centralized .NET versioning
│   ├── LemonDo.slnx              # Solution file (.NET 10 XML format)
│   ├── LemonDo.AppHost/           # Aspire orchestrator
│   ├── LemonDo.ServiceDefaults/   # Shared Aspire configuration
│   ├── LemonDo.Api/               # ASP.NET Core minimal API (+Dockerfile)
│   ├── LemonDo.Application/       # Use cases (commands + queries)
│   ├── LemonDo.Domain/            # Pure domain (entities, VOs, events)
│   ├── LemonDo.Infrastructure/    # EF Core (SQLite + SQL Server), external services
│   ├── LemonDo.Migrations.Sqlite/ # EF Core migrations for SQLite provider
│   ├── LemonDo.Migrations.SqlServer/ # EF Core migrations for SQL Server provider
│   └── client/                    # Vite + React frontend
├── tests/                         # Test projects
│   ├── LemonDo.Domain.Tests/      # Domain unit + property tests
│   ├── LemonDo.Application.Tests/ # Use case tests
│   ├── LemonDo.Api.Tests/         # API integration tests (SQLite + SQL Server)
│   └── e2e/                       # Playwright E2E tests (standalone pnpm project)
├── CHANGELOG.md                   # Release history (Keep a Changelog)
├── TASKS.md                       # Project task tracker
├── GUIDELINES.md                  # Development guidelines
├── README.md                      # This file
└── LICENSE                        # MIT License
```

## Architecture

See [docs/DOMAIN.md](./docs/DOMAIN.md) for the complete domain design (bounded contexts, entities, value objects, domain events, use cases, and API endpoints).

See [GUIDELINES.md](./GUIDELINES.md) for TDD methodology, code quality standards, git workflow, and security guidelines.

## Trade-offs and Assumptions

We optimized for incremental delivery, zero-config setup (SQLite + Aspire), and clean DDD architecture. Key trade-offs include tasks-before-auth sequencing (demonstrate architecture first), single-user mode in CP1, and sparse ranks over dense integers for card ordering. The repository pattern ensures database and infrastructure swaps require minimal changes.

See **[docs/TRADEOFFS.md](./docs/TRADEOFFS.md)** for the full assumptions list, trade-off comparison table, and scalability considerations.

## Roadmap

Beyond CP5, the roadmap spans 9 capability tiers - from AI integration to production operations.

| Tier | Focus |
|------|-------|
| 1 | AI & Agent Ecosystem |
| 2 | Third-Party Integrations |
| 3 | Collaboration & Real-Time |
| 4 | Advanced Task Modeling |
| 5 | Reporting & Developer Experience |
| 6 | Platform & Compliance |
| 7 | Product & Growth |
| 8 | UX Excellence |
| 9 | Reliability & Operations |

See **[docs/ROADMAP.md](./docs/ROADMAP.md)** for the full detailed roadmap with highlights for each tier.

## Contributing

1. Read [GUIDELINES.md](./GUIDELINES.md) first
2. Create a feature branch from `develop`: `git checkout -b feature/your-feature`
3. Follow TDD: write failing test first
4. Use conventional commits
5. Open a PR to `develop`

## License

[MIT](./LICENSE)
