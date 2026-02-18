# ngrok

> **Source**: Extracted from docs/operations/research.md §11
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date Researched**: 2026-02-18
> **Purpose**: Programmatic tunnel creation for the `projects` module — exposing local dev servers to the internet for client demos and external testing (PM-008).
> **Recommendation**: Use — official .NET API client exists, Hobbyist plan is $8/month and sufficient for personal use, and the API enables full programmatic tunnel lifecycle management.

---

## 11. ngrok

### 11.1 Capabilities

- Create HTTPS tunnels to local TCP ports (HTTP, HTTPS, TCP protocols)
- Assign custom subdomains on reserved domains (Hobbyist/paid plans)
- Programmatically start and stop tunnels via the ngrok REST API (`https://api.ngrok.com`)
- Alternatively, use the ngrok Agent Local API (runs on `localhost:4040`) to manage tunnels within a running ngrok agent process without an internet round-trip
- Traffic inspection and replay via the ngrok dashboard and API
- IP allowlists and basic auth on tunnels (paid plans)
- Webhook endpoint for traffic events
- Aspire integration: `CommunityToolkit.Aspire.Hosting.Ngrok` NuGet package adds ngrok as an Aspire resource

For LemonDo scenario S-PM-03 (Quick Demo Setup), the flow is: user clicks "Expose" → .NET backend calls ngrok API to create a tunnel → returns public URL → user shares URL → user clicks "Stop" → .NET backend deletes the tunnel via ngrok API.

### 11.2 Authentication

**Auth flow**: API key passed in `Authorization: Bearer {API_KEY}` header for the REST API; stored in Azure Key Vault or local secrets for development

**Scopes required**: Not applicable — API key grants full access to the ngrok account's resources

The ngrok Agent Local API (port 4040) does not require authentication by default when accessed from localhost.

### 11.3 Rate Limits

| Tier | Online Endpoints | HTTP Request Rate | TCP Connections | Notes |
|------|-----------------|------------------|-----------------|-------|
| Free | 3 | 4,000/minute | 100/minute | Interstitial page on endpoints |
| Hobbyist ($8-10/month) | 3 | 20,000/minute | 150/minute | No interstitial page |
| Pay-as-you-go ($20/month+) | Unlimited | 20,000/minute | 600/minute | Advanced features |

For LemonDo's demo use case, 3 online endpoints and 20,000 requests/minute on the Hobbyist plan is more than sufficient. The Hobbyist plan also removes the interstitial "ngrok free tier" warning page that appears to visitors of free tunnels.

### 11.4 Pricing

| Plan | Monthly Cost | Bandwidth | HTTP Requests | TCP Addresses |
|------|-------------|-----------|---------------|---------------|
| Free | $0 | 1 GB | 20,000 | Randomly assigned |
| Hobbyist | $8 (annual) / $10 (monthly) | 5 GB | 100,000 | 1 reserved |
| Pay-as-you-go | $20 + usage | 5 GB + $0.10/GB | 100,000 + $1/100k | 100 |

**Estimated monthly cost (single user)**: $8/month (Hobbyist annual plan) — free tier is functional but the interstitial warning page is not suitable for client demos.

### 11.5 SDK Options

| Platform | Package | Maintained | Last Updated | Notes |
|----------|---------|------------|--------------|-------|
| .NET (official REST API client) | `NgrokApi` (NuGet) | Yes — ngrok official | September 2025 (v0.16.0) | Targets .NET Standard 2.0; wraps the REST API |
| .NET (Agent Local API, community) | `Ngrok.AgentAPI` (NuGet) | Community | Active | Wraps the localhost:4040 agent API; simpler for dynamic tunnels |
| .NET (Aspire integration) | `CommunityToolkit.Aspire.Hosting.Ngrok` (NuGet) | Community | 2025 (v9.4.1-beta) | Adds ngrok as an Aspire resource for local dev |
| TypeScript | ngrok npm package | Yes — ngrok official | Active | Official Node.js SDK with tunnel management |

The `NgrokApi` NuGet package is ngrok's official .NET client for the REST API. For LemonDo, the combination of `NgrokApi` (for tunnel creation via the REST API) or `Ngrok.AgentAPI` (for local agent management) covers the PM-008 requirement completely.

### 11.6 Risks

| Risk | Level | Detail |
|------|-------|--------|
| API stability | Low | ngrok has maintained a stable API; the REST API is versioned |
| Vendor lock-in | Low | Tunneling is a commodity service; alternatives (Cloudflare Tunnel, localtunnel) exist |
| Rate limit impact | Low | Single-user demo scenarios are far below any plan's limits |
| Pricing risk | Low | Fixed monthly cost; no usage-based surprises at Hobbyist plan |
| Account/auth risk | Low | API key rotation is straightforward via ngrok dashboard |
| `NgrokApi` maturity | Medium | Package is at v0.16.0, indicating pre-1.0 status; API may have breaking changes between minor versions |

### 11.7 Alternatives

| Option | SDK (.NET) | Free Tier | Complexity | Recommendation |
|--------|-----------|-----------|------------|----------------|
| ngrok | Official (`NgrokApi`) | Yes (3 tunnels, interstitial) | Low | Primary |
| Cloudflare Tunnel | None — REST API | Free (unlimited) | Medium (requires cloudflared daemon) | Alternative — free but more complex setup |
| localtunnel | None — Node.js only | Free | Low | Not suitable — no .NET SDK, no reliability guarantees |

### 11.8 References

- [ngrok API Overview](https://ngrok.com/docs/api)
- [ngrok Pricing](https://ngrok.com/pricing)
- [NgrokApi NuGet Package](https://www.nuget.org/packages/NgrokApi/)
- [ngrok-api-dotnet GitHub](https://github.com/ngrok/ngrok-api-dotnet)
- [Ngrok.AgentAPI NuGet (community)](https://www.nuget.org/packages/Ngrok.AgentAPI)
- [CommunityToolkit.Aspire.Hosting.Ngrok NuGet](https://www.nuget.org/packages/CommunityToolkit.Aspire.Hosting.Ngrok)
