# LemonDo - Trade-offs and Assumptions

> **Date**: 2026-02-15
> **Status**: Active
> **Purpose**: Key assumptions, trade-offs, and scalability considerations behind LemonDo's architecture and technology choices.

---

## Assumptions

- **Single developer, time-boxed**: The project is designed for incremental delivery with meaningful checkpoints, not waterfall completion.
- **SQLite is sufficient for MVP**: Our data model is simple (tasks, boards, users). The repository pattern makes swapping to PostgreSQL a one-file change when scaling requires it.
- **Evaluators have .NET 10 SDK + Node.js 23+**: We target the latest LTS runtime. Aspire handles service orchestration so `dotnet run` starts everything.
- **Browser-first, not native**: We chose PWA over native apps. Service workers provide offline support without app store distribution.

---

## Key Trade-offs

### Planning & Delivery

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Delivery strategy** | 5 incremental checkpoints, each a runnable app | Build everything at once | If we stop at any checkpoint, we have something presentable; proves extensibility without over-building |
| **Auth timing** | Tasks first (CP1), auth second (CP2) | Auth-gated MVP from day one | Demonstrates architecture faster; adding user-scoping is a one-line repository change |
| **HIPAA** | Technical controls ("HIPAA-Ready") | Full certification | Certification requires legal/BAA framework beyond code scope |
| **Bounded contexts** | All 6 designed, 2-4 implemented per checkpoint | Implement all at once | Incremental delivery proves extensibility without over-building |
| **Quick-add as P0** | Title-only task creation (one tap) | Requiring title + description | Scenario analysis showed users create tasks in 2-second bursts; minimal friction is the killer feature |

### Technology Choices

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Database** | SQLite | PostgreSQL/SQL Server | Zero-config for evaluators; repository pattern means swap is trivial |
| **Test framework** | MSTest 4 + MTP | xUnit v3 | xUnit v3 had a .NET 10 "catastrophic failure" bug (#3413), hardcoded net8.0 targets; MSTest is first-party with same-day .NET compatibility |
| **State management** | Zustand + TanStack Query | Redux, React Context | Smaller bundle, no providers needed, natural server/client split |
| **API docs** | Scalar | Swagger UI | Faster, better search, dark mode, .NET 9+ default |
| **Offline strategy** | Read-only first, CRUD later | Full offline from day one | Incremental complexity; read-only covers 80% of offline scenarios |
| **i18n** | English-first, add languages later | Multi-language from start | i18next is cheap to retrofit; translation files are additive |

### Domain Design

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Column-Status relationship** | Column determines status (one source of truth) | Independent status enum and column position | Two sources of truth always desync eventually; making column authoritative eliminated an entire class of bugs |
| **ColumnRole enum** | Direct `TargetStatus: TaskStatus` on Column | Separate ColumnRole enum | ColumnRole was a 1:1 mapping to TaskStatus; direct usage is clearer than an indirection layer |
| **Archive semantics** | `IsArchived` bool flag, orthogonal to status | `Archived` as a TaskStatus enum value | Visibility flags (archive, soft-delete) are orthogonal to entity lifecycle; mixing them into the status enum creates invalid state combinations |
| **Archive guard** | Any task can be archived regardless of status | Only Done tasks can be archived | Archiving is organizational (visibility), not lifecycle; a stale Todo or abandoned InProgress should be archivable |
| **Task lifecycle methods** | `SetStatus()` + `Complete()`/`Uncomplete()` convenience methods | `MoveTo()` as single source of truth | After bounded context split, Task owns its own status directly; Board owns spatial placement separately |

### Bounded Context Architecture

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Context split** | Separate Task context (upstream) + Board context (downstream) | Single "Task Management" bounded context | Task carried board responsibilities (ColumnId, Position); splitting gives each context clear ownership and independent evolution |
| **Spatial placement** | Board owns `TaskCard` (TaskId + ColumnId + Rank) | Task stores ColumnId and Position directly | Which column a task sits in is a board layout concern, not a task identity concern; separation follows DDD aggregate boundaries |
| **Context relationship** | Conformist (Board imports TaskId/TaskStatus from Task) | Anti-corruption layer or shared kernel | We own both contexts in the same process; direct dependency is simpler and appropriate for a monolith |
| **Cross-context coordination** | Application-layer handlers orchestrate both aggregates | Domain-level coupling between contexts | Handlers coordinate CreateTask = Task.Create + board.PlaceTask, keeping domain layers independent |
| **Entity naming** | `Task` (with `using TaskEntity = ...` alias for collisions) | `BoardTask` or `TaskItem` | Tasks exist independently of boards; the name should reflect that; alias handles System.Threading.Tasks.Task collision |

### Authentication & Security

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Token storage** | HttpOnly cookie (refresh) + JS memory (access) | localStorage for both tokens | XSS can read localStorage but not HttpOnly cookies; memory-only access token is invisible to injected scripts |
| **Cookie scope** | `SameSite=Strict`, `Path=/api/auth` | `SameSite=Lax` or broader path | Strict + narrow path means cookie is only sent on same-site requests to auth endpoints; eliminates CSRF without CSRF tokens |
| **CSRF protection** | None (SameSite=Strict is sufficient) | Explicit CSRF tokens | SameSite=Strict prevents cross-origin cookie transmission; path-scoping prevents same-origin leakage to non-auth endpoints; adding CSRF tokens would be redundant complexity |
| **Session restoration** | Silent refresh on page load via cookie | Persisted access token in sessionStorage | sessionStorage is also XSS-readable; silent refresh adds ~100ms on page load but eliminates all client-side token storage |
| **Zustand persistence** | Removed entirely (memory-only store) | localStorage persistence with `skipHydration` workaround | Eliminating persist also eliminated the Zustand 5 + React 19 hydration race condition; simpler code, better security |
| **PII in logs** | Masked emails (`u***@example.com`) | Full emails for easier debugging | PII in logs violates GDPR/HIPAA; masked format preserves enough info for debugging (first char + domain) |
| **Token family detection** | Deferred | Detect stolen refresh token reuse | Requires DB migration (FamilyId column) and complex revocation logic; current single-device model limits attack surface |
| **HaveIBeenPwned check** | Deferred | Reject breached passwords on registration | External API dependency needs graceful degradation; can be added independently later |
| **Refresh token cleanup** | Background service (every 6 hours) | Manual cleanup or no cleanup | Prevents unbounded table growth; 6h interval balances DB load vs staleness |

### Card Ordering & API Design

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Ordering strategy** | Sparse decimal ranks (1000, 2000, 3000; midpoint inserts) | Dense integers, LexoRank strings, linked list pointers, CRDT | Simplest strategy that eliminates the position-collision bug class; only updates one row per move; decimal avoids float precision drift; LexoRank adds unnecessary complexity at our scale |
| **Move API contract** | Neighbor card IDs (`previousTaskId`/`nextTaskId`) | Frontend sends array index or rank directly | Intent-based ("place between these two cards") survives backend strategy changes; frontend stays dumb, backend avoids read-to-sort, API contract is unambiguous |
| **Orphan cleanup** | Delete removes card; Archive preserves card on board | Symmetric handling (both remove or both preserve) | Asymmetric by intent: deletion is destructive with no undelete, so card is removed; archive is reversible, so card stays for rank restoration on unarchive |
| **Orphan filtering** | Board query handlers filter out archived/deleted task cards at read time | Eager cleanup on every archive/delete | Preserves archived card placement in the database while presenting clean data to the frontend; read-layer filtering is cheaper than write-layer coordination |
| **E2E test isolation** | Unique user per describe block (timestamp + counter email) | Shared user + `deleteAllTasks()` cleanup between tests | Fresh users = true data isolation with zero cleanup overhead; each describe block operates on an empty board; eliminates shared auth state and token rotation conflicts |
| **E2E test execution** | `test.describe.serial` with shared page/context | Parallel execution with per-test browser context | Tests accumulate state like real users; login once in `beforeAll` instead of per test; 3x faster (20s vs 60-90s), 100% stable |

---

## Scalability Considerations

### What scales today

- Repository pattern decouples data access from domain logic - swap SQLite to PostgreSQL with no domain changes
- TanStack Query handles caching, deduplication, and background refetch - adding pagination is configuration, not rewrite
- Aspire orchestration generates Kubernetes manifests - deployment scales via `aspire do`
- Domain events decouple mutations from side effects - adding audit logging, notifications, or analytics is event subscription, not code modification

### What we'd add for production scale

- **Database migration**: PostgreSQL with connection pooling (PgBouncer) behind the repository interface
- **Caching layer**: Redis for session storage and query caching (Aspire has built-in Redis integration)
- **CDN**: Static assets served via Azure CDN or CloudFront
- **Search**: Elasticsearch for full-text task search (replace in-memory filtering)
- **Queue**: Azure Service Bus for async domain event processing (replace in-process event dispatch)
- **Monitoring**: Grafana dashboards consuming OpenTelemetry data, PagerDuty alerting
- **Multi-tenancy**: Organization-scoped data isolation (the Board aggregate root naturally supports this)
