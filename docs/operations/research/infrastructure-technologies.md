# Infrastructure Technologies

> **Source**: Extracted from docs/operations/research.md §4
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date**: 2026-02-13
> **Purpose**: Document the infrastructure and CI/CD technologies used in the LemonDo stack.

---

## 4. Infrastructure Technologies

### 4.1 Docker

- **Multi-stage builds**: Separate build/runtime stages for minimal images
- **Docker Compose**: Local development orchestration
- **Aspire integration**: Auto-generated Dockerfiles via `AddJavaScriptApp`

### 4.2 GitHub Actions

- **CI**: Build, test, lint on push/PR
- **CD**: Deploy on tag/release via environment gates
- **Matrix builds**: .NET + Node parallel test execution
- **Caching**: NuGet + pnpm cache for faster builds
- **Runner Images**:
  - `ubuntu-latest` → Ubuntu 24.04 (default since Jan 2025)
  - `windows-2025` → Windows Server 2025
  - `windows-2025-vs2026` → Windows Server 2025 + Visual Studio 2026 (beta)
  - `macos-26-large` → Intel-based macOS (preview)
- **Action Versions** (used in the project CI):
  - `actions/checkout@v6` (v6.0.2, Jan 2025)
  - `actions/setup-dotnet@v5` (v5.1.0, Jan 2025)
  - `actions/setup-node@v6` (v6.2.0, Jan 2025)
  - `pnpm/action-setup@v4` (v4.2.0, Oct 2024)
  - `azure/login@v2` (v2.3.0, Apr 2025)
  - `azure/webapps-deploy@v3`
  - `Azure/static-web-apps-deploy@v1`
- **2026 Features**:
  - Runner Scale Set Client (public preview): Go-based custom autoscaler without Kubernetes
  - Action allowlisting expanded to all plan tiers (restrict third-party actions)
  - Self-hosted runner pricing: $0.002/min platform charge in private repos (from March 2026)
- **Source**: [GitHub Actions Runner Images](https://github.com/actions/runner-images)

### 4.3 Terraform + Azure

- **Terraform Version**: 1.14.5 (latest stable, Feb 11, 2026)
  - Alpha: 1.15.0-alpha20260204
  - Project constraint: `required_version = ">= 1.5"` (compatible)
- **Provider**: `hashicorp/azurerm` 4.x (latest: 4.60.0)
  - Project constraint: `~> 4.0` (compatible)
- **Target**: Azure Container Apps (API) + Azure Static Web Apps (frontend)
  - Originally planned for App Service; migrated to Container Apps due to VM quota limitations (see [journal/v1.md](../journal/v1.md))
- **Resources**:
  - Azure Container Apps (API with auto-scaling 0-N replicas)
  - Azure Container Registry (Docker images tagged by commit SHA)
  - Azure Static Web Apps (frontend SPA)
  - Azure SQL Database (production)
  - Azure Key Vault for secrets
  - Application Insights + Log Analytics workspace
  - Azure Front Door / CDN (stage 2+)
  - Azure Cache for Redis (stage 2+)
  - Virtual Network integration (stage 2+)
- **State Backend**: Azure Storage Account with blob versioning
- **Staged Deployment**: 3 stages (MVP → Resilience → Scale) with separate state files
- **Source**: [Terraform AzureRM Provider](https://registry.terraform.io/providers/hashicorp/azurerm/latest), [Terraform Releases](https://github.com/hashicorp/terraform/releases)

### 4.4 SQL Server (CI + Production)

- **Azure SQL Database** (production): Managed service, always up-to-date, version `12.0` in Terraform
- **SQL Server 2025** (GA November 18, 2025):
  - Docker image: `mcr.microsoft.com/mssql/server:2025-latest` (Ubuntu 22.04 based)
  - AI features: vector datatype, DiskANN for large vectors, T-SQL embedding functions
  - Native JSON support, REST APIs, RegEx and fuzzy string matching
  - Change event streaming to Azure Event Hubs
  - Express edition: 50 GB max database size (up from 10 GB)
  - Standard edition: up to 32 cores / 256 GB memory
- **SQL Server 2022** (maintained, CU23 as of Jan 29, 2026):
  - Docker image: `mcr.microsoft.com/mssql/server:2022-latest`
  - Mature and battle-tested, used in the project CI pipeline
- **CI Usage**: SQL Server 2022 container for integration tests alongside SQLite for unit tests
- **Health Check**: `/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P '<password>' -Q 'SELECT 1' -C -b`
- **Source**: [SQL Server 2025 GA](https://techcommunity.microsoft.com/blog/sqlserver/sql-server-2025-is-now-generally-available/4470570), [SQL Server Docker Images](https://mcr.microsoft.com/product/mssql/server/about)
