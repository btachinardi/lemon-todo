# LemonDo - Technology Research

> **Date**: 2026-02-13
> **Purpose**: Document the latest versions, capabilities, and compatibility of all technologies in our stack.
> **Method**: Web research conducted on the day of project inception.

---

## 1. Backend Technologies

### 1.1 .NET 10 (LTS)

- **Version**: 10.0.103 (installed)
- **Release**: November 2025 (GA), LTS - supported for 3 years
- **Key Features**:
  - JIT inlining and method devirtualization improvements
  - AVX10.2 support, NativeAOT enhancements
  - New JSON serialization options (disallow duplicates, strict settings, PipeReader support)
  - CLI introspection with `--cli-schema`
  - Native container image creation for console apps
  - Platform-specific .NET tools with enhanced RID compatibility
  - One-shot tool execution with `dotnet tool exec`
- **EF Core 10**: LINQ enhancements, performance optimizations, named query filters with selective disabling
- **ASP.NET Core 10**: Blazor improvements, OpenAPI enhancements, minimal API updates
- **Source**: [Microsoft Learn - What's new in .NET 10](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-10/overview)

### 1.2 .NET Aspire 13

- **Version**: Aspire 13 (November 2025 release)
- **Naming**: Dropped the ".NET" prefix, now just "Aspire"
- **Key Features**:
  - **Aspirify**: Bring any project (.NET, Python, Node.js, Java) into the Aspire ecosystem
  - **`aspire do` command**: Replaces deployment scripts with modular, dependency-aware pipelines
  - **JavaScript integration**: `AddJavaScriptApp` API auto-detects package manager, generates Dockerfiles
  - **Python parity**: Full FastAPI/Uvicorn integration, VS Code debugging
  - **Single-file AppHost**: Reduced boilerplate for smaller projects (from 9.5)
  - **CLI GA**: Standalone CLI tool for create, configure, deploy (from 9.4)
  - **AI Visualizer**: Dashboard for inspecting LLM prompts/responses (from 9.5)
  - **Multi-resource logs**: Combined log viewing in dashboard (from 9.5)
- **Telemetry**: Built-in OpenTelemetry with OTLP export, Aspire Dashboard for logs/traces/metrics
- **Deployment**: Compiles to Kubernetes manifests, Terraform configs, Bicep/ARM, Docker Compose
- **Source**: [Aspire 13 - What's New](https://aspire.dev/whats-new/aspire-13/)

### 1.3 ASP.NET Core Identity

- **Authentication**: JWT tokens, cookie auth, external OAuth providers
- **RBAC**: Built-in role-based authorization via `[Authorize(Roles = "...")]` attribute
- **EF Core Integration**: `IdentityDbContext` for user/role storage
- **2FA**: TOTP authenticator app support built-in
- **Account Lockout**: Configurable lockout on failed attempts
- **Social OAuth**: Google, Microsoft, GitHub, Facebook providers
- **Source**: [Microsoft Learn - ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)

### 1.4 Entity Framework Core 10

- **ORM**: Latest EF Core with .NET 10
- **SQLite Provider**: `Microsoft.EntityFrameworkCore.Sqlite`
- **Named Query Filters**: Multiple filters per entity type with selective disabling
- **Performance**: Continued query optimization improvements
- **Migrations**: Code-first with full migration tooling

### 1.5 Scalar (API Documentation)

- **Purpose**: Modern replacement for Swagger UI, default in .NET 9+
- **Key Advantages**:
  - Faster load times and rendering than Swagger
  - Smart request builder with auto-generated examples
  - Advanced search across endpoints and schemas
  - OpenAPI 3.1 full support
  - Customizable theming with dark mode
  - Built-in authentication support
  - Live API request testing
  - Auto-generated client code for multiple languages
- **Integration**: `app.MapScalarApiReference()` at `/scalar/v1`
- **Enterprise**: Scalar Registry for API governance, SDK generation
- **Source**: [Scalar GitHub](https://github.com/scalar/scalar)

### 1.6 FsCheck 3.3.2

- **Version**: 3.3.2 (latest stable)
- **Purpose**: Property-based testing for .NET
- **Features**:
  - Random structured input generation
  - Automatic shrinking to minimal failing case
  - Integration with xUnit (`FsCheck.Xunit`), NUnit, MSTest
  - Custom generators and arbitraries
  - Stateful testing support
- **Usage**: `[Property]` attribute for xUnit integration
- **Source**: [FsCheck GitHub](https://github.com/fscheck/FsCheck)

### 1.7 OpenTelemetry for .NET

- **Integration**: Built into Aspire ServiceDefaults
- **Pillars**: Logging, Tracing, Metrics via OpenTelemetry SDK
- **Export**: OTLP protocol (REST or gRPC)
- **Dashboard**: Aspire Dashboard provides built-in UI for all telemetry
- **Frontend Support**: OTLP HTTP endpoint for browser trace collection
- **Source**: [Microsoft Learn - .NET Observability](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/observability-with-otel)

---

## 2. Frontend Technologies

### 2.1 Vite 7

- **Version**: 7.3.1 (latest)
- **Key**: Fast HMR, ES modules, optimized builds
- **React Plugin**: `@vitejs/plugin-react` for React 19 support
- **PWA Plugin**: `vite-plugin-pwa` for zero-config PWA
- **Vitest**: Native Vite-powered test runner
- **Source**: [Vite Releases](https://vite.dev/releases)

### 2.2 React 19

- **Version**: 19.2.1 (latest, December 2025)
- **Key Features**:
  - React Compiler (automatic memoization)
  - Server Components and Actions
  - `use()` hook for promises and context
  - `useFormStatus`, `useFormState` hooks
  - Improved error reporting
  - Document metadata support
- **Source**: [React Versions](https://react.dev/versions)

### 2.3 Shadcn/ui (2026)

- **Latest Features**:
  - **Component Styles**: Vega (classic), Nova (compact), Maia (rounded), Lyra (boxy), Mira (dense)
  - **Shadcn/Create**: Custom design systems beyond standard look
  - **Base UI Support** (Feb 2026): Choose between Radix UI and Base UI
  - **RTL Support** (Jan 2026): Inline start/end styles
  - **Structural Components**: Field wrappers, Input Groups, Button Groups
- **Foundation**: Radix UI primitives, Tailwind CSS styling
- **Source**: [Shadcn/ui Changelog](https://ui.shadcn.com/docs/changelog)

### 2.4 Tailwind CSS

- **Version**: 4.x (latest)
- **Key**: Utility-first CSS, JIT compilation, responsive design utilities
- **Integration**: First-class Vite support, Shadcn/ui foundation

### 2.5 react-i18next

- **Version**: 16.5.4 (react-i18next), 25.8.6 (i18next core)
- **Features**:
  - TypeScript selector API for typesafe translations
  - Hook-based API (`useTranslation`)
  - Context-based provider
  - Lazy loading of translation files
  - Pluralization, interpolation, nesting
  - ICU message format support
- **Source**: [react-i18next](https://react.i18next.com/)

### 2.6 vite-plugin-pwa

- **Purpose**: Zero-config PWA for Vite
- **Features**:
  - Automatic service worker registration
  - Workbox for offline caching strategies
  - Auto-inject web app manifest
  - Prompt-based content refresh
  - Development mode debugging
  - React hook: `useRegisterSW` from `virtual:pwa-register/react`
- **Strategy Options**: `generateSW` (auto) or `injectManifest` (custom control)
- **Source**: [vite-plugin-pwa](https://vite-pwa-org.netlify.app/)

### 2.7 Zustand 5

- **Version**: 5.0.11 (latest stable)
- **Purpose**: Lightweight client state management for React
- **Key Features**:
  - Minimal API (~1KB bundle), no boilerplate
  - No providers needed - stores are plain hooks
  - Built-in middleware: `persist` (localStorage/IndexedDB), `devtools`, `immer`
  - First-class TypeScript support
  - Compatible with React 19 and concurrent features
- **v5 Changes**:
  - Removed default equality function customization from `create` (use `createWithEqualityFn` for custom equality like `shallow`)
  - `persist` middleware requires explicit state hydration pattern
- **Why Zustand over Redux/Context**:
  - No provider wrapper needed (simpler component tree)
  - Stores are independent - no single global store monolith
  - Perfect for client-only state (UI state, form drafts, offline queue)
  - Naturally separates client state from server state (paired with TanStack Query)
- **Source**: [Zustand GitHub](https://github.com/pmndrs/zustand)

### 2.8 TanStack Query 5 (React Query)

- **Version**: 5.90.21 (latest stable)
- **Purpose**: Server state management - fetching, caching, synchronizing, and updating server data
- **Key Features**:
  - Automatic background refetching and cache invalidation
  - First-class Suspense support (`useSuspenseQuery`, `useSuspenseInfiniteQuery`)
  - Optimistic updates with rollback
  - Infinite scroll/pagination built-in
  - Offline support with mutation queue (pairs with our PWA requirement)
  - ~20% smaller than v4
  - Framework-agnostic devtools with cache editing and light mode
  - Prefetch multiple pages at once for infinite queries
- **Why TanStack Query**:
  - Eliminates manual loading/error/data state management
  - Cache deduplication (multiple components using same query share one request)
  - Automatic retry, stale-while-revalidate, garbage collection
  - Mutation queue supports offline-first (critical for our PWA scenario S06)
  - Devtools for debugging cache state during development
- **Requires**: React 18+ (uses `useSyncExternalStore`)
- **Source**: [TanStack Query](https://tanstack.com/query/latest)

---

## 3. Testing Technologies

### 3.1 Backend Testing

| Tool | Version | Purpose |
|------|---------|---------|
| xUnit | Latest for .NET 10 | Unit + integration testing |
| FsCheck | 3.3.2 | Property-based testing |
| FsCheck.Xunit | 3.3.2 | xUnit integration |
| Microsoft.AspNetCore.Mvc.Testing | .NET 10 | Integration test host |
| Microsoft.EntityFrameworkCore.InMemory | .NET 10 | In-memory DB for tests |

### 3.2 Frontend Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Vitest | 3.x (Vite 7 compatible) | Unit + component testing |
| @testing-library/react | Latest | React component testing |
| fast-check | Latest | Property-based testing (JS) |
| MSW (Mock Service Worker) | Latest | API mocking in tests |

### 3.3 E2E Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Playwright | 1.58.0 (.NET) | Cross-browser E2E testing |
| @playwright/test | Latest (JS) | Frontend E2E via Node |

---

## 4. Infrastructure Technologies

### 4.1 Docker

- **Multi-stage builds**: Separate build/runtime stages for minimal images
- **Docker Compose**: Local development orchestration
- **Aspire integration**: Auto-generated Dockerfiles via `AddJavaScriptApp`

### 4.2 GitHub Actions

- **CI**: Build, test, lint on push/PR
- **CD**: Deploy on tag/release
- **Matrix builds**: .NET + Node parallel test execution
- **Caching**: NuGet + npm/pnpm cache for faster builds

### 4.3 Terraform + Azure

- **Provider**: `hashicorp/azurerm` (latest)
- **Target**: Azure Container Apps
- **Resources**:
  - Container App Environment
  - Container Apps (API, Frontend)
  - Azure SQL / SQLite (dev) migration path
  - Azure Container Registry
  - Azure Key Vault for secrets
  - Application Insights for telemetry
  - Azure Log Analytics workspace
- **Aspire Support**: Dashboard component via `azapi_resource`
- **Source**: [Terraform Azure Container Apps](https://registry.terraform.io/providers/hashicorp/azurerm/latest/docs/resources/container_app_environment)

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
| Playwright 1.58 | .NET 8+ | .NET 10, all major browsers |
| Terraform azurerm | Terraform 1.x | Azure Container Apps, latest API |

---

## 6. Version Lock Summary

These are the versions we will target for LemonDo MVP:

```
# Backend
dotnet: 10.0 LTS
aspire: 13.x
ef-core: 10.x
identity: 10.x
scalar: latest
fscheck: 3.3.x
xunit: latest
serilog: latest
opentelemetry-dotnet: latest

# Frontend
node: 23.x
vite: 7.x
react: 19.x
typescript: 5.x
tailwindcss: 4.x
shadcn-ui: latest
react-i18next: 16.x
i18next: 25.x
zustand: 5.x
@tanstack/react-query: 5.x
vite-plugin-pwa: latest
vitest: 3.x
playwright: latest

# Infrastructure
docker: latest
terraform: 1.x
azurerm-provider: latest
github-actions: latest
```
