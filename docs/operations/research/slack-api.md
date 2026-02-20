# Slack API

> **Source**: Extracted from docs/operations/research.md §13
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date Researched**: 2026-02-18
> **Purpose**: Slack integration for the `comms` module — monitoring specific workspaces/channels for messages, reading message history, and sending replies from LemonDo (CM-004).
> **Recommendation**: Use — with the important caveat that LemonDo's Slack app must be registered as an "internal" workspace app (not commercially distributed) to avoid the 2025 rate limit restrictions on `conversations.history`.

---

## 13. Slack API

### 13.1 Capabilities

- Receive real-time messages via Socket Mode (WebSocket) — preferred for LemonDo; no public webhook URL required
- Read channel message history via `conversations.history` and thread replies via `conversations.replies`
- Send messages to channels via `chat.postMessage`
- Reply in threads via `thread_ts` parameter
- List workspaces, channels, and members
- React to messages with emoji via `reactions.add`
- Search messages via `search.messages` (requires user token, not bot token)
- Receive DMs via Socket Mode events

Socket Mode is the key capability for LemonDo: the bot establishes an outbound WebSocket connection to Slack, so LemonDo does not need a public HTTPS endpoint to receive events. This eliminates the need for Pub/Sub or webhook infrastructure during development.

### 13.2 Authentication

Slack apps use OAuth 2.0 to install into workspaces. For LemonDo (a single-user internal app), Bruno installs the app into his own Slack workspaces via a one-time OAuth flow. The app receives a bot token (`xoxb-...`) stored in Azure Key Vault.

**Auth flow**: OAuth 2.0 Authorization Code flow for initial workspace install; bot token (`xoxb-...`) stored server-side for all subsequent API calls; Socket Mode uses an app-level token (`xapp-...`) for the WebSocket connection

**Scopes required**:

| Scope | Purpose |
|-------|---------|
| `channels:history` | Read message history in public channels |
| `channels:read` | List public channels |
| `groups:history` | Read message history in private channels |
| `groups:read` | List private channels |
| `im:history` | Read DMs |
| `chat:write` | Send messages |
| `connections:write` | Enable Socket Mode (app-level token scope) |

### 13.3 Rate Limits

Standard rate limits for internal (non-Marketplace) apps — which LemonDo qualifies as:

| Method | Tier | Limit | Notes |
|--------|------|-------|-------|
| `conversations.history` | Special | 50+ req/min, 1,000 msgs/req | Internal apps retain full rate limits (not affected by May 2025 changes) |
| `conversations.replies` | Special | 50+ req/min | Same exemption for internal apps |
| `chat.postMessage` | Special | 1 per second per channel | Short bursts allowed |
| Most read methods | Tier 2 | 20+ req/min | e.g., `channels.list`, `users.list` |
| Most write methods | Tier 3 | 50+ req/min | e.g., `reactions.add` |

Critical note from Slack's May 2025 changes: Commercially distributed apps (not Marketplace-approved) face a 1 req/min limit on `conversations.history` with only 15 messages per request starting March 2026. However, **internal customer-built apps are explicitly exempt** — they retain 50+ req/min with 1,000 messages per request. Since LemonDo is Bruno's personal internal tool, it qualifies for the internal app exemption.

Exceeded limits return HTTP 429 with a `Retry-After` header.

### 13.4 Pricing

Slack's Web API is free for app developers. The Slack workspace subscription (what Bruno pays as a Slack user) is separate from API costs. The API itself has no per-call charges.

**Estimated monthly cost (single user)**: Free (API access is free; Slack workspace subscription is Bruno's existing cost).

### 13.5 SDK Options

| Platform | Package | Maintained | Last Updated | Notes |
|----------|---------|------------|--------------|-------|
| .NET | `SlackNet` (NuGet) | Community — active | December 2025 (v0.17.7) | Comprehensive; includes Socket Mode client; targets .NET 8 and .NET Standard 2.0 |
| .NET (AspNetCore) | `SlackNet.AspNetCore` (NuGet) | Community — active | December 2025 | ASP.NET Core integration for Slack events/webhooks |
| TypeScript | `@slack/bolt` (npm) | Official — Slack | Active | Slack's official framework for Bolt apps |
| TypeScript | `@slack/web-api` (npm) | Official — Slack | Active | Low-level Slack Web API client |

Slack does not publish an official .NET SDK. `SlackNet` (by soxtoby) is the leading community .NET library with 7+ million total downloads and active maintenance as of December 2025. It supports Socket Mode, making it the right choice for LemonDo's backend.

There is no official Slack Bolt SDK for .NET. The official Bolt SDKs are JavaScript and Python only.

### 13.6 Risks

| Risk | Level | Detail |
|------|-------|--------|
| API stability | Medium | Slack made significant rate limit changes in May 2025; future policy changes could impact internal apps; Slack has a history of deprecating APIs (RTM API deprecated) |
| Vendor lock-in | Low | Slack message formats are well-documented; adapter pattern isolates this risk |
| Rate limit impact | Low | Internal app exemption provides full rate limits; single-user is well within them |
| Pricing risk | Low | Free API |
| Community SDK | Medium | SlackNet is community-maintained with no official Slack endorsement; activity is good but volunteer-dependent |
| Internal app status | Medium | LemonDo must remain an "internal" workspace app to keep full rate limits; if it ever becomes distributed, rate limits would apply |

### 13.7 Alternatives

| Option | SDK (.NET) | Complexity | Recommendation |
|--------|-----------|------------|----------------|
| SlackNet (Socket Mode) | Community | Low | Primary — no public URL needed, real-time |
| Slack Bolt (TypeScript sidecar) | Official (TypeScript) | Medium | Alternative — only if TypeScript is preferred |
| Direct REST API | None — raw HTTP | Medium | Fallback — no Socket Mode client |

### 13.8 References

- [Slack Web API Rate Limits](https://docs.slack.dev/apis/web-api/rate-limits/)
- [Slack Rate Limit Changes — May 2025](https://docs.slack.dev/changelog/2025/05/29/rate-limit-changes-for-non-marketplace-apps/)
- [Slack Rate Limit Clarification — June 2025](https://docs.slack.dev/changelog/2025/06/03/rate-limits-clarity/)
- [Slack OAuth Scopes](https://api.slack.com/scopes)
- [SlackNet NuGet Package](https://www.nuget.org/packages/SlackNet)
- [SlackNet GitHub](https://github.com/soxtoby/SlackNet)
- [conversations.history Method Reference](https://docs.slack.dev/reference/methods/conversations.history/)
