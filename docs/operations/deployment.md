# Deployment Guide

> **Source**: Extracted from docs/DEPLOYMENT.md
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> CI/CD pipeline, Azure infrastructure, Docker, observability, and production URLs.

---

## Production URLs

| Service | URL |
|---------|-----|
| **Web App** | [lemondo.btas.dev](https://lemondo.btas.dev) |
| **API** | [api.lemondo.btas.dev](https://api.lemondo.btas.dev) |
| **API Health** | [api.lemondo.btas.dev/health](https://api.lemondo.btas.dev/health) |
| **API Liveness** | [api.lemondo.btas.dev/alive](https://api.lemondo.btas.dev/alive) |
| **API Docs (Scalar)** | [api.lemondo.btas.dev/scalar/v1](https://api.lemondo.btas.dev/scalar/v1) |
| **OpenAPI Spec** | [api.lemondo.btas.dev/openapi/v1.json](https://api.lemondo.btas.dev/openapi/v1.json) |

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/deploy.yml`) runs on every push and PR to `main` and `develop`.

### Pipeline Jobs

| Job | What it does | Runs on |
|-----|-------------|---------|
| **Backend Tests (SQLite)** | Restore, build, test full suite with in-memory SQLite | `ubuntu-latest` |
| **Backend Tests (SQL Server)** | Same suite against SQL Server 2022 service container | `ubuntu-latest` + `mssql/server:2022` |
| **Frontend Tests** | pnpm install, OpenAPI type generation, `tsc -b`, ESLint, Vitest, production build | `ubuntu-latest` + Node 24 |
| **Docker Build** | Multi-stage API Dockerfile build (depends on backend + frontend passing) | `ubuntu-latest` |
| **Deploy** | Push image to ACR, update Container App, deploy SPA to Static Web App | `ubuntu-latest` (main only) |

The first three jobs run in parallel. Docker Build gates on backend + frontend. Deploy gates on all four and only triggers on `main` push with GitHub environment approval.

### Type Safety Across the Stack

The API project generates an OpenAPI spec at build time (`openapi.json` output to `src/client/`). The frontend runs `openapi-typescript` to produce `schema.d.ts` — every API call is type-checked against the actual backend contract. This runs both locally (`./dev generate`) and in CI before the frontend build.

### Deployment Steps

After all test jobs pass (main branch push only):

1. **API**: Docker image built from `src/LemonDo.Api/Dockerfile`, tagged with commit SHA + `latest`, pushed to Azure Container Registry, deployed via `az containerapp update`
2. **Frontend**: Built with `VITE_API_BASE_URL=https://api.lemondo.btas.dev`, deployed to Azure Static Web App via the `Azure/static-web-apps-deploy` action

### Local Verification Gate

Before pushing, `./dev verify` runs 6 sequential checks locally:

1. Backend Build (clean + build, 0 warnings enforced via `TreatWarningsAsErrors`)
2. Generate API Types (OpenAPI spec → TypeScript)
3. Frontend Build (Vite production build)
4. Backend Tests (all 3 test projects)
5. Frontend Tests (Vitest)
6. Frontend Lint (ESLint)

Any failure exits with code 1. This is the same gate CI enforces — no surprises in the pipeline.

## Azure Infrastructure

The project includes a 3-stage Terraform infrastructure with 10 reusable modules. Stage 1 (MVP) is deployed and serving production traffic.

### Stages

| Stage | Compute | Database | Networking | Cost |
|-------|---------|----------|------------|------|
| **Stage 1: MVP** | Container App (0.25 CPU, 0.5 GB, 0-3 replicas) | SQL Basic (5 DTU, 2 GB) | None | ~$18/mo |
| Stage 2: Resilience | Container App (0.5 CPU, 1 GB, 0-5 replicas) | SQL S1 (20 DTU, geo-backup) | VNet + private endpoints, Front Door + WAF | ~$180/mo |
| Stage 3: Scale | Container App (1.0 CPU, 2 GB, 0-10 replicas) | SQL P1 (zone-redundant, read replica) | + Redis Premium P1, CDN | ~$1.7K/mo |

Each stage uses the same Terraform modules with different variable values. Scaling up is a `terraform.tfvars` change and a `./dev infra apply`.

### Prerequisites

- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) (`az`)
- [Terraform](https://developer.hashicorp.com/terraform/install) (>= 1.5)

Both are auto-detected from PATH or common install locations by the `./dev` CLI.

### First-Time Setup

```bash
az login
./dev infra bootstrap    # Create Terraform state backend (Azure Storage)
./dev infra init         # Initialize Terraform
./dev infra plan         # Preview changes
./dev infra apply        # Deploy Stage 1 MVP
./dev infra output       # View deployed resource URLs
```

### Other Stages

```bash
./dev infra plan stage2-resilience
./dev infra apply stage2-resilience
```

### Custom Domains

Custom domains are managed via Terraform with Azure Container App managed certificates:

- `api.lemondo.btas.dev` — API (Container App, managed TLS)
- `lemondo.btas.dev` — Frontend (Static Web App, managed TLS)

DNS is managed in Google Cloud DNS (`btas.dev` zone). See [infra/README.md](../../infra/README.md) for the full 3-phase domain setup process.

## Docker

The API uses a multi-stage Dockerfile (`src/LemonDo.Api/Dockerfile`):

| Stage | Base Image | Purpose |
|-------|-----------|---------|
| **Build** | `mcr.microsoft.com/dotnet/sdk:10.0` | Restore, build, publish |
| **Runtime** | `mcr.microsoft.com/dotnet/aspnet:10.0` | Minimal production image |

Key properties:
- **Non-root execution**: `appuser` / `appgroup` — no privileged access
- **Health check**: `curl http://localhost:8080/alive` (30s interval, 5s timeout, 3 retries)
- **Layer caching**: Project files copied before source for incremental builds
- **Migration assemblies**: Both SQLite and SQL Server migration projects bundled for `MigrateAsync()` at startup
- **Port**: 8080 (HTTP only — TLS terminates at the Container App ingress)

```bash
# Local build
docker build -f src/LemonDo.Api/Dockerfile -t lemondo-api .
```

## Observability

The production stack includes:

| Component | What it provides |
|-----------|-----------------|
| **Application Insights** | Request tracing, dependency tracking, failure analysis, live metrics |
| **Log Analytics** | Structured log storage, KQL queries, 30-day retention (MVP) |
| **Serilog** | Structured logging with correlation IDs flowing through every layer |
| **Health endpoints** | `/health` (readiness) and `/alive` (liveness) — Container App probes check every 30s |
| **OpenAPI + Scalar** | Auto-generated interactive API docs at `/scalar/v1` |

Locally, Aspire Dashboard provides the same observability (logs, traces, metrics) without any Azure dependency.

## Security Posture

| Concern | Implementation |
|---------|---------------|
| **Secrets** | Azure Key Vault (Standard, RBAC-enabled), never in source |
| **Identity** | Container App SystemAssigned managed identity |
| **TLS** | 1.2 minimum on SQL Server, Key Vault, Redis; managed certs for custom domains |
| **Container** | Non-root user, minimal base image, no shell in runtime |
| **Network** | SQL firewall (Stage 1), private endpoints (Stage 2+) |
| **Auth** | JWT bearer + HttpOnly refresh cookie, `SameSite=Strict`, `Secure` in prod |
| **Encryption** | AES-256-GCM field encryption for PII (emails, display names, sensitive notes) |
| **Headers** | `SecurityHeadersMiddleware` adds CSP, X-Frame-Options, Referrer-Policy |

## Release Process

See [releasing.md](./releasing.md) for the full gitflow release process (version bump, changelog, tag, merge).
