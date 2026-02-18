# Backend Technologies

> **Source**: Extracted from docs/operations/research.md §1
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date**: 2026-02-13
> **Purpose**: Document the latest versions, capabilities, and compatibility of all backend technologies in the LemonDo stack.

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

**CP2 Usage**: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` in Infrastructure, `Microsoft.AspNetCore.Authentication.JwtBearer` in Api

**CP2 Architecture**: `ApplicationUser : IdentityUser<Guid>` in Infrastructure (EF-aware), `User` entity in Domain (pure). Deferred JWT bearer options via `AddOptions<JwtBearerOptions>().Configure<IOptions<JwtSettings>>()` for test compatibility.

**CP2 Gotcha**: Eager JWT config read in `Program.cs` runs before test factory config overrides → use deferred options pattern

**CP2 Security Hardening**: Refresh tokens moved from JSON response body to HttpOnly cookies (`SameSite=Strict`, `Path=/api/auth`, `Secure` in production). Access tokens returned in JSON body only, stored in JS memory (Zustand, no persistence). Silent refresh on page load via cookie. Background `RefreshTokenCleanupService` (hosted service, 6h interval) prevents unbounded table growth.

**CP4 Identity/Domain Separation**: `ApplicationUser` stripped to credential shell (no custom properties). Domain `User` entity now persisted to separate `Users` table with shadow properties (`EmailHash`, `EncryptedEmail`, `EncryptedDisplayName`). Identity's `UserName` field repurposed to store SHA-256 email hash for login lookups via `FindByNameAsync(emailHash)`. `IAuthService` interface split into credential-focused methods (`CreateCredentialsAsync`, `AuthenticateAsync`, `GenerateTokensAsync`). `IUserRepository` handles transparent protected data encryption during `AddAsync(user, email, displayName)`. Admin search now requires exact email (hash match) or partial redacted display name.

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

### 1.9 WebPush (NuGet)

- **Version**: 2.0.4
- **Purpose**: VAPID-based Web Push notifications from .NET backend
- **Features**: Send push messages to browser push subscriptions, VAPID authentication, payload encryption
- **Used By**: `DueDateReminderService`, `WebPushService` in Infrastructure layer
- **Added**: CP5 (Notification system)
