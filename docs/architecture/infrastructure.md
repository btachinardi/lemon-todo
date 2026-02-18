# Infrastructure

> **Source**: Extracted from docs/TRADEOFFS.md (Planning & Delivery, Technology Choices, Card Ordering & API Design, Rich UX & Polish, API Contract & Type Safety, Testing & Quality, Offline & PWA, Scalability Considerations sections)
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Assignment Context

This project is a **lemon.io take-home assignment** for a Full Stack Developer position at a healthcare startup that detects early-stage cancers across multiple organs through a single MRI scan. The assignment asked for a todo/task management app with .NET/SQLite backend, React frontend, and "any features you would add for a real production MVP."

The prompt "add any features for a real production MVP" was interpreted through the lens of the target company's domain: regulated healthcare. This means compliance awareness (HIPAA patterns, audit trails, field encryption), offline reliability (hospital networks are unstable), Azure deployment (matching the company's infrastructure), and product analytics (an MVP exists to validate hypotheses). Every trade-off in this document flows from that interpretation.

---

## Assumptions

- **Single developer, time-boxed**: The project is designed for incremental delivery with meaningful checkpoints, not waterfall completion.
- **SQLite is sufficient for MVP**: The data model is simple (tasks, boards, users). The repository pattern makes swapping to PostgreSQL a one-file change when scaling requires it.
- **Evaluators have .NET 10 SDK + Node.js 23+**: The project targets the latest LTS runtime. Aspire handles service orchestration so `dotnet run` starts everything.
- **Browser-first, not native**: LemonDo uses PWA over native apps. Service workers provide offline support without app store distribution.

---

## Planning & Delivery Trade-offs

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Delivery strategy** | 5 incremental checkpoints, each a runnable app | Build everything at once | If delivery stops at any checkpoint, there is something presentable; proves extensibility without over-building |
| **Auth timing** | Tasks first (CP1), auth second (CP2) | Auth-gated MVP from day one | Demonstrates architecture faster; adding user-scoping is a one-line repository change |
| **HIPAA** | Technical controls ("HIPAA-Ready") | Full certification | Certification requires legal/BAA framework beyond code scope |
| **Bounded contexts** | All 6 designed, 2-4 implemented per checkpoint | Implement all at once | Incremental delivery proves extensibility without over-building |
| **Quick-add as P0** | Title-only task creation (one tap) | Requiring title + description | Scenario analysis showed users create tasks in 2-second bursts; minimal friction is the killer feature |

---

## Technology Choices

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Database** | SQLite | PostgreSQL/SQL Server | Zero-config for evaluators; repository pattern means swap is trivial |
| **Test framework** | MSTest 4 + MTP | xUnit v3 | xUnit v3 had a .NET 10 "catastrophic failure" bug (#3413), hardcoded net8.0 targets; MSTest is first-party with same-day .NET compatibility |
| **State management** | Zustand + TanStack Query | Redux, React Context | Smaller bundle, no providers needed, natural server/client split |
| **API docs** | Scalar | Swagger UI | Faster, better search, dark mode, .NET 9+ default |
| **Offline strategy** | Read-only first, CRUD later | Full offline from day one | Incremental complexity; read-only covers 80% of offline scenarios |
| **i18n** | English-first, add languages later | Multi-language from start | i18next is cheap to retrofit; translation files are additive |

---

## Card Ordering & API Design

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Ordering strategy** | Sparse decimal ranks (1000, 2000, 3000; midpoint inserts) | Dense integers, LexoRank strings, linked list pointers, CRDT | Simplest strategy that eliminates the position-collision bug class; only updates one row per move; decimal avoids float precision drift; LexoRank adds unnecessary complexity at this scale |
| **Move API contract** | Neighbor card IDs (`previousTaskId`/`nextTaskId`) | Frontend sends array index or rank directly | Intent-based ("place between these two cards") survives backend strategy changes; frontend stays dumb, backend avoids read-to-sort, API contract is unambiguous |
| **Orphan cleanup** | Delete removes card; Archive preserves card on board | Symmetric handling (both remove or both preserve) | Asymmetric by intent: deletion is destructive with no undelete, so card is removed; archive is reversible, so card stays for rank restoration on unarchive |
| **Orphan filtering** | Board query handlers filter out archived/deleted task cards at read time | Eager cleanup on every archive/delete | Preserves archived card placement in the database while presenting clean data to the frontend; read-layer filtering is cheaper than write-layer coordination |
| **E2E test isolation** | Unique user per describe block (timestamp + counter email) | Shared user + `deleteAllTasks()` cleanup between tests | Fresh users = true data isolation with zero cleanup overhead; each describe block operates on an empty board; eliminates shared auth state and token rotation conflicts |
| **E2E test execution** | `test.describe.serial` with shared page/context | Parallel execution with per-test browser context | Tests accumulate state like real users; login once in `beforeAll` instead of per test; 3x faster (20s vs 60-90s), 100% stable |

---

## Rich UX & Polish

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Drag-and-drop library** | @dnd-kit (modular, actively maintained) | react-beautiful-dnd | rbd is unmaintained (last release 2021); @dnd-kit is modular, supports touch/keyboard, has first-class React 19 support |
| **Task detail panel** | Slide-over Sheet | Modal Dialog | Sheet keeps board/list visible in background, provides spatial context, supports mobile swipe-to-dismiss; modals feel heavier and block the view |
| **Filtering strategy** | Client-side filtering + backend query params | Server-side only or client-side only | Client-side gives instant UX (no round-trip); backend params ready for future pagination/scale; dual approach is fast today and scalable tomorrow |
| **Theme persistence** | Separate Zustand store with `persist` | Single store or CSS-only | Auth store deliberately avoids `persist` (security); theme is non-sensitive UI state that should survive refresh; separate store keeps concerns isolated |
| **Error boundary scope** | Per-route `errorElement` | Single global error boundary | Route-level granularity contains failures to the affected route; global boundary unmounts entire app on any error |
| **Date library** | date-fns 4 (tree-shakeable, functional) | dayjs, moment, Temporal API | Tree-shakeable (only import what is used), no global mutation; Temporal API not yet universally available |
| **Calendar component** | react-day-picker 9 (Shadcn default) | Custom calendar or alternative library | Shadcn Calendar is built on react-day-picker; using the standard primitive avoids custom styling work |
| **Loading states** | Dedicated skeleton components | Loading spinners or `isLoading` prop on components | Skeletons mirror loaded layout exactly, preventing layout shift; separate components keep loaded components clean |
| **Empty states** | Dedicated empty state components with CTAs | Inline conditional text | Dedicated components provide better UX with illustrations and actionable CTAs; reusable across views |
| **Bundle size** | Single chunk (691 KB JS) | Code-splitting with dynamic imports | All CP3 features are used on every page load; code-splitting deferred to CP4/CP5 when the admin panel is added and other lazy-loaded routes |

---

## API Contract & Type Safety

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Type generation strategy** | Build-time OpenAPI spec via `Microsoft.Extensions.ApiDescription.Server` | Runtime spec export or manual spec authoring | Build-time catches spec changes in CI before deployment; manual authoring drifts from implementation; runtime export requires a running server |
| **Schema enrichment** | Document transformer adds enum values to string-typed properties | `[JsonStringEnumConverter]` on all DTOs or enum-typed DTO properties | DTOs intentionally use `string` for priority/status (mapper flexibility); converter attribute would leak serialization concerns into DTOs; transformer enriches at spec generation time without touching domain code |
| **Type migration scope** | Selective: derive enums from schema, keep interfaces hand-written | Wholesale replacement of all types with generated re-exports | Generated types have `number \| string` for int32 fields and `optional` where frontend expects `required+nullable`; fixing these globally would break 400+ tests for minimal gain; enum drift is the actual bug class |
| **Enum const guard** | `satisfies { [K in SchemaType]: K }` on const objects | Unused type-level assertions or runtime checks | `satisfies` produces a real compile error (not just a type resolving to `never`); no unused warnings; no runtime overhead; fails exactly when a backend enum value is missing from the const |
| **Spec artifact** | `openapi.json` committed to git, `schema.d.ts` gitignored | Both committed or both gitignored | Spec is the contract artifact (reviewable in PRs); generated types are derived and deterministic — committing them creates merge noise; CI regenerates types from the committed spec |
| **Translation guard** | Vitest test importing `openapi.json` directly | TypeScript compile-time check or runtime i18next fallback key detection | Tests produce clear failure messages ("es is missing audit action translations: NewAction"); compile-time can't check JSON translation files; runtime detection only fires when the UI renders that specific path |

---

## Testing & Quality

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Property-based testing** | FsCheck (.NET) + fast-check (TypeScript) on both stacks | Example-based tests only | Property tests find edge cases humans miss; `Prop.ForAll` validates entire input spaces, not just known examples; value object invariants are ideal property-test targets |
| **Dual-database CI** | SQLite + SQL Server in parallel on every push | SQLite-only in CI, SQL Server manual/optional | Catches provider-specific SQL differences (e.g., DateTimeOffset ordering) before production; parallel execution adds minimal CI time |
| **Visual regression baselines** | 26 Playwright screenshots across themes and viewports | Manual visual QA or no regression checks | Automated pixel comparison catches unintended UI changes that unit tests miss; light/dark + desktop/mobile matrix covers the key permutations |
| **WCAG contrast testing** | Automated contrast ratio checks against WCAG 2.1 AA | Manual contrast checking or browser devtools | Programmatic verification ensures every color pairing meets thresholds; runs on every PR, not just when someone remembers to check |
| **E2E data isolation** | Unique user per describe block (timestamp + counter email) | Shared user + deleteAllTasks() cleanup | Fresh users = true isolation with zero cleanup overhead; eliminates flaky tests from shared state and token rotation conflicts |
| **E2E execution model** | `test.describe.serial` with shared page/context | Parallel execution with per-test browser context | Tests accumulate state like real users; login once in beforeAll; 3x faster (20s vs 60-90s), stable |

---

## Offline & PWA

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Offline mutation queue** | IndexedDB via idb-keyval, replayed on reconnect | localStorage queue or no offline writes | IndexedDB handles large payloads and survives browser crashes; idb-keyval provides a simple key-value API; queue replays mutations in order to maintain consistency |
| **Service worker strategy** | Workbox with stale-while-revalidate for API, cache-first for assets | Custom service worker or network-only | Workbox is maintained by Google Chrome team; stale-while-revalidate gives instant reads from cache while updating in background; cache-first for static assets eliminates redundant network requests |
| **Startup drain** | Check navigator.onLine at startup, drain queue immediately | Only drain on 'online' event | 'online' event only fires on transitions, not initial load; startup check handles the common case of refreshing while already online |
| **Cache invalidation after drain** | CustomEvent dispatched from Zustand store, caught by QueryProvider | Direct queryClient import in non-React code | Event-based bridge keeps Zustand stores decoupled from React-specific systems (TanStack Query); zero coupling, testable pub/sub pattern |
| **StrictMode double-fire protection** | useRef<Promise> to share fetch between StrictMode mounts | AbortController in useEffect cleanup | AbortController doesn't prevent server-side mutations — refresh token rotation is non-idempotent; useRef shares the promise so the fetch fires exactly once |

---

## Scalability Considerations

### What scales today

- Repository pattern decouples data access from domain logic - swap SQLite to PostgreSQL with no domain changes
- TanStack Query handles caching, deduplication, and background refetch - adding pagination is configuration, not rewrite
- Aspire orchestration generates Kubernetes manifests - deployment scales via `aspire do`
- Domain events decouple mutations from side effects - adding audit logging, notifications, or analytics is event subscription, not code modification

### What production scale would require

- **Database migration**: PostgreSQL with connection pooling (PgBouncer) behind the repository interface
- **Caching layer**: Redis for session storage and query caching (Aspire has built-in Redis integration)
- **CDN**: Static assets served via Azure CDN or CloudFront
- **Search**: Elasticsearch for full-text task search (replace in-memory filtering)
- **Queue**: Azure Service Bus for async domain event processing (replace in-process event dispatch)
- **Monitoring**: Grafana dashboards consuming OpenTelemetry data, PagerDuty alerting
- **Multi-tenancy**: Organization-scoped data isolation (the Board aggregate root naturally supports this)
