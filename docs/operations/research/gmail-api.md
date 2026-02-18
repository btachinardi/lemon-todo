# Gmail API

> **Source**: Extracted from docs/operations/research.md §8
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date Researched**: 2026-02-18
> **Purpose**: Email integration for the `comms` module — reading inbox threads, sending replies, and receiving real-time push notifications for new emails.
> **Recommendation**: Use — mature, well-documented REST API with an official .NET SDK, fits LemonDo's single-user OAuth flow, and free quota is substantial for personal use.

---

## 8. Gmail API

### 8.1 Capabilities

- Read inbox messages, threads, labels, and attachments via `messages.list`, `messages.get`, `threads.list`, `threads.get` — [Gmail API Reference](https://developers.google.com/workspace/gmail/api/reference/rest)
- Send and reply to emails via `messages.send` and `drafts.send`
- Manage labels (read/create/update/delete) via the `labels` resource
- Search messages using Gmail's full query syntax via the `q` parameter on `messages.list`
- Watch for real-time new-mail events using `users.watch` combined with Google Cloud Pub/Sub push notifications — [Push Notifications Guide](https://developers.google.com/workspace/gmail/api/guides/push)
- Mark as read/unread, archive, trash via `messages.modify`
- Access metadata only (headers, labels, no body) via the `metadata` format — useful for polling without reading full content

### 8.2 Authentication

OAuth 2.0 Authorization Code flow is required. The user (Bruno) grants LemonDo access to their Gmail account via a Google OAuth consent screen. The .NET backend must register a redirect URI with Google, exchange the authorization code for tokens, and store the refresh token securely (Azure Key Vault recommended). Access tokens expire after 1 hour; the SDK handles refresh automatically.

**Auth flow**: OAuth 2.0 Authorization Code with offline access; redirect URI registered in Google Cloud Console; tokens stored server-side (never in browser)

**Scopes required**:

| Scope | Purpose | Classification |
|-------|---------|---------------|
| `https://www.googleapis.com/auth/gmail.readonly` | Read all messages and metadata | Restricted |
| `https://www.googleapis.com/auth/gmail.send` | Send emails and replies | Sensitive |
| `https://www.googleapis.com/auth/gmail.modify` | Read + modify (mark read, archive) — use instead of readonly if reply is needed | Restricted |

Note: Restricted scopes require a Google OAuth verification process if the app is ever made public. For a single-user personal app used only by Bruno, verification is not required — the app stays in "testing" mode with Bruno as the sole test user.

Push notifications additionally require a Google Cloud Pub/Sub topic and a public HTTPS endpoint (or Azure Service Bus relay) to receive notifications. The `watch()` call must be renewed every 7 days.

### 8.3 Rate Limits

| Limit Type | Quota | Window | Notes |
|------------|-------|--------|-------|
| Per-project | 1,200,000 units | per minute | Shared across all users of the app |
| Per-user | 15,000 units | per minute | Per authenticated user |
| `messages.list` | 5 units | per call | Very low cost |
| `messages.get` | 5 units | per call | Low cost |
| `messages.send` / `drafts.send` | 100 units | per call | High cost — throttle sends |

At single-user scale (Bruno), hitting the per-user limit of 15,000 units/minute would require sustained bulk operations. Normal inbox polling (e.g., every 60 seconds fetching 20 message headers) uses approximately 100 units/minute — well within limits. Prefer push notifications over polling to stay near-zero on quota usage.

Exceeded limits return HTTP 429 with a `Retry-After` header. The official .NET client library implements exponential backoff automatically.

### 8.4 Pricing

Gmail API access is free for standard usage (no charge for API calls). Google Cloud Pub/Sub costs approximately $0.04 per million messages, and push notification volume for a single-user inbox is negligible (tens to hundreds of messages per day).

**Estimated monthly cost (single user)**: Free tier sufficient — Pub/Sub charges for push notifications are effectively $0.00 at single-user personal inbox scale.

### 8.5 SDK Options

| Platform | Package | Maintained | Last Updated | Notes |
|----------|---------|------------|--------------|-------|
| .NET | `Google.Apis.Gmail.v1` (NuGet) | Yes — Google official | January 2026 (v1.73.0.4029) | Targets .NET Framework 4.6.2+, .NET Standard 2.0, .NET 6.0+; compatible with .NET 10 |
| TypeScript | `googleapis` (npm) | Yes — Google official | Active | Full Google APIs client for Node.js/TypeScript |

The `Google.Apis.Gmail.v1` NuGet package is Google's official .NET client, auto-generated from the API discovery document. It includes built-in OAuth2 token management (`GoogleWebAuthorizationBroker`), automatic retry with exponential backoff, and strongly-typed request/response objects.

### 8.6 Risks

| Risk | Level | Detail |
|------|-------|--------|
| API stability | Low | Gmail API has been stable for years; Google provides advance notice of breaking changes |
| Vendor lock-in | Medium | Google-specific OAuth and quota system; switching to IMAP would require significant refactor but is feasible |
| Rate limit impact | Low | Single-user personal inbox; normal usage is far below per-user limits |
| Pricing risk | Low | Free API with negligible Pub/Sub costs; no scenario where costs spike unexpectedly for personal use |
| OAuth verification | Low | Single-user app in "testing" mode avoids the Google verification process entirely; risk emerges only if Bruno shares the app with others |
| Push notification renewal | Medium | The `watch()` call expires every 7 days; requires a background job to renew or Gmail will stop delivering push notifications silently |

### 8.7 Alternatives

There is no meaningful alternative to the Gmail API for reading a Gmail inbox programmatically. IMAP access via `MailKit` (a well-maintained .NET library) is a protocol-level alternative that avoids OAuth entirely (uses App Password), but provides no push notifications, no label management, and no Gmail-specific features. For the comms module's goal of real-time inbox integration, the Gmail API is the only viable choice.

| Option | SDK (.NET) | Free Tier | Complexity | Recommendation |
|--------|-----------|-----------|------------|----------------|
| Gmail API (REST) | Official (`Google.Apis.Gmail.v1`) | Free + cheap Pub/Sub | Medium (OAuth + Pub/Sub webhook) | Primary |
| IMAP via MailKit | Community (`MailKit`) | Free | Low (App Password, no OAuth) | Fallback — no real-time, no labels |

### 8.8 References

- [Gmail API Overview](https://developers.google.com/workspace/gmail/api/guides)
- [Gmail API Quota and Rate Limits](https://developers.google.com/workspace/gmail/api/reference/quota)
- [Gmail API OAuth Scopes](https://developers.google.com/workspace/gmail/api/auth/scopes)
- [Gmail Push Notifications Guide](https://developers.google.com/workspace/gmail/api/guides/push)
- [Google.Apis.Gmail.v1 NuGet Package](https://www.nuget.org/packages/Google.Apis.Gmail.v1)
- [Google API .NET Client Library — GitHub](https://github.com/googleapis/google-api-dotnet-client)
