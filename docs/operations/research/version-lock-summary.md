# Version Lock Summary

> **Source**: Extracted from docs/operations/research.md §6
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date**: 2026-02-13
> **Purpose**: Pinned versions for every dependency in the LemonDo MVP stack.

---

## 6. Version Lock Summary

These are the versions targeted for the LemonDo MVP:

```
# Backend
dotnet: 10.x (LTS)
aspire: 13.x
ef-core: 10.x
identity: 10.x
scalar: 2.x
fscheck: 3.x
mstest: 4.x (MTP runner)
serilog: 4.x
web-push: 2.x
opentelemetry-dotnet: 1.x

# Frontend
node: 24.x (LTS "Krypton")
vite: 7.x
react: 19.x
typescript: 5.x
tailwindcss: 4.x
shadcn-ui: CLI-based (generates code, no runtime version)
react-router: 7.x
react-i18next: 16.x
i18next: 25.x
zustand: 5.x
@tanstack/react-query: 5.x
vite-plugin-pwa: 1.x
@opentelemetry/sdk-trace-web: 2.x
@opentelemetry/instrumentation-fetch: 0.x (experimental, follows OTel JS contrib)
@opentelemetry/exporter-trace-otlp-http: 0.x (experimental, follows OTel JS contrib)
date-fns: 4.x
react-day-picker: 9.x

# Testing
vitest: 4.x
fast-check: 4.x
@testing-library/react: 16.x
msw: 2.x
playwright: 1.x (E2E with cookie-based auth, unique users per describe block)

# Infrastructure
docker: 29.x
terraform: 1.14.x
azurerm-provider: 4.x (latest: 4.60.0)
sql-server: 2022 (CI container), Azure SQL Database (production)
pnpm: 10.x
developer-cli: ./dev bash script (build, test, lint, start, migrate, docker, verify)

# CI/CD (GitHub Actions)
actions/checkout: v6
actions/setup-dotnet: v5
actions/setup-node: v6
pnpm/action-setup: v4
azure/login: v2
azure/webapps-deploy: v3
Azure/static-web-apps-deploy: v1
runner-image: ubuntu-latest (Ubuntu 24.04)

# Codegen / Contract Tools
openapi-typescript: 7.x
microsoft-extensions-apidescription-server: 10.x
```
