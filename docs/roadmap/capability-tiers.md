# Capability Tiers

> **Source**: Extracted from docs/ROADMAP.md
> **Status**: Active
> **Last Updated**: 2026-02-18

---

Beyond CP5, the roadmap is organized into capability tiers:

---

## Tier 1: AI & Agent Ecosystem

- **AI Assistant** - Natural language chat interface for managing tasks ("create a high-priority task to review the Q1 report by Friday", "what's overdue this week?", "summarize my board status"). Built as a new bounded context with a clean adapter pattern - swap between Azure OpenAI, Anthropic, or local models without domain changes.
- **MCP Server** - Expose LemonDo as a [Model Context Protocol](https://modelcontextprotocol.io) server. AI agents (Claude, Copilot, custom) can create/complete/move tasks, query board status, and manage projects through standardized tool definitions. The use case layer maps naturally to MCP tools - each command/query becomes an MCP tool with typed parameters and responses. This is the critical bridge to a future where agents orchestrate work across systems.
- **MCP Client** - LemonDo as an MCP client, connecting to external MCP servers for calendar, email, CRM, and code repository access. The AI assistant can pull context from a user's entire toolchain without custom integrations for each service.
- **Smart Categorization** - Auto-suggest priority, tags, and columns based on task title content and historical patterns.
- **Daily Digest** - AI-generated summary of what was accomplished, what's in progress, and what needs attention. Delivered via email or in-app notification.
- **Natural Language Filters** - "Show me tasks Marcus created last week that are still in progress" → query builder.

---

## Tier 2: Third-Party Integrations

- **Calendar Sync** - Two-way sync with Google Calendar and Outlook. Tasks with due dates appear as calendar events; calendar events can spawn tasks. OAuth2 integration via adapter pattern.
- **Messaging Notifications** - Slack, Microsoft Teams, WhatsApp, and SMS reminders for due dates, mentions, and status changes. Each channel is a Notification bounded context adapter - add new channels without changing domain logic.
- **Push Notifications** - Web Push API (building on the existing PWA service worker) for real-time browser notifications even when the app isn't open.
- **Email-to-Task** - Forward emails to a dedicated address to create tasks. Azure Functions + SendGrid inbound parse.
- **Zapier/Make Webhooks** - Outbound webhooks on domain events, enabling no-code integrations with 5000+ external services.

---

## Tier 3: Collaboration & Real-Time

- **Multi-Tenancy** - Organization-scoped data isolation using EF Core global query filters. The Board aggregate root naturally supports this - add an `OrganizationId` field and the filter does the rest.
- **Teams & Projects** - Group boards under projects, assign team members, track per-team velocity.
- **Real-Time Board Updates** - SignalR (or SSE for simpler clients) for live Kanban board sync across team members. See a task move the moment a colleague drags it.
- **Idempotency & Conflict Resolution** - Idempotency keys on mutations, optimistic concurrency with ETag headers, last-write-wins with conflict notification for simultaneous edits.
- **Message Queue / DLQ** - Azure Service Bus for reliable async event processing. Dead-letter queue for failed event handlers with retry policies and manual inspection.
- **Activity Feed** - Per-task and per-board activity streams showing who did what and when. Built on top of the domain events + audit trail.
- **Comments & Mentions** - Threaded comments on tasks with @mentions that trigger notifications.

---

## Tier 4: Advanced Task Modeling

- **Task Dependencies** - "Blocked by" relationships with visual dependency graphs. Automatic status propagation (parent blocked until all blockers resolved).
- **Subtasks & Checklists** - Nested task hierarchies with progress tracking.
- **Recurring Tasks** - Cron-based task generation via Hangfire or Azure Functions. "Every Monday at 9am, create a 'Weekly standup prep' task."
- **Time Tracking** - Start/stop timer on tasks, time estimates vs actuals, timesheet export.
- **Custom Fields** - Extensible task schema per workspace (dropdown, date, number, text fields). JSON column in SQLite/PostgreSQL with indexed extraction.
- **Templates** - Board templates ("Sprint Board", "Personal GTD") and task templates ("Bug Report", "Feature Request") with pre-filled fields.

---

## Tier 5: Reporting & Developer Experience

- **Dashboards** - Burndown/burnup charts, velocity tracking, workload heatmaps, time-to-completion trends. Custom dashboard builder with drag-and-drop widgets.
- **Public API + SDK** - Versioned REST API (/api/v1/, /api/v2/) with auto-generated TypeScript and C# SDKs via Scalar's code generation.
- **CLI Tool** - `lemondo tasks list --status=todo --priority=high` for power users and CI/CD integration.
- **GitHub/GitLab Integration** - Link commits and PRs to tasks, auto-transition task status on merge.
- **Browser Extension** - Quick-capture from any webpage ("clip this page as a task" with URL, title, and screenshot).

---

## Tier 6: Platform & Compliance

- **Desktop App** - Tauri (Rust shell wrapping the React frontend) for native desktop experience with system tray, global shortcuts, and offline-first storage.
- **Mobile Native** - React Native sharing domain types from `packages/shared-types` for iOS and Android.
- **SSO** - SAML 2.0 and OIDC for enterprise single sign-on (Okta, Azure AD, Auth0).
- **MFA Step-Up Authentication** - TOTP (RFC 6238) or WebAuthn/passkey as required second factor for break-the-glass protected data reveal, replacing current password re-entry. Also available as optional MFA for user login.
- **Task Content Encryption at Rest** - AES-256-GCM field-level encryption for task titles and descriptions (same pattern as protected data fields). Enables PHI-safe task storage without impacting query performance for non-encrypted fields.
- **Protected Data Reveal Rate Limiting** - Max N reveals per admin per time window (e.g., 5/hour). Anomaly detection alerts when unusual patterns emerge (e.g., bulk reveals).
- **Protected Data Reveal Notifications** - Email/Slack alerts to security team when protected data is revealed. Configurable per-organization notification channels.
- **Full HIPAA Certification** - BAA templates, annual security risk assessment, workforce training program, breach notification procedures, subcontractor BAA verification.
- **GDPR Compliance** - Right to erasure, data portability (full JSON export), consent management, Data Protection Officer workflow.
- **SOC 2 Type II** - The existing audit trail and encryption foundations make this achievable. Add formal policies, evidence collection, and annual audit.
- **Data Residency** - Region-specific database deployments for organizations with data sovereignty requirements.

---

## Tier 7: Product & Growth

This tier shifts focus from *what gets built* to *how the product grows*. Features here are about the business, not just the code.

### Monetization & Conversion

- **Freemium Model** - Free tier (1 user, 3 boards, 100 tasks), Pro tier (unlimited, integrations, themes), Team tier (collaboration, real-time, admin), Enterprise tier (HIPAA, SSO, data residency, custom fields).
- **Conversion Journey** - Land on page → interactive demo → sign up free (no credit card) → guided onboarding → habit formation → hit a limit → contextual upgrade prompt → Pro. No friction walls - users feel the value before seeing the price.
- **Upgrade Prompts at Natural Friction Points** - "You've used 3 of 3 boards. Upgrade to Pro for unlimited boards." Shown at the moment of need, not in a settings page.
- **Self-Serve Billing Portal** - Stripe integration for subscription management, invoices, plan changes. No sales calls required for Pro/Team tiers.
- **Revenue Metrics** - MRR, ARPU, conversion rate, LTV, churn rate. Dashboard for internal business health monitoring.

### Landing Page & Marketing

- **Landing Page** - Hero with clear value proposition, interactive live demo (try before signup), social proof (testimonials, company logos), comparison tables (vs Trello, Asana, Jira), pricing tiers with clear differentiators.
- **Use Case Pages** - SEO-optimized pages: "LemonDo for Healthcare Teams", "LemonDo for Freelancers", "LemonDo for Software Teams." Show the user what the platform can do *for them*, not what they can do *with the platform*.
- **Template Gallery** - Pre-built boards for specific workflows (Sprint Planning, Personal GTD, Client Onboarding, Content Calendar). Users start with a proven structure, not a blank board. Community-contributed templates with ratings.
- **Success Stories** - Case studies with real metrics: "How Team X reduced task completion time by 40%." Video testimonials, before/after screenshots.
- **Content Marketing** - Blog with productivity tips, workflow guides, and thought leadership. SEO funnel: search → blog → signup → onboarding.

### Retention & Customer Success

- **Onboarding Optimization** - Track drop-off at every onboarding step. A/B test variations. Measure time-to-first-completed-task (the primary activation metric).
- **Churn Prevention** - Identify at-risk users via usage signals (7+ days inactive, declining task creation, never completed a task). Automated re-engagement: email sequences, in-app prompts, "You've been missed" notifications.
- **Automated Customer Success** - Health score per user/team based on WATC (Weekly Active Task Completers), feature adoption depth, and engagement trends. Surface at-risk accounts to customer success team before they churn.
- **NPS / CSAT Surveys** - In-app micro-surveys at strategic moments (after completing 10th task, after first week, monthly). Track satisfaction trends, route detractors to support.
- **Lifecycle Emails** - Welcome sequence, feature discovery drips ("Did you know you can..."), milestone celebrations ("You completed 100 tasks!"), dormancy re-engagement.

---

## Tier 8: UX Excellence

Features focused on *how it feels* to use LemonDo, not just what it does.

- **Command Palette** - Cmd+K to search everything: tasks, boards, actions, settings. Power users never touch the mouse. Fuzzy search with recent items and contextual suggestions.
- **Keyboard Shortcuts** - Full keyboard navigation for every action. `N` for new task, `E` to edit, `D` to mark done, arrow keys to navigate board. Discoverable via `?` overlay.
- **Undo Everywhere** - Every destructive action gets a 5-second undo toast, not a confirmation dialog. Delete a task? Undo. Move a column? Undo. Archive a board? Undo. Faster and less disruptive than "Are you sure?" modals.
- **Batch Operations** - Multi-select tasks (Shift+click, Cmd+click), bulk move between columns, bulk tag, bulk delete, bulk change priority. Essential for power users managing dozens of tasks.
- **Progressive Disclosure** - New users see simplified UI (single board, basic task cards). Power features (filters, custom fields, keyboard shortcuts) reveal progressively as users demonstrate readiness. No overwhelming first-day experience.
- **Micro-Interactions** - Confetti on task completion (the P0 celebration feature), smooth drag physics with haptic-like feedback, satisfying checkbox animation, subtle board column count badges, progress ring on boards showing completion percentage.
- **Smart Defaults** - Pre-fill priority based on keywords ("urgent" → Critical, "meeting" → adds due date), auto-suggest tags from recent usage, remember last-used column for quick-add.
- **Contextual Empty States** - Not just "No tasks yet" but specific, actionable prompts: "Create your first task" with a single CTA, "No results for this filter" with a "Clear filters" link, "This column is empty - drag tasks here or create one" with inline quick-add.
- **Session Analytics** - PostHog for heatmaps, session replays, and funnel analysis. Understand how users *actually* use the product, not how the team *assumes* they do. Feed insights back into UX improvements.

---

## Tier 9: Reliability & Operations

The unglamorous work that separates a demo from a product people depend on.

### Networking, CDN & Edge

- **SPA + API Split** - The frontend is purely static files (JS, CSS, images, `index.html`). The API is the only dynamic origin. This split allows scaling each independently and leverage CDN for the entire frontend with near-100% cache hit rates.
- **CDN for Frontend** - Azure Front Door (or CloudFlare) as the global edge network. SPA assets served from 100+ edge locations worldwide. Users in Tokyo, Berlin, and São Paulo all get sub-50ms first-byte times. The API can be in a single region while the frontend is everywhere.
- **Cache-Control Strategy** - `index.html`: `no-cache` (always revalidated, ~1KB, instant). JS/CSS bundles: `Cache-Control: public, max-age=31536000, immutable` (content-hashed filenames mean cache-bust on every deploy, 1-year cache otherwise). Images/fonts: same immutable strategy. API responses: `Cache-Control: private, max-age=0` (TanStack Query manages client-side caching, not HTTP cache).
- **CDN Invalidation on Deploy** - CI/CD pipeline purges only `index.html` from CDN after frontend deploy. Hashed assets never need purging - new deploy means new hashes, old assets naturally expire. Zero stale content, zero cold-cache penalty for unchanged assets.
- **Load Balancer** - Azure Front Door doubles as a global load balancer with health-probe routing. SSL/TLS termination at the edge (no HTTPS overhead on API containers). WebSocket/SSE passthrough for real-time features (SignalR in CP3+). Automatic failover if a region's health probe fails.
- **WAF & DDoS Protection** - Web Application Firewall rules at the edge: block SQL injection, XSS, and known bot signatures before they reach the API. Azure DDoS Protection Standard for volumetric attack mitigation. Rate limiting at the edge layer (cheaper than rate limiting at the application layer).
- **API Gateway** - Azure API Management (or a lightweight reverse proxy) in front of the API for: request throttling per API key, request/response transformation, API versioning routing (`/api/v1/*` → container revision A, `/api/v2/*` → revision B), and analytics on API usage patterns.
- **Private Networking** - API containers, database, and Redis communicate over Azure VNet (private IPs, no public internet exposure). Only the load balancer/CDN has public endpoints. Database is never directly accessible from outside the VNet.
- **Geo-Distribution Strategy** - Phase 1: Single-region API + global CDN frontend (covers 80% of latency concerns since most interactions are SPA-local). Phase 2: Multi-region API with latency-based routing (Azure Traffic Manager) for global teams. Phase 3: Per-region database read replicas for data-heavy queries, writes always go to primary region.
- **Frontend Bundle Optimization** - Code-split the SPA with React.lazy + dynamic `import()` for route-level and heavy-component chunks. Critical path (shell, auth, board list) loads in the initial bundle; everything else (task detail modals, settings, admin, story page, rich-text editors) loads on demand. Combined with Vite's automatic chunk hashing and the CDN immutable cache strategy, this minimizes Time to First Paint and Largest Contentful Paint for returning and first-time users alike.
- **Client-Side Caching as a Tier** - TanStack Query's `staleTime` + `gcTime` means the browser itself is a cache tier. Board data cached for 30 seconds (stale-while-revalidate), user profile cached for 5 minutes, static reference data (priorities, statuses) cached for 1 hour. Combined with the service worker (PWA), many user interactions never hit the network at all.

### Scaling Strategy

- **Horizontal API Scaling** - Stateless API behind Azure Container Apps auto-scaler. JWT tokens mean no session affinity needed. Scale to zero on idle, burst to N replicas on demand.
- **Database Scaling Path** - SQLite (MVP) → PostgreSQL single instance → PostgreSQL with read replicas (read-heavy task queries go to replicas, writes to primary) → Citus for horizontal sharding by organization if multi-tenancy demands it. Each step is a repository implementation swap, no domain changes.
- **Caching Tiers** - L1: TanStack Query in-browser cache. L2: In-memory response cache on API (hot queries). L3: Redis for shared cache across API replicas (board data, user profiles). L4: CDN for static frontend assets. Cache invalidation via domain events - when a task changes, invalidate its Redis + board cache entries. TanStack Query handles client-side invalidation via `onSettled` callbacks.
- **Connection Pooling** - PgBouncer in front of PostgreSQL. EF Core configured for transient lifetime in pooled mode. Prevents connection exhaustion under load.
- **Load Testing & Capacity Planning** - k6 load tests simulating realistic user patterns (create tasks, drag between columns, switch views). Run against staging monthly to establish baseline and detect regressions. Know the breaking point before users find it. Performance budget: API p99 < 500ms at 10x current peak traffic.

### SLI, SLO, SLA

- **Service Level Indicators** - API response latency (p50, p95, p99), error rate (5xx / total), availability (successful health checks / total), task creation success rate, frontend Time to Interactive (TTI), Core Web Vitals (LCP, FID, CLS).
- **Service Level Objectives** - API p95 latency < 200ms, availability 99.9% (8.7h downtime/year), error rate < 0.1%, task creation success > 99.95%. These are internal targets - aggressive enough to catch degradation early.
- **Service Level Agreements** - External commitments per pricing tier: Free (no SLA), Pro (99.5%), Team (99.9%), Enterprise (99.95% with financial credits). SLAs are always looser than SLOs - the error budget between them provides breathing room.
- **Error Budgets** - Monthly error budget = 1 - SLO. If the budget is exhausted, freeze feature releases and focus on reliability. Dashboard showing real-time budget consumption. Alerts at 50%, 75%, 90% burn rate.
- **Monitoring Stack** - OpenTelemetry → Aspire Dashboard (dev) → Grafana + Prometheus (production). PagerDuty for on-call alerting. Uptime monitoring via external probe (Checkly or Pingdom). Synthetic monitoring for critical user journeys.

### Disaster Recovery & Backups

- **RPO / RTO Targets** - Recovery Point Objective: 1 hour (max data loss). Recovery Time Objective: 4 hours (max downtime). These drive the backup frequency and recovery procedure design.
- **Backup Strategy** - Automated PostgreSQL backups every hour to geo-redundant Azure Blob Storage. Daily full backup + hourly WAL archiving for point-in-time recovery. Backup encryption with customer-managed keys.
- **Backup Verification** - Weekly automated restore-to-staging to prove backups actually work. An untested backup is not a backup.
- **Geo-Redundancy** - Active-passive across two Azure regions. Primary serves traffic, secondary receives replicated data. Failover via Azure Traffic Manager with health probe. Manual failover trigger (not automatic - avoid split-brain).
- **Runbooks** - Documented step-by-step recovery procedures for every failure scenario: database corruption, region outage, credential compromise, dependency failure. Practiced quarterly via game days.

### Deployment & Release Strategy

- **Deployment Model** - Blue/green deployments via Azure Container Apps revisions. New version deploys to "green" slot, health checks validate, traffic shifts 10% → 50% → 100% (canary progression). Instant rollback by shifting traffic back to "blue."
- **Database Migrations - EXPAND / MIGRATE / SHRINK** - Zero-downtime schema changes:
  1. **EXPAND**: Add new columns/tables alongside existing ones. Both old and new code work against the expanded schema. Deploy the expanded schema first, then deploy new application code.
  2. **MIGRATE**: Dual-write to old and new columns. Backfill historical data with a background job. Validate data consistency between old and new.
  3. **SHRINK**: Once all application code reads from new columns and old columns have zero reads, drop old columns in a subsequent release. Never expand and shrink in the same release.
- **Release Cadence** - Continuous deployment to staging on every merge to `develop`. Weekly release trains to production (Tuesday mornings, never Fridays). Hotfix path for critical issues: `hotfix/*` branch → direct to `main` + `develop`.
- **Rollback Procedure** - Application rollback: revert Container Apps revision (< 30 seconds). Database rollback: never - EXPAND/MIGRATE/SHRINK means every migration is forward-only and backward-compatible.

### Feature Flags & Experimentation

- **Feature Flag System** - LaunchDarkly (or open-source Unleash) for runtime feature control. Every new feature ships behind a flag. Flags enable: gradual rollouts (1% → 10% → 100%), kill switches (disable broken features without deploy), beta programs (enable for specific users/organizations).
- **A/B Testing** - Experimentation framework for UX decisions. Split traffic between variants, measure impact on WATC (the north star metric). Examples: "Does confetti on task completion increase weekly completion rate?" "Does the quick-add bar perform better at the top or bottom of the board?"
- **Flag Lifecycle** - Flags are not permanent. Every flag has an expiry date. After full rollout, the flag is removed in the next release cycle. Stale flags are tech debt.

### Environments & Promotion

- **Environment Chain** - `local` → `dev` → `staging` → `production`. Each environment is a full replica of the production stack (API + frontend + database + Redis + monitoring). No "works in dev, breaks in prod" surprises.
- **Staging = Production Mirror** - Same container images, same environment variables (except secrets), same database schema, same feature flags (with staging-specific overrides). Staging receives production-like traffic via load replay.
- **Promotion Gates** - Merge to `develop` → auto-deploy to `dev` → automated test suite passes → promote to `staging` → QA approval + smoke tests → promote to `production` with canary progression. Any gate failure blocks promotion.
- **Environment Parity** - Infrastructure-as-code (Terraform) ensures all environments are structurally identical. Drift detection alerts if staging diverges from production config.

### Developer Experience & Containers

- **Dev Containers** - `.devcontainer/` configuration for VS Code / GitHub Codespaces. One-click development environment with .NET 10 SDK, Node.js 23, pnpm, all tools pre-installed. New developer goes from clone to running app in under 5 minutes.
- **Docker Compose for Local Dev** - `docker compose up` starts the full stack: API, frontend, SQLite (or PostgreSQL), Redis, Aspire Dashboard, mock mail server. Matches production topology without cloud dependencies.
- **Testcontainers** - Integration tests spin up real PostgreSQL and Redis containers via Testcontainers for .NET. No in-memory fakes for integration testing - test against the real thing, then throw it away.
- **Seed Data & Fixtures** - Development seed script that populates realistic data: users with different roles, boards with tasks in various states, audit entries, onboarding progress. Developers always work with representative data, not empty databases.
- **Hot Reload Everywhere** - Backend: `dotnet watch`. Frontend: Vite HMR. Both restart automatically on file change. Aspire orchestrates both with `AddJavaScriptApp` - one terminal, full stack.

### Incident Management

- **Severity Levels** - SEV1 (total outage, all users affected), SEV2 (major feature broken, significant user impact), SEV3 (minor feature degraded, workaround available), SEV4 (cosmetic or low-impact issue).
- **On-Call Rotation** - PagerDuty rotation with primary + secondary. Escalation policy: page primary → 5 min → page secondary → 10 min → page engineering lead. On-call handoff includes "state of the world" briefing.
- **Incident Response** - Detect → Acknowledge → Communicate (status page update) → Mitigate (restore service) → Resolve (permanent fix) → Review (post-mortem within 48 hours).
- **Post-Mortems** - Blameless post-mortem for every SEV1/SEV2. Document: timeline, root cause, impact, what went well, what went wrong, action items with owners and deadlines. Published internally for organizational learning.
- **Status Page** - Public status page (Statuspage.io or Instatus) showing real-time service health. Automated incident creation from monitoring alerts. Subscriber notifications via email and webhook.

### Security Operations

- **Dependency Scanning** - Dependabot (GitHub) + Snyk for automated CVE detection in NuGet and npm packages. PRs auto-created for security patches. Critical CVEs block deployment pipeline.
- **SAST / DAST** - Static analysis (SonarQube or CodeQL) on every PR. Dynamic analysis (OWASP ZAP) against staging environment weekly. Findings triaged by severity, critical/high block release.
- **Penetration Testing** - Annual third-party pen test. Findings documented, remediated, and verified. Report available to Enterprise customers on request.
- **Secrets Rotation** - Azure Key Vault for all secrets. JWT signing keys rotate every 90 days (automated). Database credentials rotate every 30 days. API keys for third-party services rotate on compromise or annually.
- **Supply Chain Security** - Lockfile integrity verification in CI. Package provenance checking. No `npm install` in production - only `pnpm install --frozen-lockfile` from verified lockfile.

### Cost Management

- **Cloud Cost Monitoring** - Azure Cost Management with per-environment and per-service tagging. Monthly budget alerts at 80% and 100% of target. Cost anomaly detection for unexpected spikes.
- **Right-Sizing** - Quarterly review of container resource allocations (CPU/memory). Auto-scale policies tuned to actual traffic patterns, not worst-case estimates. Scale-to-zero for non-production environments outside business hours.
- **Reserved Capacity** - Azure Reserved Instances for predictable production workloads (database, Redis). Pay-as-you-go for burst capacity. Estimated 30-40% savings over on-demand pricing.
