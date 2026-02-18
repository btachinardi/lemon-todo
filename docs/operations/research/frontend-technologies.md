# Frontend Technologies

> **Source**: Extracted from docs/operations/research.md §2
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date**: 2026-02-13
> **Purpose**: Document the latest versions, capabilities, and compatibility of all frontend technologies in the LemonDo stack.

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
  - Offline support with mutation queue (pairs with the PWA requirement)
  - ~20% smaller than v4
  - Framework-agnostic devtools with cache editing and light mode
  - Prefetch multiple pages at once for infinite queries
- **Why TanStack Query**:
  - Eliminates manual loading/error/data state management
  - Cache deduplication (multiple components using same query share one request)
  - Automatic retry, stale-while-revalidate, garbage collection
  - Mutation queue supports offline-first (critical for PWA scenario S06)
  - Devtools for debugging cache state during development
- **Requires**: React 18+ (uses `useSyncExternalStore`)
- **Source**: [TanStack Query](https://tanstack.com/query/latest)

### 2.9 date-fns 4

- **Version**: 4.1.0
- **Purpose**: Date formatting and manipulation for task due dates
- **Key Advantages**:
  - Tree-shakeable: only imports what you use (unlike moment.js which bundles everything)
  - Pure functional API: no global mutation, immutable operations
  - Excellent TypeScript support with strict types
  - Smaller bundle impact than dayjs for selective imports
- **CP3 Usage**: `format()`, `formatDistanceToNow()` for DueDateLabel and TaskDetailSheet
- **Source**: [date-fns.org](https://date-fns.org/)

### 2.10 react-day-picker 9

- **Version**: 9.13.2
- **Purpose**: Calendar widget for date selection
- **Key Advantages**:
  - Default calendar primitive used by Shadcn/ui's Calendar component
  - Accessible (ARIA roles, keyboard navigation)
  - Customizable with Tailwind CSS classes
  - Works with date-fns for locale and formatting
- **CP3 Usage**: Due date picker inside TaskDetailSheet via Shadcn Calendar + Popover
- **Source**: [react-day-picker](https://react-day-picker.js.org/)

### 2.11 OpenTelemetry Browser SDK (Frontend Telemetry)

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
