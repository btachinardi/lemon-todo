# LemonDo Infrastructure

Azure infrastructure-as-code using Terraform, organized in 3 deployment stages.

## Prerequisites

- [Terraform](https://developer.hashicorp.com/terraform/install) >= 1.5
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (logged in via `az login`)

## Architecture

```
                    ┌─────────────────┐
                    │   Front Door    │  (Stage 2+)
                    │   + WAF         │
                    └───────┬─────────┘
                            │
              ┌─────────────┴─────────────┐
              │                           │
    ┌─────────▼─────────┐    ┌───────────▼──────────┐
    │   Static Web App  │    │   Container App      │
    │   (React SPA)     │    │   (.NET API)         │
    └───────────────────┘    └───────────┬───────────┘
                                        │
                          ┌─────────────┼─────────────┐
                          │             │             │
                ┌─────────▼──┐  ┌───────▼──┐  ┌──────▼───┐
                │  Azure SQL │  │ Key Vault│  │  Redis   │
                │  Database  │  │          │  │ (Stage 3)│
                └────────────┘  └──────────┘  └──────────┘
```

Stage 1 (MVP) uses Azure Container Apps — serverless containers with built-in auto-scaling, ingress, and managed identity. Stages 2 and 3 can optionally use Azure App Service for workloads that need dedicated compute.

## Stage Comparison

| Resource | Stage 1: MVP | Stage 2: Resilience | Stage 3: Scale |
|----------|-------------|-------------------|----------------|
| **Container App** | 0.25 CPU, 0.5 GB, 0-3 replicas | 0.5 CPU, 1 GB, 0-5 replicas | 1.0 CPU, 2 GB, auto-scale 0-10 |
| **Container Registry** | Basic | Basic | Basic |
| **SQL Database** | Basic 5 DTU ($5/mo) | S1 20 DTU + geo-backup ($30/mo) | P1 + read replica ($930/mo) |
| **Key Vault** | Standard ($0/mo) | Standard + purge protect ($0/mo) | Standard + purge protect ($0/mo) |
| **App Insights** | Free tier ($0/mo) | Pay-as-you-go | Pay-as-you-go |
| **Static Web App** | Standard ($9/mo) | Standard ($9/mo) | Standard ($9/mo) |
| **Networking** | None | VNet + private endpoints ($14/mo) | VNet + private endpoints ($14/mo) |
| **Front Door** | None | Standard + WAF ($35/mo) | Premium + WAF ($50/mo) |
| **Redis** | None | None | Premium P1 ($180/mo) |
| **CDN + Storage** | None | None | Blob static site ($15/mo) |
| **Est. Total** | **~$18/mo** | **~$180-195/mo** | **~$1,700-2,600/mo** |

Container Apps use consumption-based pricing — replicas scale to zero when idle and auto-scale under load.

## Quick Start

### 1. Bootstrap State Backend (one-time)

```bash
# Create Azure Storage for Terraform remote state
./dev infra bootstrap
```

### 2. Deploy a Stage

```bash
cp infra/stages/stage1-mvp/terraform.tfvars.example infra/stages/stage1-mvp/terraform.tfvars
# Edit terraform.tfvars with your values (subscription_id, sql_admin_password, etc.)

./dev infra init            # Initialize Terraform
./dev infra plan            # Preview changes
./dev infra apply           # Deploy Stage 1 MVP
./dev infra output          # View deployed resource URLs
```

### 3. Other Stages

```bash
./dev infra plan stage2-resilience
./dev infra apply stage2-resilience
```

## Directory Structure

```
infra/
├── README.md                    # This file
├── bootstrap/                   # One-time state backend setup
│   ├── main.tf
│   ├── variables.tf
│   ├── outputs.tf
│   └── terraform.tfvars.example
├── modules/                     # Reusable Terraform modules (10)
│   ├── app-service/             # App Service Plan + Web App (alternative to container-app)
│   ├── container-app/           # Container App Environment + App + Registry
│   ├── sql-database/            # Azure SQL Server + Database + Firewall
│   ├── key-vault/               # Key Vault + secrets + RBAC
│   ├── static-web-app/          # SPA hosting + custom domain
│   ├── monitoring/              # App Insights + Log Analytics
│   ├── networking/              # VNet, subnets, private endpoints, private DNS
│   ├── frontdoor/               # Azure Front Door + WAF + health probes
│   ├── cdn/                     # CDN + Zone-Redundant Storage
│   └── redis/                   # Redis Cache + private endpoint
└── stages/
    ├── stage1-mvp/              # ~$18/mo  (Container App)
    ├── stage2-resilience/       # ~$180-195/mo
    └── stage3-scale/            # ~$1,700-2,600/mo
```

## Naming Convention

`{abbreviation}-lemondo-{environment}-{region}`

| Resource | Abbreviation | Example |
|----------|-------------|---------|
| Resource Group | `rg` | `rg-lemondo-mvp-eus2` |
| Container App | `ca` | `ca-lemondo-mvp-eus2` |
| Container App Env | `cae` | `cae-lemondo-mvp-eus2` |
| Container Registry | `cr` | `crlemondomvpeus2` |
| SQL Server | `sql` | `sql-lemondo-mvp-eus2` |
| Key Vault | `kv` | `kv-lemondo-mvp-eus2` |
| Static Web App | `swa` | `swa-lemondo-mvp-eus2` |
| Front Door | `afd` | `afd-lemondo-prod-eus2` |

## CI/CD

The GitHub Actions pipeline (`.github/workflows/deploy.yml`) runs on every push and PR to `main` and `develop`:

1. **Backend tests (SQLite)** — Restore, build, run full test suite
2. **Backend tests (SQL Server)** — Same suite against a SQL Server 2022 service container
3. **Frontend tests** — pnpm install, OpenAPI type generation, TypeScript check, ESLint, Vitest, production build
4. **Docker build** — Multi-stage Dockerfile verification (SDK build + aspnet runtime)
5. **Deploy** — Production on `main` push only (requires GitHub environment approval)

Deploy pushes a Docker image to Azure Container Registry (tagged by commit SHA + `latest`), updates the Container App, and deploys the frontend to Azure Static Web App.

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Service principal JSON for Azure login |
| `SWA_DEPLOYMENT_TOKEN` | Static Web App deployment token |

### Required GitHub Variables

| Variable | Description |
|----------|-------------|
| `AZURE_ACR_NAME` | Container Registry name (e.g., `crlemondomvpeus2`) |
| `AZURE_ACR_LOGIN_SERVER` | ACR login server URL (e.g., `crlemondomvpeus2.azurecr.io`) |
| `AZURE_CONTAINER_APP_NAME` | Container App resource name |
| `AZURE_RESOURCE_GROUP` | Resource group name |

## Custom Domains

Custom domains require a two-phase deployment:

### Phase 1: Deploy without custom domains (get DNS values)

```bash
./dev infra apply
./dev infra output
```

Note the outputs:
- `container_app_ingress_fqdn` — CNAME target for API
- `custom_domain_verification_id` — TXT record value for domain verification
- `static_web_app_hostname` — CNAME target for frontend

### Phase 2: Create DNS records (Google Cloud DNS)

```bash
# API: TXT record for domain verification
gcloud dns record-sets create asuid.api.lemondo.btas.dev \
  --zone=btas-dev \
  --type=TXT \
  --ttl=300 \
  --rrdatas='"<custom_domain_verification_id>"'

# API: CNAME record pointing to Container App
gcloud dns record-sets create api.lemondo.btas.dev \
  --zone=btas-dev \
  --type=CNAME \
  --ttl=300 \
  --rrdatas='<container_app_ingress_fqdn>.'

# Frontend: CNAME record pointing to Static Web App
gcloud dns record-sets create lemondo.btas.dev \
  --zone=btas-dev \
  --type=CNAME \
  --ttl=300 \
  --rrdatas='<static_web_app_hostname>.'
```

Wait for DNS propagation (check with `dig` or `nslookup`).

### Phase 3: Enable custom domains in Terraform

Add to `terraform.tfvars`:
```hcl
api_custom_domain      = "api.lemondo.btas.dev"
frontend_custom_domain = "lemondo.btas.dev"
```

```bash
./dev infra apply
```

This binds custom domains, provisions managed TLS certificates, and updates CORS.

**Note**: The Container App custom domain uses `terraform_data` with `local-exec`
(Azure CLI). On Windows, this requires `bash` in PATH (Git Bash). The provisioner
includes a polling loop that waits up to 5 minutes for managed certificate
provisioning before binding.

### Verification

```bash
curl https://api.lemondo.btas.dev/health   # → Healthy
curl https://api.lemondo.btas.dev/alive    # → Healthy
# Open https://lemondo.btas.dev → frontend loads
# Login flow works (cookies sent cross-subdomain)
```

### Cookie Auth Cross-Subdomain

The refresh token cookie uses `SameSite=Strict` and `Path=/api/auth`. Since
`lemondo.btas.dev` and `api.lemondo.btas.dev` share the same registrable
domain (`btas.dev`), they are "same-site" — cookies are sent automatically.
No cookie domain configuration changes are needed.

## SPA Routing

The frontend requires `src/client/public/staticwebapp.config.json` with a
`navigationFallback` rewrite to `/index.html`. Without this, refreshing on
client-side routes (e.g. `/login`, `/list`) returns 404 from Azure Static Web Apps.

## Security

- All secrets stored in Azure Key Vault (Standard SKU, RBAC-enabled)
- Container App uses SystemAssigned managed identity
- Secrets injected as Container App environment variables referencing Key Vault entries
- SQL Server uses firewall rules (Stage 1) or private endpoints (Stage 2+)
- TLS 1.2 minimum enforced on SQL Server, Key Vault, and Redis
- Non-root user in Docker container
- `terraform.tfvars` files are gitignored — never commit secrets
