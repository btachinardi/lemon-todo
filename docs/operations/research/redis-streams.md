# Redis Streams — Event Bus for Agent Sessions

> **Date Researched**: 2026-02-18
> **Purpose**: Event bus for the `agents` module — Node.js Claude Agent SDK sidecars publish session lifecycle events; the .NET backend subscribes and builds domain state from the event stream, and the React UI receives real-time updates via SSE.
> **Recommendation**: Use — Redis Streams is the best-fit event bus for this architecture: first-class Aspire integration (auto-provisioned Docker container locally, Azure Managed Redis in production), excellent SDKs on both Node.js and .NET, and the append-only stream with consumer groups provides exactly the replay and at-least-once delivery guarantees needed for agent session auditing.

---

## 14. Redis Streams

### 14.1 Capabilities

**Core stream commands** — all O(1) for fixed-count reads; O(N) for range scans:

- **XADD** — append an entry to a stream; auto-generates a millisecond-timestamp ID (e.g., `1708000000000-0`); supports `MAXLEN ~N` for automatic trimming in the same call. [Redis XADD docs](https://redis.io/docs/latest/commands/xadd/)
- **XREAD** — read one or more entries starting from a given ID; supports blocking with `BLOCK <ms>` to wait for new messages. [Redis XREAD docs](https://redis.io/docs/latest/commands/xread/)
- **XRANGE / XREVRANGE** — range scan with start/end IDs; supports count limiting; enables event replay from any point. [Redis Streams docs](https://redis.io/docs/latest/develop/data-types/streams/)
- **XLEN** — O(1) count of entries in a stream. [Redis Streams docs](https://redis.io/docs/latest/develop/data-types/streams/)
- **XTRIM** — trim a stream to a maximum length (exact or approximate `~`); approximate trimming is much more efficient as it only evicts full radix-tree nodes. [Redis Streams docs](https://redis.io/docs/latest/develop/data-types/streams/)

**Consumer groups** — reliable, at-least-once delivery:

- **XGROUP CREATE** — create a consumer group; set start position to `0` to replay all history or `$` to read only new messages. [XGROUP docs](https://redis.io/docs/latest/commands/xgroup/)
- **XREADGROUP** — read into a named consumer within a group; each delivered message is tracked in the Pending Entries List (PEL) until acknowledged. [XREADGROUP docs](https://redis.io/docs/latest/commands/xreadgroup/)
- **XACK** — acknowledge a message, removing it from the PEL and confirming successful processing. [XACK docs](https://redis.io/docs/latest/commands/xack/)
- **XPENDING** — inspect pending (unacknowledged) messages per consumer; enables dead-letter logic. [XPENDING docs](https://redis.io/docs/latest/commands/xpending/)
- **XCLAIM** — reassign a stuck pending message from one consumer to another; used for failure recovery.
- **At-least-once delivery**: messages are redelivered if the consumer crashes before calling XACK. This is the correct guarantee for agent session events — we want to know if a session started, even if the first delivery was lost.

**Persistence and replay**:

- Streams participate in Redis AOF (Append-Only File) and RDB (snapshot) persistence. With `appendonly yes` + `appendfsync everysec`, durability is sub-second. [Redis Persistence docs](https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/)
- **Replay**: any consumer can rewind to any past ID and re-read history. The .NET backend can replay all agent session events on startup to rebuild in-memory state — a classic event sourcing pattern.
- **Memory management**: use `XTRIM MAXLEN ~1000` to cap each stream or use time-based trimming via `MINID` to evict entries older than N days. For a single-user personal app, event volume is tiny (tens of events per session, dozens of sessions per month) — memory impact is negligible without trimming.

**Performance** (single-node, in-memory):

- XADD / XREADGROUP: sub-millisecond latency under normal load.
- Benchmark: 10,000 messages processed with 99.9% latency < 2ms using `XREADGROUP COUNT 10000`. [Redis Streams vs Kafka — Instaclustr](https://www.instaclustr.com/blog/redis-streams-vs-apache-kafka/)
- At single-user scale (a few hundred events per day), Redis Streams will never approach any performance ceiling.

**For LemonDo's agent session event bus specifically**:

- Node.js Claude Agent SDK sidecars call `xAdd('agent-events', '*', { sessionId, type: 'output', payload: ... })` for every session lifecycle event
- .NET `BackgroundService` polls `StreamReadGroupAsync` on the `agent-events` stream using a consumer group, processes events, and updates the `AgentSession` domain aggregate
- ASP.NET Core SSE endpoint pushes processed events to the React UI in real time
- On backend restart, replay from stream position `0` (or last-processed ID stored in DB) to rebuild any incomplete session state

### 14.2 Authentication

**Auth flow**: Redis uses password authentication (`requirepass` directive or `AUTH` command). Connection string format: `redis://:<password>@<host>:6379`. For Aspire local dev, no password is set by default. For Azure Managed Redis, Microsoft Entra ID authentication (managed identity) is available in addition to access keys.

**Scopes required**: Not applicable — Redis is not OAuth-based. The connection string (with password or managed identity token) grants full database access.

**For Azure Managed Redis production**: Use `Aspire.Hosting.Azure.Redis` which injects the connection string automatically via Aspire's resource binding. Microsoft Entra ID (managed identity) authentication avoids hardcoded secrets. [Azure Managed Redis Node.js quickstart](https://learn.microsoft.com/en-us/azure/redis/nodejs-get-started)

### 14.3 Rate Limits

Redis itself has no rate limits — it processes commands as fast as the hardware allows. Practical limits are memory and network throughput.

| Constraint | Limit | Notes |
|------------|-------|-------|
| Max stream entry size | No enforced limit per entry | Practical limit: keep entries under 10KB; use references to large blobs, not inline data |
| Max stream length | No enforced limit | Set `XTRIM MAXLEN ~N` per stream to manage memory |
| Azure Managed Redis B0 max memory | ~500MB | Far exceeds single-user agent event volume |
| Azure Managed Redis B0 max connections | ~256 connections | More than sufficient for one user |
| Local Docker Redis | Memory of host machine | No practical concern for dev |

At single-user scale (Bruno running a few agent sessions per day), total stream data volume is measured in kilobytes, not megabytes.

### 14.4 Pricing

**Local development**: Free — Aspire provisions a Docker container automatically from `docker.io/library/redis`. No Azure cost during development.

**Production (Azure Managed Redis)**:

> **Important**: Azure Cache for Redis (the older service) is being **retired** on September 30, 2028. New instances cannot be created after October 1, 2026 for existing customers. Microsoft recommends migrating to **Azure Managed Redis** (built on Redis Enterprise software). All new projects should target Azure Managed Redis. [Azure Cache for Redis Retirement announcement](https://techcommunity.microsoft.com/blog/azure-managed-redis/azure-cache-for-redis-retirement-what-to-know-and-how-to-prepare/4458721)

Azure Managed Redis pricing (Balanced tier, pay-as-you-go, East US region):

| SKU | Memory | Monthly Cost (single node) | Notes |
|-----|--------|---------------------------|-------|
| Balanced B0 | 500MB | ~$13/month | Smallest available; no HA replica |
| Balanced B1 | 1GB | ~$26/month | Double B0; no HA |
| Balanced B3 | 3GB | ~$78/month | More memory |
| Balanced B5 | 6GB | ~$156/month | — |

Note: B0 and B1 do not support active geo-replication; that is fine for a personal single-region app. Persistence (AOF/RDB) is included in all tiers.

**Estimated monthly cost (single user)**: ~$13/month (Azure Managed Redis B0 Balanced tier). This is the minimum Azure-hosted Redis cost. For a personal app, this may be the dominant infrastructure addition from v2.

**Alternative to managed Redis**: Run Redis as a sidecar container in Azure Container Apps alongside the API. This avoids the $13/month Azure Managed Redis cost entirely, at the expense of managing your own Redis container (no managed backup, no SLA). Given that Redis is used as a transient event bus (events are processed and stored in the main DB), data loss on container restart is recoverable by re-running the stream from a stored checkpoint.

**Estimated monthly cost with self-hosted container**: ~$0 additional (Redis container runs in the existing Container Apps environment; charges are for compute time, not a separate line item). Viable for a personal app.

### 14.5 SDK Options

**Node.js (Claude Agent SDK sidecar — publisher)**:

| Platform | Package | Maintained | Last Published | Notes |
|----------|---------|------------|----------------|-------|
| Node.js / TypeScript | `redis` (npm) | Yes — official Redis client | ~February 2026 (v5.11.0) | Recommended for new Node.js projects; supports `xAdd`, `xRead`, `xReadGroup`, `xAck`; blocking reads supported |
| Node.js / TypeScript | `ioredis` (npm) | Yes — community, maintained by Redis Inc | ~February 2026 (v5.9.3) | Full-featured alternative; supports Streams, Cluster, Sentinel, Lua scripting; built-in TypeScript types |

Both clients are well-maintained. `redis` (node-redis) is the official Redis-maintained client and is recommended for new projects. `ioredis` is the battle-tested community option with a longer history.

**Important**: `node-redis` supports blocking `XREAD BLOCK` and `XREADGROUP BLOCK`. This is preferable to polling in the Node.js sidecar — the sidecar can block-wait for new messages without a busy-loop.

**.NET (ASP.NET Core backend — consumer/subscriber)**:

| Platform | Package | Maintained | Last Published | Notes |
|----------|---------|------------|----------------|-------|
| .NET | `StackExchange.Redis` (NuGet) | Yes — Stack Overflow / community | February 11, 2026 (v2.11.0) | The standard .NET Redis client; supports all stream commands: `StreamAddAsync`, `StreamReadGroupAsync`, `StreamAcknowledgeAsync`, `StreamCreateConsumerGroupAsync`, etc. |
| .NET (Aspire) | `Aspire.StackExchange.Redis` (NuGet) | Yes — Microsoft / .NET Aspire team | February 2026 (v13.1.1) | Aspire integration wrapper; injects `IConnectionMultiplexer` with health checks, telemetry, and automatic connection string binding |
| .NET (Aspire hosting) | `Aspire.Hosting.Redis` (NuGet) | Yes — Microsoft | February 2026 (v13.1.1) | AppHost-side: provisions local Docker container or binds to Azure Redis resource |

**Critical limitation of StackExchange.Redis**: The library's connection multiplexer architecture does not support blocking Redis commands (`XREAD BLOCK`, `BRPOP`, etc.). Blocking operations would tie up the shared connection. The workaround for the .NET consumer is **short-interval polling** (`StreamReadGroupAsync` every 100-500ms in a `BackgroundService`). This is the officially documented pattern for .NET. [Redis .NET Streams guide](https://redis.io/learn/develop/dotnet/streams/stream-basics) [StackExchange.Redis blocking operations issue](https://github.com/StackExchange/StackExchange.Redis/issues/1961)

The polling interval of 500ms is acceptable for LemonDo's use case (agent session event latency of <500ms is fine for a personal dev tool — not a high-frequency trading system).

**Aspire integration summary**:

```csharp
// AppHost (local dev — auto-provisions Docker container)
var redis = builder.AddRedis("redis");
var api = builder.AddProject<Projects.LemonDo_Api>()
    .WithReference(redis);

// AppHost (production — switch to Azure Managed Redis)
var redis = builder.AddAzureRedis("redis");
// Same WithReference — connection string injected automatically
```

The Aspire integration handles the local-to-production transition seamlessly. The `BackgroundService` in the API reads from the same `IConnectionMultiplexer` regardless of whether it is a Docker container or Azure Managed Redis.

### 14.6 Risks

| Risk | Level | Detail |
|------|-------|--------|
| API stability | Low | Redis Streams commands have been stable since Redis 5.0 (2018); the command set has only gained features, not broken changes |
| Vendor lock-in | Low | Redis Streams is an open standard; StackExchange.Redis and node-redis both implement it identically; switching from Azure Managed Redis to self-hosted Docker changes only the connection string |
| Rate limit impact | Low | No rate limits; single-user event volume is trivially small |
| Pricing risk | Low-Medium | Azure Managed Redis B0 is ~$13/month with no usage-based spikes; predictable. If using self-hosted container in ACA, no additional cost. |
| Azure Cache for Redis retirement | Medium | If an existing Redis cache was provisioned under the old service, migration to Azure Managed Redis is required before September 2028 (creation of new instances is blocked from October 2026). For new projects, target Azure Managed Redis directly. [Retirement FAQ](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/retirement-faq) |
| StackExchange.Redis no blocking reads | Low | Polling every 500ms is the documented and accepted workaround; for single-user agent sessions, 500ms latency is acceptable |
| Data loss on Redis restart (without persistence) | Low | If Redis restarts without AOF persistence, unacknowledged events in the stream are lost. Mitigation: enable AOF persistence on Azure Managed Redis (included by default), or store the last-processed event ID in the main SQL Server DB so the .NET backend can request replay from that checkpoint |
| Memory if stream grows unbounded | Low | At single-user scale, stream growth is negligible. Add `XTRIM MAXLEN ~10000` as a safeguard to cap each stream at 10K entries |

### 14.7 Alternatives

| Option | .NET SDK | Node.js SDK | Free Tier | Monthly Cost (single user) | Complexity | Recommendation |
|--------|----------|-------------|-----------|---------------------------|------------|----------------|
| **Redis Streams** | StackExchange.Redis (official, v2.11.0) | redis npm (official, v5.11.0) | Docker (local) | ~$13/month (Azure Managed Redis B0) or ~$0 (self-hosted container) | Low | **Primary** — purpose-built append-only log, excellent Aspire integration, replay capability |
| Azure Service Bus | Azure.Messaging.ServiceBus (official) | @azure/service-bus (official) | None | ~$0.05/M ops (Basic tier) | Medium | Fallback — fully managed, no Docker container needed, but overkill for single-user; lacks replay; adds Azure dependency without Aspire auto-provisioning |
| RabbitMQ | MassTransit + RabbitMQ.Client | amqplib / MassTransit | Docker (local) | ~$0 (self-hosted in ACA) | Medium | Fallback — good .NET support via MassTransit; no native event replay; more complex routing logic than needed |
| Apache Kafka | Confluent.Kafka | kafkajs | Docker (local) | ~$0 (self-hosted) or ~$15+ (Confluent Cloud) | High | Do Not Use — engineered for millions of events/sec; far too complex for single-user agent sessions |
| In-process `Channel<T>` + SQLite log | System.Threading.Channels (built-in) | N/A (Node.js cannot write to .NET channel) | Free | $0 | Low | **Strong alternative** — see analysis below |

**In-process `Channel<T>` + SQLite event log — the zero-infrastructure alternative**:

For LemonDo's specific architecture (single user, Node.js sidecar → .NET API), `Channel<T>` alone cannot cross the process boundary. However, a hybrid approach using the **existing SQL Server / SQLite database** as the event bus is worth serious consideration:

- Node.js sidecar writes agent session events to a `AgentSessionEvents` table via the LemonDo REST API (the API already exists for agent callbacks per AG-012)
- .NET `BackgroundService` polls the `AgentSessionEvents` table every 500ms for unprocessed events
- SSE endpoint pushes to the React UI

**Pros**: Zero additional infrastructure (no Redis), zero additional monthly cost, full ACID durability, trivial debugging (just query the table), natural audit trail (satisfies AG-018).

**Cons**: Database polling is less elegant; SQL Server is slightly heavier than Redis for this pattern; tight coupling between event transport and the main data store.

**Verdict**: The SQLite/SQL Server approach is a viable and simpler alternative, especially during initial implementation. Redis Streams is the recommended path because it decouples the event transport from the main DB, supports the blocking-read pattern in Node.js without polling, and has first-class Aspire integration. However, if infrastructure cost is a concern, starting with the SQL-based approach and migrating to Redis Streams later is a legitimate option.

### 14.8 References

- [Redis Streams official documentation](https://redis.io/docs/latest/develop/data-types/streams/)
- [Redis XADD command reference](https://redis.io/docs/latest/commands/xadd/)
- [Redis XREAD command reference](https://redis.io/docs/latest/commands/xread/)
- [Redis XREADGROUP command reference](https://redis.io/docs/latest/commands/xreadgroup/)
- [Redis XACK command reference](https://redis.io/docs/latest/commands/xack/)
- [Redis XPENDING command reference](https://redis.io/docs/latest/commands/xpending/)
- [Redis Persistence (AOF/RDB)](https://redis.io/docs/latest/operate/oss_and_stack/management/persistence/)
- [Redis Streams in .NET — official guide (redis.io)](https://redis.io/learn/develop/dotnet/streams/stream-basics)
- [StackExchange.Redis Streams documentation](https://stackexchange.github.io/StackExchange.Redis/Streams.html)
- [StackExchange.Redis NuGet (v2.11.0)](https://www.nuget.org/packages/StackExchange.Redis)
- [StackExchange.Redis — blocking operations issue (GitHub)](https://github.com/StackExchange/StackExchange.Redis/issues/1961)
- [redis npm package (v5.11.0)](https://www.npmjs.com/package/redis)
- [ioredis npm package (v5.9.3)](https://www.npmjs.com/package/ioredis)
- [.NET Aspire Redis integration](https://aspire.dev/integrations/caching/redis/)
- [.NET Aspire Azure Cache for Redis integration](https://aspire.dev/integrations/cloud/azure/azure-cache-redis/)
- [Aspire.Hosting.Redis NuGet (v13.1.1)](https://www.nuget.org/packages/Aspire.Hosting.Redis)
- [Aspire.StackExchange.Redis NuGet (v13.1.1)](https://www.nuget.org/packages/Aspire.StackExchange.Redis)
- [Azure Managed Redis — overview](https://learn.microsoft.com/en-us/azure/redis/overview)
- [Azure Managed Redis — pricing](https://azure.microsoft.com/en-us/pricing/details/managed-redis/)
- [Azure Cache for Redis retirement announcement](https://techcommunity.microsoft.com/blog/azure-managed-redis/azure-cache-for-redis-retirement-what-to-know-and-how-to-prepare/4458721)
- [Azure Cache for Redis retirement FAQ](https://learn.microsoft.com/en-us/azure/azure-cache-for-redis/retirement-faq)
- [Redis Streams vs Apache Kafka comparison (Instaclustr)](https://www.instaclustr.com/blog/redis-streams-vs-apache-kafka/)
- [redis-streams-with-dotnet GitHub examples](https://github.com/redis-developer/redis-streams-with-dotnet)
- [Azure Managed Redis Node.js quickstart](https://learn.microsoft.com/en-us/azure/redis/nodejs-get-started)
