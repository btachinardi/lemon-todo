# Roadmap

> Plans for capability expansion beyond the v1 MVP checkpoints.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [capability-tiers.md](./capability-tiers.md) | Nine capability tiers (Tiers 1–9) covering AI/agents, integrations, collaboration, advanced task modeling, reporting, platform/compliance, product growth, UX excellence, and reliability/operations | Active |
| [v2-checkpoints.md](./v2-checkpoints.md) | v2 implementation plan: 6 checkpoints (CP6–CP11), 8 technology spikes, ~155 tasks, critical path analysis | Active |

---

## Summary

The LemonDo roadmap picks up after Checkpoint 5 (the first stable v1.0.0 release) and is organized into nine capability tiers. Each tier groups related features by theme rather than strict delivery sequence.

**Tier 1: AI & Agent Ecosystem** introduces an AI Assistant with natural language task management, an MCP Server exposing LemonDo as a tool provider for AI agents, and an MCP Client for pulling context from external services (calendar, email, CRM). Smart categorization and daily digest features round out this tier.

**Tiers 2–3** cover Third-Party Integrations (calendar sync, Slack/Teams notifications, email-to-task, webhooks) and Collaboration & Real-Time features (multi-tenancy, team projects, SignalR board sync, activity feeds, threaded comments).

**Tier 4** deepens the task model with dependencies, subtasks, recurring tasks, time tracking, custom fields, and templates. **Tier 5** adds developer-facing features: dashboards, a public API with SDKs, a CLI tool, GitHub/GitLab integration, and a browser extension.

**Tier 6** addresses Platform & Compliance: native desktop (Tauri), mobile (React Native), SSO (SAML/OIDC), MFA step-up, full HIPAA certification, GDPR compliance, and SOC 2 Type II. **Tier 7** shifts focus to Product & Growth — freemium monetization, landing page, conversion funnel, churn prevention, and lifecycle emails.

**Tier 8** refines UX Excellence with command palette, keyboard shortcuts, undo everywhere, batch operations, micro-interactions, and session analytics. **Tier 9** covers the operational infrastructure required for a product people depend on: CDN/edge networking, scaling strategy, SLI/SLO/SLA definitions, disaster recovery, deployment and release strategy, feature flags, environment promotion, developer experience tooling, incident management, security operations, and cost management.

The v2 implementation checkpoints (CP6–CP10) will translate tier priorities into a concrete delivery sequence, focusing first on the four new product modules (Projects, Communications, People, Agent Sessions) before layering in the broader capability tiers.
