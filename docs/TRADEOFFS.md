# LemonDo - Trade-offs and Assumptions

> **Date**: 2026-02-14
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

| Trade-off | What we chose | What we gave up | Why |
|---|---|---|---|
| **Auth timing** | Tasks first (CP1), auth second (CP2) | Auth-gated MVP from day one | Demonstrates architecture faster; adding user-scoping is a one-line repository change |
| **Database** | SQLite | PostgreSQL/SQL Server | Zero-config for evaluators; repository pattern means swap is trivial |
| **HIPAA** | Technical controls ("HIPAA-Ready") | Full certification | Certification requires legal/BAA framework beyond code scope |
| **State management** | Zustand + TanStack Query | Redux, React Context | Smaller bundle, no providers needed, natural server/client split |
| **API docs** | Scalar | Swagger UI | Faster, better search, dark mode, .NET 9+ default |
| **Offline strategy** | Read-only first, CRUD later | Full offline from day one | Incremental complexity; read-only covers 80% of offline scenarios |
| **i18n** | English-first, add languages later | Multi-language from start | i18next is cheap to retrofit; translation files are additive |
| **Bounded contexts** | All 6 designed, 2-4 implemented per checkpoint | Implement all at once | Incremental delivery proves extensibility without over-building |

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
