# LemonDo - Technology Research

> **Date**: 2026-02-13
> **Status**: Active
> **Purpose**: Document the latest versions, capabilities, and compatibility of all technologies in our stack.

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
- **CP2 Usage**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` in Infrastructure, `Microsoft.AspNetCore.Authentication.JwtBearer` in Api
- **CP2 Architecture**: `ApplicationUser : IdentityUser<Guid>` in Infrastructure (EF-aware), `User` entity in Domain (pure). Deferred JWT bearer options via `AddOptions<JwtBearerOptions>().Configure<IOptions<JwtSettings>>()` for test compatibility.
- **CP2 Gotcha**: Eager JWT config read in `Program.cs` runs before test factory config overrides → use deferred options pattern
- **CP2 Security Hardening**: Refresh tokens moved from JSON response body to HttpOnly cookies (`SameSite=Strict`, `Path=/api/auth`, `Secure` in production). Access tokens returned in JSON body only, stored in JS memory (Zustand, no persistence). Silent refresh on page load via cookie. Background `RefreshTokenCleanupService` (hosted service, 6h interval) prevents unbounded table growth.

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

### 1.6 MSTest 4 + Microsoft.Testing.Platform (MTP)

- **Version**: MSTest 4.0.1 (latest stable)
- **Purpose**: First-party .NET testing framework by Microsoft
- **Key Advantages**:
  - Built-in `dotnet new mstest` template auto-targets the installed SDK (net10.0 with zero friction)
  - Guaranteed same-day compatibility with every .NET release (first-party maintenance)
  - Microsoft.Testing.Platform (MTP) replaces VSTest as the modern test runner
  - `[TestClass]`, `[TestMethod]`, `[DataRow]` attributes for clear test structure
  - Parallel test execution, test filtering, and rich IDE integration
  - Requires `<EnableMSTestRunner>true</EnableMSTestRunner>` + `<OutputType>Exe</OutputType>` for MTP mode
- **Why MSTest over xUnit v3**: xUnit v3 templates default to net8.0 (not auto-detecting SDK version), and have an active .NET 10 compatibility bug (GitHub issue #3413 - "catastrophic failure" in CI). MSTest is Microsoft-maintained with zero-friction .NET 10 support.
- **Source**: [Microsoft Learn - MSTest](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-intro)

### 1.7 FsCheck 3.3.2

- **Version**: 3.3.2 (latest stable)
- **Purpose**: Property-based testing for .NET
- **Features**:
  - Random structured input generation
  - Automatic shrinking to minimal failing case
  - Core API works with any test framework (MSTest, xUnit, NUnit)
  - Custom generators and arbitraries
  - Stateful testing support
- **Usage**: Core `FsCheck` package with `Prop.ForAll` / `Check.Quick` in any `[TestMethod]`
- **Source**: [FsCheck GitHub](https://github.com/fscheck/FsCheck)

### 1.8 OpenTelemetry for .NET

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
- **CP2 Update**: Auth store no longer uses `persist` middleware. Tokens stored in memory only (HttpOnly cookie for refresh, JS variable for access). This eliminated the Zustand 5 + React 19 `useSyncExternalStore` hydration race condition entirely.
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

### 2.9 OpenTelemetry Browser SDK (Frontend Telemetry)

- **Purpose**: Distributed tracing and performance monitoring for the browser, connected to the same OTel pipeline as the backend
- **Key Packages**:
  - `@opentelemetry/sdk-trace-web` (2.x) - Browser-specific trace provider
  - `@opentelemetry/instrumentation-fetch` - Auto-instruments all fetch calls (TanStack Query uses fetch under the hood)
  - `@opentelemetry/instrumentation-document-load` - Page load performance spans
  - `@opentelemetry/exporter-trace-otlp-http` - Exports traces via OTLP HTTP to Aspire Dashboard
  - `@opentelemetry/context-zone` - Zone.js-based context propagation for browser async operations
  - `@opentelemetry/resources` - Resource identification (service name, version)
- **Distributed Tracing**: W3C `traceparent` header propagation via `propagateTraceHeaderCorsUrls` in fetch instrumentation. Frontend spans connect to backend spans - one trace from button click → fetch → API → DB.
- **Aspire Integration**: Aspire Dashboard exposes OTLP HTTP endpoint (port 4318) with CORS support for browser apps. When launched via `AddJavaScriptApp`, the endpoint URL and API key are available via `OTEL_EXPORTER_OTLP_ENDPOINT` and `OTEL_EXPORTER_OTLP_HEADERS` environment variables.
- **What It Captures**: Fetch/XHR request timing with response status, page load performance (Navigation Timing API), user interaction spans (clicks, inputs), custom spans for business operations (task creation, board switch), errors and exceptions with stack traces.
- **What It Doesn't Do** (vs Sentry): No source map processing, no session replays, no issue grouping/deduplication, no release tracking. For production, Sentry can be added alongside OTel - Sentry SDK v8+ supports OTLP export, so traces flow to both Sentry and the OTel collector.
- **Phasing**: CP4 introduces OTel Browser SDK for unified observability. Sentry is a future production enhancement (Tier 9).
- **Source**: [OpenTelemetry JS Browser Getting Started](https://opentelemetry.io/docs/languages/js/getting-started/browser/), [Aspire: Enable Browser Telemetry](https://aspire.dev/dashboard/enable-browser-telemetry/)

---

## 3. Testing Technologies

### 3.1 Backend Testing

| Tool | Version | Purpose |
|------|---------|---------|
| MSTest 4 + MTP | 4.0.1 | Unit + integration testing (first-party) |
| FsCheck | 3.3.2 | Property-based testing (core API) |
| Microsoft.AspNetCore.Mvc.Testing | .NET 10 | Integration test host |
| Microsoft.EntityFrameworkCore.InMemory | .NET 10 | In-memory DB for tests |

### 3.2 Frontend Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Vitest | 4.x (Vite 7 compatible) | Unit + component testing |
| @testing-library/react | 16.x | React component testing |
| fast-check | 4.x | Property-based testing (JS) |
| MSW (Mock Service Worker) | 2.x | API mocking in tests |

### 3.3 E2E & Cross-Browser Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Playwright | 1.58.0 (.NET) | Cross-browser E2E testing |
| @playwright/test | 1.x | Frontend E2E via Node |
| BrowserStack | Cloud service | Real device/browser testing |

**Cross-Browser Testing Strategy**:

Playwright natively supports three rendering engines with a single API:
- **Chromium** - Chrome, Edge, Opera, Brave, and all Chromium-based browsers
- **Firefox** - Gecko engine
- **WebKit** - Safari engine (derived from latest WebKit trunk, often ahead of shipping Safari)

Playwright projects configuration runs the same test suite against all three engines in CI. This covers the vast majority of desktop browser rendering differences.

**Device Emulation**:

Playwright includes predefined device descriptors (iPhone 14, Pixel 7, iPad, etc.) that configure viewport, user agent, touch events, and device scale factor. This validates:
- Responsive breakpoints (mobile, tablet, desktop)
- Touch interaction behavior
- Viewport-dependent layout and overflow

**Limitation**: Emulation is not real-device testing. It simulates viewport and user agent but runs in the same desktop engine. Real Safari on iOS has rendering quirks that WebKit emulation may not catch.

**Real Device Testing (Production)**:

BrowserStack provides 3500+ real browsers and devices in the cloud. Playwright tests can run directly on BrowserStack via their integration - same test code, real hardware:
- Real iOS Safari on physical iPhones/iPads
- Real Android Chrome on physical devices
- Older browser versions (Safari 15, Chrome 100, Firefox ESR)
- Real OS-level rendering (font smoothing, scrollbar behavior, safe areas)

**Phasing**:
- **CP5**: Playwright E2E with Chromium + Firefox + WebKit projects + device emulation (iPhone, iPad, Pixel). Covers ~95% of rendering scenarios.
- **Production**: BrowserStack for real device matrix. Run on every release candidate. Focus on iOS Safari (historically most quirky) and older Android versions.

### 3.4 Visual Regression Testing

| Tool | Version | Purpose |
|------|---------|---------|
| Playwright screenshots | Built-in | Baseline visual comparison |
| Percy (BrowserStack) | Cloud service | AI-powered cross-browser visual diffs |

**Strategy**:

- **CP5**: Playwright's built-in `toHaveScreenshot()` for baseline visual comparison. Captures screenshots during E2E runs, compares against committed baselines, fails on pixel-level drift. Free, no external service, works in CI.
- **Production**: Percy (by BrowserStack) or Chromatic for AI-powered visual regression. These render snapshots across real browser engines (not emulated), detect meaningful visual changes vs noise (anti-aliasing, font rendering), and provide team review workflows with approval flows.

**What Visual Regression Catches**:
- CSS regressions (margin collapse, flexbox/grid issues across browsers)
- Font rendering differences across OS (Windows ClearType vs macOS subpixel)
- Theme inconsistencies (dark mode colors, contrast ratios)
- Responsive breakpoint edge cases (content overflow, truncation)
- i18n layout issues (longer text in pt-BR/es breaking layouts)

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

- **Provider**: `hashicorp/azurerm` 4.x
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
| OTel Browser SDK | ES2022+ browsers | Aspire Dashboard (OTLP HTTP) |
| Playwright 1.58 | .NET 8+ | .NET 10, Chromium, Firefox, WebKit |
| Terraform azurerm | Terraform 1.x | Azure Container Apps, latest API |

---

## 6. Version Lock Summary

These are the versions we will target for LemonDo MVP:

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
opentelemetry-dotnet: 1.x

# Frontend
node: 23.x
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

# Testing
vitest: 4.x
fast-check: 4.x
@testing-library/react: 16.x
msw: 2.x
playwright: 1.x (E2E with cookie-based auth, unique users per describe block)

# Infrastructure
docker: 29.x
terraform: 1.x
azurerm-provider: 4.x
github-actions: N/A (platform, not versioned)
```
