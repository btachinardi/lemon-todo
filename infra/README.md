# LemonDo Infrastructure

Azure infrastructure-as-code using Terraform, organized in 3 deployment stages.

## Prerequisites

- [Terraform](https://developer.hashicorp.com/terraform/install) >= 1.5
- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli) (logged in via `az login`)
- [infracost](https://www.infracost.io/docs/) (optional, for cost estimates)

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
    │   Static Web App  │    │    App Service        │
    │   (React SPA)     │    │    (.NET API)         │
    └───────────────────┘    └───────────┬───────────┘
                                        │
                          ┌─────────────┼─────────────┐
                          │             │             │
                ┌─────────▼──┐  ┌───────▼──┐  ┌──────▼───┐
                │  Azure SQL │  │ Key Vault│  │  Redis   │
                │  Database  │  │          │  │ (Stage 3)│
                └────────────┘  └──────────┘  └──────────┘
```

## Stage Comparison

| Resource | Stage 1: MVP | Stage 2: Resilience | Stage 3: Scale |
|----------|-------------|-------------------|----------------|
| **App Service** | B1 ($13/mo) | S1 + staging slot ($73/mo) | P2v3 auto-scale 2-10 ($220-1100/mo) |
| **SQL Database** | Basic 5 DTU ($5/mo) | S1 20 DTU + geo-backup ($30/mo) | P1 + read replica ($930/mo) |
| **Key Vault** | Standard ($0/mo) | Standard + purge protect ($0/mo) | Standard + purge protect ($0/mo) |
| **App Insights** | Free tier ($0/mo) | Pay-as-you-go | Pay-as-you-go |
| **Static Web App** | Free ($0/mo) | Standard ($9/mo) | Standard ($9/mo) |
| **Networking** | None | VNet + private endpoints ($14/mo) | VNet + private endpoints ($14/mo) |
| **Front Door** | None | Standard + WAF ($35/mo) | Premium + WAF ($50/mo) |
| **Redis** | None | None | Premium P1 ($180/mo) |
| **CDN + Storage** | None | None | Blob static site ($15/mo) |
| **Est. Total** | **~$18/mo** | **~$180-195/mo** | **~$1,700-2,600/mo** |

## Quick Start

### 1. Bootstrap State Backend (one-time)

```bash
cp infra/bootstrap/terraform.tfvars.example infra/bootstrap/terraform.tfvars
# Edit terraform.tfvars with your subscription ID

./infra/scripts/bootstrap.sh
```

### 2. Deploy a Stage

```bash
cp infra/stages/stage1-mvp/terraform.tfvars.example infra/stages/stage1-mvp/terraform.tfvars
# Edit terraform.tfvars with your values (secrets, Azure AD info)

./infra/scripts/deploy.sh stage1-mvp
```

### 3. Cost Estimates

```bash
# All stages
./infra/scripts/cost-estimate.sh

# Specific stage
./infra/scripts/cost-estimate.sh stage1-mvp
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
├── modules/                     # Reusable Terraform modules
│   ├── app-service/             # App Service Plan + Web App
│   ├── sql-database/            # Azure SQL Server + Database
│   ├── key-vault/               # Key Vault + secrets + RBAC
│   ├── static-web-app/          # SPA hosting
│   ├── monitoring/              # App Insights + Log Analytics
│   ├── networking/              # VNet, subnets, private endpoints
│   ├── frontdoor/               # Azure Front Door + WAF
│   ├── cdn/                     # CDN + Storage
│   └── redis/                   # Redis Cache
├── stages/
│   ├── stage1-mvp/              # ~$18/mo
│   ├── stage2-resilience/       # ~$180-195/mo
│   └── stage3-scale/            # ~$1,700-2,600/mo
└── scripts/
    ├── bootstrap.sh
    ├── deploy.sh
    └── cost-estimate.sh
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

The GitHub Actions pipeline (`.github/workflows/deploy.yml`) runs:

1. **Backend tests** - SQLite (fast) + SQL Server (service container)
2. **Frontend tests** - Lint + Vitest + Build
3. **Docker build** - Verify the API Dockerfile builds
4. **Deploy** - Staging on `develop` push, Production on `main` push

### Required GitHub Secrets

| Secret | Description |
|--------|-------------|
| `AZURE_CREDENTIALS` | Service principal JSON for Azure login |
| `SWA_DEPLOYMENT_TOKEN` | Static Web App deployment token |

### Required GitHub Variables

| Variable | Description |
|----------|-------------|
| `AZURE_APP_SERVICE_NAME` | App Service resource name |

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

## Security Notes

- All secrets stored in Key Vault, referenced via `@Microsoft.KeyVault()` syntax
- App Service uses managed identity for Key Vault access (zero credentials in config)
- SQL Server uses firewall rules (Stage 1) or private endpoints (Stage 2+)
- `terraform.tfvars` files are gitignored - never commit secrets
