# Technology Research

> Versioned technology decisions, compatibility notes, and integration research for every library and API in the LemonDo stack.

---

## Contents

| Document | Description | Module | Recommendation | Status |
|----------|-------------|--------|---------------|--------|
| [backend-technologies.md](./backend-technologies.md) | .NET 10, Aspire 13, ASP.NET Core Identity, EF Core 10, Scalar, MSTest 4, FsCheck, OpenTelemetry, WebPush | v1 stack | Active stack | Active |
| [frontend-technologies.md](./frontend-technologies.md) | Vite 7, React 19, Shadcn/ui, Tailwind CSS, react-i18next, vite-plugin-pwa, Zustand 5, TanStack Query 5, date-fns, react-day-picker, OTel Browser SDK | v1 stack | Active stack | Active |
| [testing-technologies.md](./testing-technologies.md) | MSTest/FsCheck (backend), Vitest/fast-check (frontend), Playwright, BrowserStack, visual regression strategy | v1 stack | Active stack | Active |
| [infrastructure-technologies.md](./infrastructure-technologies.md) | Docker, GitHub Actions, Terraform + Azure, SQL Server | v1 stack | Active stack | Active |
| [compatibility-matrix.md](./compatibility-matrix.md) | Cross-reference of version compatibility across all stack components | v1 stack | Reference | Active |
| [version-lock-summary.md](./version-lock-summary.md) | Pinned versions for every dependency in the LemonDo MVP stack | v1 stack | Reference | Active |
| [openapi-type-generation.md](./openapi-type-generation.md) | Build-time OpenAPI generation, schema transformers, openapi-typescript, enum enrichment strategy, translation guard tests | v1 stack | Use | Active |
| [gmail-api.md](./gmail-api.md) | Gmail REST API: inbox reading, push notifications, OAuth 2.0, rate limits | `comms` | Use | Active |
| [whatsapp-api.md](./whatsapp-api.md) | WhatsApp Business Cloud API vs Baileys bridge: capabilities, risks, ToS implications | `comms` | Do Not Use (official); Spike (Baileys) | Active |
| [claude-api.md](./claude-api.md) | Anthropic Messages API and Claude Agent SDK: session management, rate limits, pricing tiers | `agents` | Use | Active |
| [ngrok.md](./ngrok.md) | ngrok REST API: programmatic tunnel management, Hobbyist plan, .NET SDK options | `projects` | Use | Active |
| [discord-api.md](./discord-api.md) | Discord Bot API: Gateway WebSocket, Discord.Net SDK, privileged intents, rate limits | `comms` | Use | Active |
| [slack-api.md](./slack-api.md) | Slack Web API: Socket Mode, SlackNet SDK, internal app exemption from 2025 rate limit changes | `comms` | Use | Active |
| [redis-streams.md](./redis-streams.md) | Redis Streams event bus: XADD/XREAD/consumer groups, StackExchange.Redis + node-redis SDKs, Azure Managed Redis hosting, alternatives comparison | `agents` | Use | Active |

---

## Quick Decision Matrix

| Technology | Recommendation | Estimated Monthly Cost | Notes |
|------------|---------------|----------------------|-------|
| .NET 10 + Aspire 13 | Use — active v1 stack | Infra cost only | LTS, November 2025 GA |
| EF Core 10 | Use — active v1 stack | — | Named query filters, migrations |
| Vite 7 + React 19 | Use — active v1 stack | — | HMR, React Compiler |
| Shadcn/ui | Use — active v1 stack | — | CLI-based, no runtime version |
| Playwright | Use — active v1 stack | — | Cross-browser E2E |
| openapi-typescript | Use | — | Type-safe frontend from OpenAPI spec |
| Gmail API | Use | ~$0.00 | Free + negligible Pub/Sub costs |
| WhatsApp Cloud API | Do Not Use | $0 | Not designed for personal inbox reading |
| Baileys (WhatsApp) | Needs Spike | $0 | ToS violation risk; only personal inbox option |
| Claude API (Anthropic) | Use | ~$15-$40/month | Sonnet 4.6 recommended for agent sessions |
| ngrok | Use | ~$8/month | Hobbyist annual plan; removes interstitial |
| Discord Bot API | Use | Free | Discord.Net community SDK; Gateway WebSocket |
| Slack API | Use | Free | Internal app exemption retains full rate limits |
| Redis Streams | Use | ~$13/month (Azure Managed Redis B0) or ~$0 (self-hosted container) | First-class Aspire integration; StackExchange.Redis + node-redis; append-only log with replay |

---

## Summary

**v1 Stack Research** (sections 1-6) documents the full backend, frontend, testing, and infrastructure technology choices made during LemonDo v1 development. All versions are pinned and verified compatible. The [compatibility matrix](./compatibility-matrix.md) provides a quick cross-reference, and the [version lock summary](./version-lock-summary.md) lists every pinned dependency for reproducible builds.

**OpenAPI Type Generation** (section 7) was researched in February 2026 to eliminate manual TypeScript type files that silently drift from the C# backend. The solution uses `Microsoft.Extensions.ApiDescription.Server` for build-time spec generation and `openapi-typescript` for client-side type generation. A custom schema transformer enriches string-typed enum properties in the OpenAPI output, and a Vitest translation guard test ensures all enum values stay translated.

**v2 API Research** (sections 8-13) evaluates the external APIs required for the four new v2 modules. Gmail, Discord, and Slack are all straightforward integrations with good .NET SDK support and free or negligible costs. ngrok provides programmatic tunnel management for the projects module at $8/month. WhatsApp is the most complex and risky integration — the official API is not suitable for personal inbox reading, and the only unofficial alternative (Baileys) carries ToS-violation risk; it is recommended for deferral or a dedicated spike. The Anthropic Claude API and Agent SDK are the core of the agents module, with costs varying by session volume and a Node.js sidecar required for Agent SDK integration from the .NET backend.

**Redis Streams** (section 14) was researched in February 2026 as the event bus connecting Node.js Claude Agent SDK sidecars to the .NET backend. Redis Streams is recommended as the primary option: it is an append-only log with consumer groups, at-least-once delivery guarantees, and event replay capability — exactly what is needed for agent session state management. Aspire 13 auto-provisions a Redis Docker container locally and switches to Azure Managed Redis in production with a single line of AppHost configuration. The main tradeoff is StackExchange.Redis's lack of blocking reads (polling every 500ms is the accepted workaround). An alternative path using the existing SQL Server database as the event store (via the agent callback API) is documented as a viable zero-infrastructure option.

All v2 API entries follow a consistent structure: capabilities, authentication flow, rate limits, pricing, SDK options, risk table, and alternatives — enabling quick comparison when making implementation decisions.
