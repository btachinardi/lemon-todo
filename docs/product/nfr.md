# Non-Functional Requirements

> **Source**: Extracted from docs/PRD.draft.md §3, docs/PRD.md §2, docs/PRD.2.draft.md §9,
>             docs/domain/contexts/agents.md, docs/domain/contexts/bridges/project-agent-bridge.md,
>             docs/domain/contexts/comms.md, docs/domain/contexts/projects.md,
>             docs/operations/research/redis-streams.md
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## NFR-001: Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001.1 | API response time (p95) | < 200ms |
| NFR-001.2 | Frontend First Contentful Paint | < 1.5s |
| NFR-001.3 | Frontend Time to Interactive | < 3.0s |
| NFR-001.4 | Lighthouse Performance score | > 90 |
| NFR-001.5 | API throughput | > 1000 req/s |

---

## NFR-002: Responsive Design

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-002.1 | Mobile viewport (320px - 768px) | Full functionality |
| NFR-002.2 | Tablet viewport (768px - 1024px) | Full functionality |
| NFR-002.3 | Desktop viewport (1024px+) | Full functionality |
| NFR-002.4 | Touch-friendly tap targets | >= 44px |
| NFR-002.5 | Kanban horizontal scroll on mobile | Native gesture support |
| NFR-002.6 | Quick-add accessible via floating action button on mobile | Always visible |
| NFR-002.7 | Kanban columns scroll horizontally on mobile with snap | Native gesture |

---

## NFR-003: Progressive Web App

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-003.1 | Installable on mobile/desktop | PWA manifest |
| NFR-003.2 | Offline task viewing AND creation AND completion | Full offline CRUD |
| NFR-003.3 | Background sync for offline changes | Workbox |
| NFR-003.4 | Push notification support | Web Push API |
| NFR-003.5 | Offline change indicator on affected tasks | Visual sync status |
| NFR-003.6 | Automatic sync on reconnection with conflict resolution | Last-write-wins |

**Rationale**: Scenario S06 (airplane) shows offline must support full task lifecycle, not just viewing.

---

## NFR-004: API Documentation

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-004.1 | OpenAPI 3.1 specification | Auto-generated |
| NFR-004.2 | Scalar API reference UI | /scalar endpoint |
| NFR-004.3 | Interactive request testing | Built-in |
| NFR-004.4 | Authentication flow documentation | Included |

---

## NFR-005: Observability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-005.1 | Structured logging (backend) | Serilog + OTLP |
| NFR-005.2 | Distributed tracing | OpenTelemetry |
| NFR-005.3 | Metrics collection | Prometheus-compatible |
| NFR-005.4 | Frontend error tracking | OpenTelemetry browser |
| NFR-005.5 | Aspire Dashboard integration | Built-in |
| NFR-005.6 | Health check endpoints | /health, /ready |

---

## NFR-006: CI/CD

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-006.1 | Automated build on push | GitHub Actions |
| NFR-006.2 | Automated test suite execution | All test types |
| NFR-006.3 | Docker image building | Multi-stage builds |
| NFR-006.4 | Deployment to staging on PR merge | Automated |
| NFR-006.5 | Production deployment on release tag | Manual trigger |

---

## NFR-007: UI/UX

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-007.1 | Light and dark theme | System preference detection |
| NFR-007.2 | Consistent design system | Shadcn/ui + Radix |
| NFR-007.3 | WCAG 2.1 AA accessibility | Minimum standard |
| NFR-007.4 | Smooth animations and transitions | 60fps |
| NFR-007.5 | Loading states and skeletons | All async operations |
| NFR-007.6 | Error states with recovery actions | All failure points |

---

## NFR-008: Internationalization

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-008.1 | Frontend i18n with react-i18next | All user-facing strings |
| NFR-008.2 | Backend i18n for API messages | All error/validation messages |
| NFR-008.3 | Initial languages: English, Portuguese, Spanish | MVP |
| NFR-008.4 | RTL layout support | Infrastructure ready |
| NFR-008.5 | Date/number/currency localization | Locale-aware |

---

## NFR-009: Containerization & Deployment

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-009.1 | Docker multi-stage builds | API + Frontend |
| NFR-009.2 | Docker Compose for local dev | Full stack |
| NFR-009.3 | Terraform Azure configuration | Container Apps |
| NFR-009.4 | Aspire orchestration | Local + cloud |
| NFR-009.5 | Environment-based configuration | Dev, Staging, Prod |

---

## NFR-010: Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-010.1 | OWASP Top 10 compliance | All categories |
| NFR-010.2 | HTTPS everywhere | Enforced |
| NFR-010.3 | CORS properly configured | Origin whitelist |
| NFR-010.4 | Rate limiting | Per-endpoint |
| NFR-010.5 | Input validation | All endpoints |
| NFR-010.6 | SQL injection prevention | Parameterized queries |
| NFR-010.7 | XSS prevention | Content Security Policy |
| NFR-010.8 | CSRF protection | Anti-forgery tokens |

---

## NFR-011: Micro-Interactions & UX Polish

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-011.1 | Task creation animation (slide-in) | < 300ms |
| NFR-011.2 | Task completion animation (strikethrough + fade) | < 500ms |
| NFR-011.3 | Drag-and-drop with ghost element and drop shadow | Real-time |
| NFR-011.4 | Theme switch transition (no white flash) | Instant |
| NFR-011.5 | Loading skeletons for all async content | Immediate |
| NFR-011.6 | Empty states with helpful illustrations/CTAs | All empty views |
| NFR-011.7 | Toast notifications for async operations | Non-blocking |

**Rationale**: Multiple scenarios emphasize that UX polish (animations, celebrations, feedback) is core to the product, not decoration.

---

## v2 Non-Functional Requirements

> **Status**: Draft (v2)
> **Scope**: The following NFR categories apply exclusively to v2 modules (agents, projects, comms, people).
> All v1 NFRs above remain in force. v2 must not regress any v1 NFR.

---

## NFR-V2-01: Agent Session Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-01.1 | Sidecar startup time (Node.js process spawn to first Redis event) | < 3s |
| NFR-V2-01.2 | Activity stream event latency (SDK event → UI render via SSE/SignalR) | < 500ms |
| NFR-V2-01.3 | Redis Streams write latency (XADD from Node.js sidecar) | < 10ms |
| NFR-V2-01.4 | Redis Streams read latency (.NET BackgroundService poll cycle) | < 500ms |
| NFR-V2-01.5 | Skill composition time (merge base template + all enabled skills) | < 100ms |
| NFR-V2-01.6 | Skill hot-load + sidecar reload_config roundtrip | < 5s |
| NFR-V2-01.7 | Session pool allocation (CanAllocate + Allocate domain service call) | < 50ms |
| NFR-V2-01.8 | Context window snapshot update frequency | Every SDK usage_update event (no batching) |
| NFR-V2-01.9 | AutoContinue validation loop turn start latency | < 200ms from previous turn end |

**Rationale**: The sidecar-to-UI pipeline has multiple hops (Node.js → Redis → .NET → SSE → React). Each hop must be budgeted separately to achieve the end-to-end < 500ms rendering target. The 500ms Redis poll interval is the limiting factor and is documented as acceptable for a personal dev tool (see docs/operations/research/redis-streams.md §14.5).

---

## NFR-V2-02: Real-Time Streaming

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-02.1 | SSE/WebSocket delivery latency (SignalR hub → React UI) | < 200ms |
| NFR-V2-02.2 | Activity stream pagination (cursor-based) | 100 items per page |
| NFR-V2-02.3 | SignalR hub concurrent connections (single user, multiple tabs) | Up to 50 connections |
| NFR-V2-02.4 | Log chunk buffer flush interval (streaming session output) | Max 500ms before flush |
| NFR-V2-02.5 | Session reconnection (auto-reconnect on disconnect) | Within 5s |
| NFR-V2-02.6 | Stream replay on .NET backend restart (from last acknowledged offset) | Starts within 10s of restart |
| NFR-V2-02.7 | Dev server log streaming (IProcessService stdout → UI) | < 500ms latency |
| NFR-V2-02.8 | Unified inbox message delivery (IngestMessageCommand → inbox UI) | < 2s from adapter fetch |

---

## NFR-V2-03: Agent Budget & Metrics Accuracy

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-03.1 | Cost tracking accuracy vs. Claude API billing | Within $0.001 of actual charge |
| NFR-V2-03.2 | Token count accuracy vs. Claude API usage reports | Exact match |
| NFR-V2-03.3 | Context window snapshot staleness | Never stale by more than 1 SDK usage_update event |
| NFR-V2-03.4 | Budget hard-cap enforcement timing | Cap enforced BEFORE the session processes the turn that would exceed it |
| NFR-V2-03.5 | Budget warning threshold event | Fired at >= 80% of SessionBudget.HardCapUsd |
| NFR-V2-03.6 | Subagent cost aggregation | Parent session total always includes all subagent costs |
| NFR-V2-03.7 | WorkQueue total cost accuracy | Sum of all item session costs within $0.01 of actual |
| NFR-V2-03.8 | QueueBudget hard-cap enforcement | Queue auto-pauses before sum of session costs exceeds HardCapUsd |

**Rationale**: Budget enforcement is a safety invariant, not just a display concern. An over-running session can incur real financial cost. The domain enforces this conservatively: the cap must trigger before the overspend, never after. See docs/domain/contexts/agents.md §8.1 principle 5.

---

## NFR-V2-04: Skills System

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-04.1 | Maximum skills enabled per session | 20 |
| NFR-V2-04.2 | Maximum tool definitions per skill | 50 |
| NFR-V2-04.3 | Maximum subagent definitions per skill | 10 |
| NFR-V2-04.4 | Memory pill storage limit per skill | Unlimited (paginated queries; no hard cap) |
| NFR-V2-04.5 | Skill instruction text limit | 50,000 characters per skill |
| NFR-V2-04.6 | Skill version history retained | Last 10 versions |
| NFR-V2-04.7 | Skill consolidation session availability | Always available as a meta-skill option |
| NFR-V2-04.8 | Composed system prompt size (base template + all active skills) | Must fit within model context window; error surfaced if exceeded before session start |
| NFR-V2-04.9 | Skill name uniqueness scope | Per owner (no two active skills with the same name) |

---

## NFR-V2-05: Communication Adapters

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-05.1 | Adapter API call response time | < 2s per outbound call |
| NFR-V2-05.2 | Gmail sync poll interval | Configurable; default 5 minutes |
| NFR-V2-05.3 | WhatsApp bridge reconnection on disconnect | Auto-reconnect within 30s |
| NFR-V2-05.4 | Discord/Slack webhook event processing | < 1s from webhook receipt to IngestMessageCommand |
| NFR-V2-05.5 | Adapter failure isolation | One adapter crash must not affect message ingestion for other channels |
| NFR-V2-05.6 | Graceful degradation when adapter is down | Other modules (tasks, projects, agents) continue normally; degraded channel is marked with ChannelStatus.Degraded |
| NFR-V2-05.7 | Message ingestion idempotency | Duplicate ExternalMessageIds are silently ignored (no duplicate threads or messages) |
| NFR-V2-05.8 | Outbound reply timeout | 5s; surfaces DomainError on timeout without retrying automatically |
| NFR-V2-05.9 | Channel credential re-validation on health check failure | Attempted once per health check cycle; three consecutive failures transition channel to Error |

**Rationale**: Each IChannelAdapter is isolated in the infrastructure layer. Adapter crashes are bounded by the adapter factory pattern — no cross-adapter state sharing exists at the domain level. See docs/domain/contexts/comms.md §9.8.

---

## NFR-V2-06: Security (v2 Additions)

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-06.1 | Agent API key entropy | 256-bit, cryptographically random |
| NFR-V2-06.2 | Agent API key storage | SHA-256 hash only; plaintext shown once at session start, never retrievable |
| NFR-V2-06.3 | Agent API key scope | Per-session; one key cannot access any other session's endpoints |
| NFR-V2-06.4 | Redis Streams access | Authentication required (access key or Azure Managed Identity); no anonymous access |
| NFR-V2-06.5 | Node.js sidecar filesystem isolation | Each process runs with read-only access outside its WorkingDirectory |
| NFR-V2-06.6 | Agent tool ACL enforcement | Tools validated against session's allowed tool list before execution; disallowed tool calls are rejected and logged |
| NFR-V2-06.7 | Memory pill content sanitization | Content sanitized to prevent prompt injection into skill instructions before persistence |
| NFR-V2-06.8 | Communication adapter OAuth tokens | Encrypted at rest using AES-256-GCM with per-record IV (same pattern as v1 credential storage) |
| NFR-V2-06.9 | Project local path access control | IFileService enforces that resolved paths stay within the project root (path traversal prevention) |
| NFR-V2-06.10 | Redis command stream write access | Only the .NET API backend may write to agent command streams; Node.js sidecar is read-only on command streams |

---

## NFR-V2-07: Bridge Context Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-07.1 | Domain event propagation latency (source context raises event → bridge handler runs) | < 100ms |
| NFR-V2-07.2 | Correlation lookup by SessionId or WorktreeId | < 10ms (indexed columns) |
| NFR-V2-07.3 | WorkQueue item dispatch latency (AdvanceQueue → StartQueuedAgentSessionCommand) | < 200ms |
| NFR-V2-07.4 | Cross-context eventual consistency (source event → all subscriber contexts settled) | < 2s |
| NFR-V2-07.5 | Concurrent Resolving correlations per project | Maximum 1 at a time (design constraint; see bridge Design Notes) |
| NFR-V2-07.6 | Worktree creation roundtrip (CreateWorktreeCommand → CorrelationActivatedEvent) | < 10s (dominated by git worktree add command) |

**Rationale**: The bridge context orchestrates multi-step workflows that cross the Projects and Agents bounded contexts. Each step adds latency. The 10s worktree creation budget covers git I/O and is the only step expected to take more than 200ms. All in-memory correlation and queue operations must be fast enough to not stall session dispatch.

---

## NFR-V2-08: Data & Storage

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-08.1 | Primary data store (dev) | SQLite (same as v1) |
| NFR-V2-08.2 | Primary data store (prod) | SQL Server (same as v1) |
| NFR-V2-08.3 | Redis role | Event transport only (agent session streams + command streams); not the primary persistence store |
| NFR-V2-08.4 | Redis stream retention period | 30 days (configurable via XTRIM MINID); older entries automatically evicted |
| NFR-V2-08.5 | Git repository data ownership | Never copied into the LemonDo database; referenced by LocalPath only |
| NFR-V2-08.6 | Activity stream storage | Append-only in primary DB; configurable retention (default 90 days) |
| NFR-V2-08.7 | Memory pill storage | Persisted permanently in primary DB as part of AgentSkill aggregate |
| NFR-V2-08.8 | Session output storage | Stored in primary DB on session completion; Redis stream is the live feed |
| NFR-V2-08.9 | Channel message body storage | Up to 50,000 chars per message in primary DB; large body overflow to blob store is a future infrastructure decision |
| NFR-V2-08.10 | Redis data loss recovery | .NET backend replays from last-acknowledged stream offset (stored in primary DB); recovery completes within 30s of backend restart |

---

## NFR-V2-09: Reliability & Recovery

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-09.1 | Sidecar crash detection | Detected within 5s; session marked Failed; pool slot released |
| NFR-V2-09.2 | .NET backend restart recovery | Re-subscribes to all active Redis Streams; replays from last-acknowledged offset |
| NFR-V2-09.3 | Intermediate state settlement on restart | Sessions in Interrupting, Pausing, Resuming, or Cancelling must settle to a terminal or stable state within 30s of backend restart |
| NFR-V2-09.4 | SessionChain integrity on session failure mid-handoff | Chain remains Active; failed session is marked Failed; user can retry (start a new session in the chain) |
| NFR-V2-09.5 | WorkQueue resilience in Sequential mode | Queue auto-pauses on item failure; subsequent items are not dispatched until manually resumed |
| NFR-V2-09.6 | WorkQueue resilience in Parallel mode | Failed item is marked Failed; other running items continue; queue completes when all non-failed items are done |
| NFR-V2-09.7 | Communication adapter failure recovery | Failed channels do not block inbox load; inbox returns messages from healthy channels with a per-channel error indicator |
| NFR-V2-09.8 | Dev server process crash detection | IProcessService detects unexpected exit; DevServer status transitions to Failed; exit code recorded |
| NFR-V2-09.9 | Git operation failure handling | All IGitService errors surface as DomainError via Result<T,E>; Worktree is not left in a Creating state indefinitely |

---

## NFR-V2-10: Scalability Constraints (Single-User)

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-10.1 | Maximum concurrent agent sessions | 20 (configurable via SessionPoolConfig) |
| NFR-V2-10.2 | Maximum session chain length | 50 sessions per chain |
| NFR-V2-10.3 | Maximum AutoContinue iterations per turn | 5 (configurable; prevents runaway loops) |
| NFR-V2-10.4 | Maximum queued messages per session | 100 |
| NFR-V2-10.5 | Maximum WorkQueue items | 50 |
| NFR-V2-10.6 | Maximum parallel sessions in a WorkQueue | 10 (MaxParallelSessions; enforced by WorkQueue invariant) |
| NFR-V2-10.7 | Maximum registered projects | No hard limit; list queries paginate at 50 per page |
| NFR-V2-10.8 | Maximum worktrees per project | No hard limit; list queries paginate at 20 per page |
| NFR-V2-10.9 | Maximum connected channels (Comms) | No hard limit; one channel per ChannelType enforced by domain invariant |
| NFR-V2-10.10 | Maximum enabled skills per session | 20 (NFR-V2-04.1) |

**Rationale**: These limits are intentionally conservative for a single-user personal tool. The primary concern is preventing runaway agent sessions from incurring unbounded API costs (sessions, chain lengths, AutoContinue iterations) or overwhelming the local machine (concurrent sessions, parallel WorkQueue items). All limits are configurable to allow tuning without code changes.

---

## NFR-V2-11: Local-First & Offline Behaviour

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-11.1 | Projects module (list, view, git log) | Fully functional without internet |
| NFR-V2-11.2 | Task management (all v1 features) | Fully functional without internet (v1 PWA — unchanged) |
| NFR-V2-11.3 | Agent sessions | Require internet (Claude API dependency); unavailable offline |
| NFR-V2-11.4 | Communication features | Require internet (external platform APIs); unavailable offline |
| NFR-V2-11.5 | Offline indicator for agent/comms features | Displayed when internet is unavailable; affected features are disabled with clear messaging |
| NFR-V2-11.6 | Git operations (worktree create, status, commit, log) | Fully local; no internet dependency |
| NFR-V2-11.7 | Dev server start/stop | Fully local; no internet dependency |
| NFR-V2-11.8 | Ngrok tunnel creation | Requires internet (ngrok API); gracefully disabled when offline |

---

## NFR-V2-12: v1 Non-Regression

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-12.1 | All v1 backend tests continue to pass | 403 tests; zero regressions |
| NFR-V2-12.2 | All v1 frontend tests continue to pass | 531 tests; zero regressions |
| NFR-V2-12.3 | All v1 E2E tests continue to pass | 152 Playwright tests; zero regressions |
| NFR-V2-12.4 | v1 API contracts unchanged | No breaking changes to existing endpoints |
| NFR-V2-12.5 | v1 performance targets maintained | All NFR-001 targets continue to be met |
| NFR-V2-12.6 | v1 PWA offline behaviour unchanged | NFR-003 targets continue to be met |
| NFR-V2-12.7 | Database migrations are additive only | No v1 table drops or column removals |

**Rationale**: v2 is evolutionary, not a rewrite. All v1 modules (tasks, boards, identity, administration, onboarding, analytics, notification) must remain fully functional throughout v2 development. Each v2 implementation checkpoint must run the full v1 test suite before merging.
