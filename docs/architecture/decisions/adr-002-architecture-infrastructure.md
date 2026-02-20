# ADR-002: Architecture & Infrastructure

> **Source**: Extracted from docs/architecture/decisions/trade-offs.md §Technology Choices, §Bounded Context Architecture, §Scalability Considerations
> **Status**: Active
> **Last Updated**: 2026-02-18

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

## Bounded Context Architecture

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Context split** | Separate Task context (upstream) + Board context (downstream) | Single "Task Management" bounded context | Task carried board responsibilities (ColumnId, Position); splitting gives each context clear ownership and independent evolution |
| **Spatial placement** | Board owns `TaskCard` (TaskId + ColumnId + Rank) | Task stores ColumnId and Position directly | Which column a task sits in is a board layout concern, not a task identity concern; separation follows DDD aggregate boundaries |
| **Context relationship** | Conformist (Board imports TaskId/TaskStatus from Task) | Anti-corruption layer or shared kernel | Both contexts live in the same process; direct dependency is simpler and appropriate for a monolith |
| **Cross-context coordination** | Application-layer handlers orchestrate both aggregates | Domain-level coupling between contexts | Handlers coordinate CreateTask = Task.Create + board.PlaceTask, keeping domain layers independent |
| **Entity naming** | `Task` (with `using TaskEntity = ...` alias for collisions) | `BoardTask` or `TaskItem` | Tasks exist independently of boards; the name should reflect that; alias handles System.Threading.Tasks.Task collision |

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
