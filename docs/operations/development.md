# Development Guide

> **Source**: Extracted from docs/DEVELOPMENT.md
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> Prerequisites, CLI reference, test accounts, database setup, and troubleshooting.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 22+](https://nodejs.org/) (LTS recommended)
- [pnpm](https://pnpm.io/)
- [Docker](https://www.docker.com/) (optional, for SQL Server)

> **Dev Container**: If you use VS Code or GitHub Codespaces, open the project in the [dev container](../../.devcontainer/devcontainer.json) — all prerequisites are pre-installed and `./dev install` runs automatically.

## Quick Start

```bash
git clone https://github.com/btachinardi/lemon-todo.git
cd lemon-todo
./dev install   # Restores packages, generates dev config files + API types
./dev start     # Starts API + frontend via Aspire
```

`./dev install` handles everything a fresh clone needs:

1. HTTPS dev certificate (`dotnet dev-certs https`) — required for Aspire; no-ops if already present
2. .NET package restore (`dotnet restore`)
3. Frontend packages (`pnpm install` in `src/client/`)
4. E2E packages (`pnpm install` in `tests/e2e/`)
5. Dev config files — creates `appsettings.Development.json` and `launchSettings.json` with safe dev defaults (skips if they already exist)
6. TypeScript API types — generates `src/client/src/api/schema.d.ts` from the committed OpenAPI spec

The Aspire Dashboard URL (with login token) will appear in the console output. From there you can see all service URLs, logs, traces, and metrics.

## Developer CLI (`./dev`)

All common commands are available via the `./dev` script at the project root.

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
./dev test e2e:update-snapshots  # Regenerate visual regression baselines

# Lint & Verify
./dev lint                  # Frontend ESLint
./dev verify                # Full gate: build + generate + all tests + lint

# Code Generation
./dev generate              # Regenerate OpenAPI spec + TypeScript types

# Start services
./dev start                 # Full stack via Aspire
./dev start api             # API only
./dev start frontend        # Frontend dev server only

# Migrations (adds to BOTH providers at once)
./dev migrate add <Name>    # Add migration to SQLite + SQL Server
./dev migrate list sqlite   # List SQLite migrations
./dev migrate list sql      # List SQL Server migrations
./dev migrate remove sqlite # Remove last SQLite migration
./dev migrate remove sql    # Remove last SQL Server migration

# SQL Server Docker
./dev docker up             # Start SQL Server container
./dev docker down           # Stop and remove container

# Azure Infrastructure (Terraform)
./dev infra plan            # Plan stage1-mvp (default)
./dev infra apply           # Apply stage1-mvp
./dev infra output          # Show deployed resource URLs
./dev infra status          # List resources in state
./dev infra destroy         # Tear down infrastructure
```

### Direct Commands

When you need more control than the CLI provides:

```bash
# Run specific backend test project
dotnet test tests/LemonDo.Domain.Tests
dotnet test tests/LemonDo.Application.Tests
dotnet test tests/LemonDo.Api.Tests

# Backend coverage
dotnet test --solution src/LemonDo.slnx --collect:"XPlat Code Coverage"

# Frontend extras
cd src/client && pnpm test:watch     # Watch mode
cd src/client && pnpm test:coverage  # Coverage
cd src/client && pnpm tsc --noEmit   # Type checking
cd src/client && pnpm build          # Production build

# Add a Shadcn/ui component
cd src/client && pnpm dlx shadcn@latest add <component-name>
```

## Test Accounts

In **Development** mode, three accounts are seeded automatically:

| Role | Email | Password |
|------|-------|----------|
| User | `dev.user@lemondo.dev` | `User1234` |
| Admin | `dev.admin@lemondo.dev` | `Admin1234` |
| SystemAdmin | `dev.sysadmin@lemondo.dev` | `SysAdmin1234` |

> These accounts are **only** created when `ASPNETCORE_ENVIRONMENT=Development`. They are never seeded in production.

## Running Services Individually

If Aspire has issues, start services separately:

```bash
./dev start api        # Terminal 1
./dev start frontend   # Terminal 2
```

> When running via Aspire, ports are dynamically assigned. When running individually, the API defaults to `http://localhost:5155` and the frontend to `http://localhost:5173`.

## SQL Server Testing

The default development database is SQLite (zero-config). SQL Server is available for production-parity testing:

```bash
# Start SQL Server (Docker)
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

> Default connection: `sa/YourStr0ngPassw0rd` on `localhost:1433`. Override with `TEST_SQLSERVER_CONNECTION_STRING` env var.

## Database Migrations

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

## Visual Regression Testing

E2E tests include visual regression snapshots that detect unintended UI changes. Snapshots are stored in `tests/e2e/specs/visual-regression.spec.ts-snapshots/` and committed to git.

### When snapshots fail

If `./dev test e2e` reports visual regression failures, the screenshots have drifted from their baselines. This happens when you intentionally change colors, layout, or typography.

**Review the diff first** — Playwright saves actual/diff/expected images in `tests/e2e/test-results/`. Verify the visual changes are intentional.

**Regenerate baselines:**

```bash
# Update ALL visual regression snapshots
./dev test e2e:update-snapshots

# Update only specific snapshots (pass extra Playwright args)
./dev test e2e:update-snapshots --grep="Landing Page"
```

After updating, review the new snapshot files in git diff to confirm they look correct, then commit them alongside your UI changes.

### WCAG contrast testing

Color contrast tests use axe-core and run with `reducedMotion: 'reduce'` to disable Framer Motion / CSS animations. A CSS override forces `opacity: 1 !important` so that below-fold elements gated by `useInView` are evaluated at their final intended colors.

## API Documentation

When the API is running (port may vary with Aspire):

| Endpoint | URL |
|----------|-----|
| Scalar UI | `http://localhost:5155/scalar/v1` |
| OpenAPI JSON | `http://localhost:5155/openapi/v1.json` |
| Health Check | `http://localhost:5155/health` |
| Liveness | `http://localhost:5155/alive` |

## Troubleshooting

### Browser certificate warning (ERR_CERT_AUTHORITY_INVALID)

```bash
# Trust the ASP.NET Core development certificate (run once)
dotnet dev-certs https --trust

# If still failing, reset and re-trust
dotnet dev-certs https --clean
dotnet dev-certs https --trust
```

### Aspire Dashboard certificate warning

Click "Advanced" > "Proceed to localhost (unsafe)" or use the login token URL from the console output directly.

### SQLite database locked errors

Ensure only one instance of the API is running. E2E tests use a separate database (`lemondo-e2e.db`) to avoid conflicts.

### Port conflicts

Aspire auto-assigns different ports when defaults are in use. Check the Aspire Dashboard or console output for actual URLs.

### .NET 10 gotchas

| Issue | Detail |
|-------|--------|
| Solution format | `dotnet new sln` creates `.slnx` (XML-based), not `.sln` |
| `dotnet test` syntax | Requires `--solution` flag, no positional arg |
| Aspire + pnpm | `AddJavaScriptApp` defaults to npm — chain `.WithPnpm()` |
| Cached build hides warnings | Use `dotnet clean && dotnet build` for true warning count |
