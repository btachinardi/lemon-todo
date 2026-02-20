# WhatsApp Business Cloud API

> **Source**: Extracted from docs/operations/research.md §9
> **Status**: Active
> **Last Updated**: 2026-02-18

---

> **Date Researched**: 2026-02-18
> **Purpose**: WhatsApp messaging integration for the `comms` module — reading and sending WhatsApp messages from the unified inbox.
> **Recommendation**: Do Not Use (official Cloud API) — requires business account verification, dedicated phone number, and is designed for B2C messaging at scale, not personal inbox reading. Use the Baileys bridge approach for personal use, but flag as high-risk.

---

## 9. WhatsApp Business Cloud API

### 9.1 Capabilities

Official WhatsApp Cloud API (Meta):
- Send text, template, media, and interactive messages to WhatsApp users
- Receive inbound messages via webhook (HTTP POST to a registered endpoint)
- Send pre-approved Message Templates for outbound marketing/utility messages
- Webhook delivers: message received, delivery receipt, read receipt, reaction events

What the official API cannot do (critical limitation for LemonDo):
- It does not provide a way to read your existing WhatsApp inbox history
- It is a business-to-customer API, not an inbox reading API
- Receiving messages only works if people send messages TO your business number; you cannot passively monitor a personal WhatsApp account

### 9.2 Authentication

Official Cloud API: Bearer token (permanent system user token) generated in Meta Business Suite. No OAuth flow needed after setup.

However, setup requires:
1. A verified Meta Business Manager account (business verification takes 2-14 days)
2. A dedicated phone number not previously registered on WhatsApp (personal numbers cannot be used)
3. A WhatsApp Business Account linked to the Meta Business Manager

**Auth flow**: Static bearer token stored in Azure Key Vault; webhook verification via `hub.verify_token` handshake during registration

**Scopes required**: `whatsapp_business_messaging`, `whatsapp_business_management` (Meta app-level permissions, not OAuth scopes)

### 9.3 Rate Limits

| Tier | Limit | Window | Notes |
|------|-------|--------|-------|
| Unverified business | 250 conversations | per 24 hours | Very restrictive |
| Verified business (Tier 1) | 1,000 conversations | per 24 hours | After business verification |
| Verified business (Tier 2) | 10,000 conversations | per 24 hours | After hitting Tier 1 volume |

Note: These limits apply to outbound messaging. Receiving inbound messages is not rate-limited at the API level.

### 9.4 Pricing

Pricing is conversation-based (24-hour conversation windows), with four categories: Marketing, Utility, Authentication, and Service (customer-initiated). Each business account receives 1,000 free service conversations per month. Rates vary by country. For Brazil (Bruno's market), rates are approximately $0.0315 per marketing conversation, $0.0105 per service conversation.

**Estimated monthly cost (single user)**: $0 if only receiving messages from contacts (service conversations within free 1,000/month); potentially $0-$5 for occasional sending.

### 9.5 SDK Options

| Platform | Package | Maintained | Last Updated | Notes |
|----------|---------|------------|--------------|-------|
| .NET | None — official | N/A | N/A | No official Meta .NET SDK; use REST API directly |
| TypeScript | None — official | N/A | N/A | No official Meta TypeScript SDK; use REST API directly |
| Node.js (community) | `@whiskeysockets/baileys` | Community | Active (2025) | Unofficial bridge — different approach entirely |

### 9.6 Risks

| Risk | Level | Detail |
|------|-------|--------|
| API stability | Medium | Official API is stable but requires business verification infrastructure; personal use is not the intended use case |
| Vendor lock-in | High | Meta platform with no portable data export for conversation history |
| Rate limit impact | Low | At single-user personal scale, 1,000 free service conversations/month is more than sufficient |
| Pricing risk | Low | Costs are predictable and low at personal scale |
| Account ban risk (Baileys) | High | Unofficial bridges (Baileys) use reverse-engineered WhatsApp Web protocol; accounts can be banned by Meta without warning; this violates WhatsApp's Terms of Service |
| Business verification | High | Official API requires a legal business entity, verified Meta Business Manager, and a dedicated phone number — significant friction for a personal tool |

### 9.7 Alternatives

**Baileys (unofficial bridge)**: The `@whiskeysockets/baileys` npm library uses the WhatsApp Web WebSocket protocol to act as a WhatsApp Web client. It can read and send personal WhatsApp messages. However, it violates WhatsApp's Terms of Service and risks permanent account bans. A Node.js sidecar process running Baileys could expose a REST API that LemonDo calls. High-risk but the only way to read a personal WhatsApp inbox programmatically.

**Evolution API**: An open-source self-hosted gateway that wraps Baileys and provides a REST API. Same risk profile as Baileys — unofficial, ToS-violating — but easier to deploy.

**Defer to v3**: Given the friction and risk, WhatsApp integration is the most complex channel to implement correctly. Consider deferring until v3 in favor of completing the lower-risk channels (Gmail, Discord, Slack) first.

| Option | SDK (.NET) | Risk | Complexity | Recommendation |
|--------|-----------|------|------------|----------------|
| Official Cloud API | REST only | Medium (business setup) | Medium | Do Not Use for personal inbox reading |
| Baileys (Node.js sidecar) | None — REST bridge | High (ToS violation) | High | Needs Spike — only option for personal inbox |
| Evolution API | None — REST bridge | High (ToS violation) | High | Alternative to raw Baileys |

### 9.8 References

- [WhatsApp Business Platform Overview](https://business.whatsapp.com/developers/developer-hub)
- [WhatsApp Cloud API Pricing](https://business.whatsapp.com/products/platform-pricing)
- [WhatsApp Cloud API Webhooks](https://business.whatsapp.com/blog/how-to-use-webhooks-from-whatsapp-business-api)
- [Baileys GitHub (unofficial)](https://github.com/WhiskeySockets/Baileys)
- [Evolution API (Baileys wrapper)](https://github.com/EvolutionAPI/evolution-api)
