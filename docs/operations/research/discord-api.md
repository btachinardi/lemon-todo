# Discord Bot API

> **Source**: Extracted from docs/operations/research.md §12
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date Researched**: 2026-02-18
> **Purpose**: Discord integration for the `comms` module — monitoring specific servers/channels for messages, reading message history, and sending replies from LemonDo (CM-003).
> **Recommendation**: Use — well-maintained community .NET SDK (Discord.Net), simple bot token auth, no OAuth required for reading channels Bruno is a member of, and rate limits are generous for single-server personal use.

---

## 12. Discord Bot API

### 12.1 Capabilities

- Receive real-time messages via Gateway WebSocket connection (preferred over polling)
- Read message history in channels using `GET /channels/{channel.id}/messages`
- Send messages to channels using `POST /channels/{channel.id}/messages`
- Reply to specific messages using the `message_reference` field
- React to messages with emoji
- List members, roles, and channels in a server
- Monitor message edits and deletions via Gateway events
- Receive direct messages via Gateway
- Read message content requires the `MESSAGE_CONTENT` privileged intent (see §12.2)

For LemonDo scenario S-CM-01 (Morning Inbox Review), LemonDo registers a bot in Bruno's personal Discord servers. The bot connects via Gateway and streams new messages to LemonDo's backend in real time, without polling.

### 12.2 Authentication

A Discord bot token is used for all API calls. The bot is created in the Discord Developer Portal, added to Bruno's servers via an OAuth2 invite URL (one-time setup), and the token is stored in Azure Key Vault.

**Auth flow**: Bot token in `Authorization: Bot {token}` header for REST calls; same token for Gateway WebSocket IDENTIFY payload

**Privileged intents required**:

| Intent | Required For | Approval Needed |
|--------|-------------|-----------------|
| `MESSAGE_CONTENT` | Reading message body text | No — bots in under 100 servers can enable freely in Developer Portal |
| `GUILD_MEMBERS` | Listing server members | No — same threshold |
| `GUILD_PRESENCES` | Online status | Not needed for LemonDo |

Since LemonDo's bot will be in far fewer than 100 servers (Bruno's personal servers only), all privileged intents can be enabled in the Developer Portal without applying to Discord for approval.

### 12.3 Rate Limits

| Limit | Value | Window | Notes |
|-------|-------|--------|-------|
| Global (all bots) | 50 requests | per second | Applies across all API calls |
| Per-route | Varies | Per route | Indicated by `X-RateLimit-Bucket` header |
| Message send | No explicit limit | — | Subject to global + per-route limits; burst allowed |
| Invalid requests | 10,000 | per 10 minutes | 401/403/429 responses count; ban risk if exceeded |

Discord does not publish specific per-route limits in advance and warns that limits change; applications must parse `X-RateLimit-*` response headers and respect `Retry-After` on 429 responses. For personal use reading a few Discord servers, the global limit of 50 req/sec is never approached.

### 12.4 Pricing

Discord's Bot API is free. Creating a Discord application and bot in the Developer Portal has no cost.

**Estimated monthly cost (single user)**: Free.

### 12.5 SDK Options

| Platform | Package | Maintained | Last Updated | Notes |
|----------|---------|------------|--------------|-------|
| .NET | `Discord.Net` (NuGet) | Community — active | July 2025 (v3.18.0) | Targets .NET Framework 4.6.1, .NET Standard 2.0/2.1, .NET 5-9; compatible with .NET 10 via .NET Standard 2.0 |
| TypeScript | `discord.js` (npm) | Community — very active | Active | Industry-standard Discord bot library for Node.js |

`Discord.Net` is the leading .NET Discord library, MIT-licensed and community-maintained with regular releases. It provides a full Gateway client (WebSocket), REST client, and command/interaction framework. The `Discord.Addons.Hosting` NuGet package (`6.1.0`) wraps Discord.Net as an ASP.NET Core hosted service — ideal for the LemonDo backend.

Note: Discord.Net is not an official Discord library. However, it has been the de facto standard for .NET Discord bots for several years and is actively maintained.

### 12.6 Risks

| Risk | Level | Detail |
|------|-------|--------|
| API stability | Medium | Discord has made breaking changes to their API (v6 → v8 → v10 intent requirements) in the past; Discord.Net tracks these changes |
| Vendor lock-in | Low | Discord message formats are unique but the adapter pattern isolates this risk |
| Rate limit impact | Low | Single-user monitoring of a few servers is negligible traffic |
| Pricing risk | Low | Free API |
| Community SDK | Medium | Discord.Net is community-maintained, not official; if maintainers abandon it, LemonDo would need to fork or switch |
| Privileged intent changes | Low | Discord has not tightened intent requirements for small bots; policy is stable |

### 12.7 Alternatives

| Option | SDK (.NET) | Complexity | Recommendation |
|--------|-----------|------------|----------------|
| Discord.Net (Gateway) | Community | Low | Primary — real-time, feature-complete |
| Discord REST API (direct HTTP) | None — raw HTTP | Medium | Fallback — no WebSocket client needed but no real-time |
| NetCord (community) | Community (`NetCord`) | Low | Alternative — newer, targets .NET 8+, less mature |

### 12.8 References

- [Discord Developer Documentation — Gateway](https://discord.com/developers/docs/events/gateway)
- [Discord Rate Limits Documentation](https://discord.com/developers/docs/topics/rate-limits)
- [Discord Privileged Intents FAQ](https://support-dev.discord.com/hc/en-us/articles/6207308062871-What-are-Privileged-Intents)
- [Discord.Net NuGet Package](https://www.nuget.org/packages/Discord.Net)
- [Discord.Net GitHub](https://github.com/discord-net/Discord.Net)
- [Discord.Addons.Hosting NuGet](https://www.nuget.org/packages/Discord.Addons.Hosting)
