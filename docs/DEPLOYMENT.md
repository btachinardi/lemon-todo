# Deployment Guide

> CI/CD pipeline, Azure infrastructure, Docker, and production URLs.

---

## Production URLs

| Service | URL |
|---------|-----|
| **Web App** | [lemondo.btas.dev](https://lemondo.btas.dev) |
| **API** | [api.lemondo.btas.dev](https://api.lemondo.btas.dev) |
| **API Health** | [api.lemondo.btas.dev/health](https://api.lemondo.btas.dev/health) |
| **API Docs** | [api.lemondo.btas.dev/scalar/v1](https://api.lemondo.btas.dev/scalar/v1) |

## CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/deploy.yml`) runs on every push and PR to `main` and `develop`.

### Test Matrix (4 jobs)

| Job | What it does |
|-----|-------------|
| Backend Tests (SQLite) | Restore, build, test with SQLite |
| Backend Tests (SQL Server) | Same + SQL Server service container |
| Frontend Tests | pnpm install, lint, test, build |
| Docker Build | Multi-stage API image build |

### Deployment (on push to `main`)

After all 4 test jobs pass:

1. **API**: Docker image built, pushed to Azure Container Registry (tagged with commit SHA + `latest`), deployed to Container App via `az containerapp update`
2. **Frontend**: Built with `VITE_API_BASE_URL=https://api.lemondo.btas.dev`, deployed to Azure Static Web App

## Azure Infrastructure

The project includes a 3-stage Terraform infrastructure. Stage 1 (MVP) is deployed.

### Stages

| Stage | Resources | Cost |
|-------|-----------|------|
| **Stage 1: MVP** | Container App, ACR, SQL Database, Key Vault, Static Web App, App Insights, Log Analytics | ~$18/mo |
| Stage 2: Resilience | + Front Door with WAF, VNet, private endpoints | ~$180/mo |
| Stage 3: Scale | + Redis Cache, CDN, premium Container Apps with auto-scaling | ~$1.7K/mo |

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

- `api.lemondo.btas.dev` — API (Container App)
- `lemondo.btas.dev` — Frontend (Static Web App)

DNS is managed in Google Cloud DNS (`btas.dev` zone).

## Docker

The API uses a multi-stage Dockerfile (`src/LemonDo.Api/Dockerfile`):

- Non-root user for security
- Curl healthcheck included
- Migration assemblies bundled for startup migrations
- `.dockerignore` for minimal image size

```bash
# Local build
docker build -f src/LemonDo.Api/Dockerfile -t lemondo-api .
```

## Release Process

See [RELEASING.md](./RELEASING.md) for the full gitflow release process (version bump, changelog, tag, merge).
