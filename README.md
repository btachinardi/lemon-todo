# LemonDo

> A task management platform that combines consumer-grade UX with enterprise-grade compliance.

[![Build Status](https://img.shields.io/badge/build-planning-yellow)]()
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](./LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple)]()
[![React](https://img.shields.io/badge/React-19-blue)]()
[![Aspire](https://img.shields.io/badge/Aspire-13-orange)]()

## What is LemonDo?

LemonDo is a full-stack task management platform built with .NET Aspire and React. It features a Kanban board, list view, HIPAA-Ready data handling, role-based access control, and a delightful onboarding experience. It's designed for individuals and small teams in regulated industries who need simplicity without sacrificing compliance.

### Key Features

- **Kanban Board & List View** - Visualize work your way
- **HIPAA-Ready** - PII encryption, redaction, and audit trails
- **Role-Based Access Control** - User, Admin, SystemAdmin roles
- **Beautiful Onboarding** - Guided tour from signup to first completed task
- **Mobile-First PWA** - Works offline, installable, responsive
- **Dark & Light Themes** - System-aware with manual toggle
- **Multi-Language** - English, Portuguese, Spanish
- **Full Observability** - OpenTelemetry traces, metrics, and structured logs

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 23+](https://nodejs.org/)
- [pnpm](https://pnpm.io/)
- [Docker](https://www.docker.com/) (optional, for containerized development)

### Quick Start

```bash
git clone https://github.com/your-org/lemon-todo.git
cd lemon-todo

# Run with Aspire (starts all services)
dotnet run --project src/LemonDo.AppHost

# Or run individually
dotnet run --project src/LemonDo.Api          # Backend
cd src/client && pnpm install && pnpm dev     # Frontend
```

### Running Tests

```bash
dotnet test                                        # All backend tests
cd src/client && pnpm test                         # Frontend tests
cd tests/LemonDo.E2E.Tests && dotnet test          # E2E tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
cd src/client && pnpm test:coverage
```

### API Documentation

When the API is running, visit:
- **Scalar UI**: `http://localhost:5000/scalar/v1`
- **OpenAPI JSON**: `http://localhost:5000/openapi/v1.json`
- **Health Check**: `http://localhost:5000/health`

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
| **Backend** | .NET 10 LTS | 10.x |
| **Orchestration** | Aspire 13 | 13.x |
| **ORM** | Entity Framework Core 10 | 10.x |
| **Database** | SQLite | 3.x |
| **Auth** | ASP.NET Core Identity | 10.x |
| **API Docs** | Scalar | 2.x |
| **Frontend** | React 19 + TypeScript | 19.x |
| **Build** | Vite 7 | 7.x |
| **UI** | Shadcn/ui + Radix | CLI-based |
| **Styling** | Tailwind CSS | 4.x |
| **Routing** | React Router | 7.x |
| **Server State** | TanStack Query 5 | 5.x |
| **Client State** | Zustand 5 | 5.x |
| **i18n** | react-i18next | 16.x |
| **PWA** | vite-plugin-pwa | 1.x |
| **Backend Tests** | xUnit v3 + FsCheck | 3.x |
| **Frontend Tests** | Vitest + fast-check | 3.x / 4.x |
| **E2E Tests** | Playwright | 1.x |
| **Backend Observability** | OpenTelemetry + Serilog | 1.x / 4.x |
| **Frontend Observability** | OTel Browser SDK | 2.x |
| **CI/CD** | GitHub Actions | N/A |
| **IaC** | Terraform + azurerm | 1.x / 4.x |
| **Containers** | Docker Engine | 29.x |

## Project Structure

```
lemon-todo/
├── docs/                          # Design documents
│   ├── PRD.md                     # Product requirements (official)
│   ├── PRD.draft.md               # Initial draft PRD (historical)
│   ├── RESEARCH.md                # Technology research
│   ├── SCENARIOS.md               # User storyboards and analytics
│   ├── DOMAIN.md                  # DDD domain design
│   ├── JOURNAL.md                 # Development journal
│   ├── ROADMAP.md                 # Future roadmap (Tiers 1-9)
│   └── TRADEOFFS.md               # Trade-offs and assumptions
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

## Architecture

See [docs/DOMAIN.md](./docs/DOMAIN.md) for the complete domain design (bounded contexts, entities, value objects, domain events, use cases, and API endpoints).

See [GUIDELINES.md](./GUIDELINES.md) for TDD methodology, code quality standards, git workflow, and security guidelines.

## Trade-offs and Assumptions

We optimized for incremental delivery, zero-config setup (SQLite + Aspire), and browser-first PWA. Key trade-offs include tasks-before-auth sequencing, HIPAA-Ready over full certification, and Zustand + TanStack Query over Redux. The repository pattern ensures database and infrastructure swaps require minimal changes.

See **[docs/TRADEOFFS.md](./docs/TRADEOFFS.md)** for the full assumptions list, trade-off comparison table, and scalability considerations.

## Roadmap

Beyond CP5, the roadmap spans 9 capability tiers — from AI integration to production operations.

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
