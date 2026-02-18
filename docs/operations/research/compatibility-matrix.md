# Compatibility Matrix

> **Source**: Extracted from docs/operations/research.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date**: 2026-02-13
> **Purpose**: Cross-reference of version compatibility across all technologies in the LemonDo stack.

---

## 5. Compatibility Matrix

| Component | Requires | Compatible With |
|-----------|----------|-----------------|
| .NET 10 | - | Aspire 13, EF Core 10, Identity |
| Aspire 13 | .NET 8+ | .NET 10, JavaScript apps, Python apps |
| Vite 7 | Node 18+ | React 19, Vitest 3+, Tailwind 4 |
| React 19 | Vite 6+ | Shadcn/ui, react-i18next 16 |
| Shadcn/ui | React 18+ | React 19, Tailwind 4, Radix UI |
| Zustand 5 | React 18+ | React 19, TypeScript 5 |
| TanStack Query 5 | React 18+ | React 19, TypeScript 5 |
| OTel Browser SDK | ES2022+ browsers | Aspire Dashboard (OTLP HTTP) |
| Playwright 1.58 | .NET 8+ | .NET 10, Chromium, Firefox, WebKit |
| Terraform 1.14 | - | azurerm 4.x, Azure backends |
| azurerm 4.x | Terraform 1.x | Azure Container Apps, SQL, Static Web Apps |
| SQL Server 2025 | Docker / Azure SQL | .NET 10, EF Core 10, Ubuntu 22.04 |
| Node.js 24 (LTS) | - | Vite 7, pnpm 10, Playwright |
| GitHub Actions | ubuntu-24.04 | .NET 10, Node 24, pnpm 10 |
