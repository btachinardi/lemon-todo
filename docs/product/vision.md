# Vision

> **Source**: Extracted from docs/PRD.draft.md §1, docs/PRD.md §Review Summary + §4 Updated Success Criteria, docs/PRD.2.draft.md §1 + §10 + §13
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Product Vision

**LemonDo** is a task management platform that combines the simplicity of a to-do list with the power of a Kanban board. It is designed for individuals and small teams who need a secure, compliant, and delightful way to organize their work.

### Mission Statement

Empower users to capture, organize, and complete their work with zero friction, while maintaining enterprise-grade security and compliance standards.

### Target Audience

- **Primary**: Knowledge workers, freelancers, and small team leads who need a personal/team task management tool
- **Secondary**: Organizations in regulated industries (healthcare, finance) that require HIPAA-compliant task management
- **Tertiary**: Product managers and team leads who need visibility into team progress

### Value Proposition

LemonDo is the only task management tool that combines consumer-grade UX with enterprise-grade compliance. Users get a beautiful, fast, mobile-first experience while administrators get full auditability and HIPAA-compliant data handling.

---

## v1 Key Insights (from Scenario Analysis)

After creating detailed user scenarios and personas, several insights emerged that refine the initial PRD:

1. **Quick-add is the killer feature**: Sarah's workflow shows that the #1 interaction is rapidly capturing tasks. The task creation UX must be lightning-fast (< 2 taps/clicks to add a task).

2. **Mobile-first is not optional**: Sarah uses her phone 60% of the time. Kanban drag-and-drop on mobile must be native-feeling (long-press + drag), not a desktop afterthought.

3. **Onboarding must be emotionally rewarding**: The create-then-complete loop in onboarding needs micro-celebrations (animations, feedback) to establish positive association.

4. **Protected data redaction is the default, not the exception**: Diana's admin workflow shows that revealing protected data should require explicit action. Default to redacted everywhere.

5. **Offline-first is table stakes for mobile**: Sarah's flight scenario proves PWA offline must work for core operations (view, create, complete tasks).

6. **Analytics must be privacy-first**: HIPAA requires all analytics events to hash or exclude protected data. The analytics architecture must be designed with this constraint from day one.

---

## v1 Updated Success Criteria

Based on scenario analysis, the success metrics are refined as follows:

| Metric | Original Target | Revised Target | Rationale |
|--------|----------------|----------------|-----------|
| Time to first task created | Not defined | < 60 seconds from signup | S01: onboarding speed |
| Quick-add usage | Not defined | > 70% of tasks via quick-add | S02: quick capture dominance |
| Mobile session share | Not defined | > 40% of sessions | S02, S06: mobile-first thesis |
| Offline operations per week | Not defined | Measured (baseline) | S06: offline usage tracking |
| Protected data reveal frequency | Not defined | < 10% of admin sessions | S05: minimal protected data exposure |

The following success metrics carry over without modification from the initial PRD:

| Metric | Target | Measurement |
|--------|--------|-------------|
| Registration completion rate | > 80% | Analytics funnel |
| Onboarding completion rate | > 60% | Analytics funnel |
| Day-1 retention | > 40% | Cohort analysis |
| Day-7 retention | > 25% | Cohort analysis |
| Task completion rate | > 50% of created tasks | Feature analytics |
| Lighthouse score | > 90 all categories | Automated CI check |
| Test coverage (backend domain) | > 90% | CI coverage report |
| Test coverage (frontend) | > 80% | CI coverage report |
| API p95 latency | < 200ms | APM monitoring |
| Zero critical security findings | 0 | Security scans |

---

## v1 Out of Scope (MVP)

- Real-time collaboration (multi-user editing)
- File attachments on tasks
- Calendar view
- Recurring tasks
- Time tracking
- Third-party integrations (Slack, Jira, etc.)
- Native mobile apps (iOS/Android)
- Team workspaces with shared boards
- Billing and subscription management

---

## v1 Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| HIPAA compliance complexity | High | Medium | Consult compliance checklist, encrypt all PII at rest |
| SQLite scaling limits | Medium | Low | Abstract repository, easy to swap to PostgreSQL |
| Aspire maturity gaps | Medium | Low | Fallback to standard Docker Compose |
| OAuth provider changes | Low | Low | Abstract behind provider interface |
| Scope creep | High | High | Strict MVP scope, defer to "Out of Scope" |

---

## v2 Vision Statement

> **Status**: Draft (v2)

LemonDo v1 is a feature-complete, production-grade task management platform with HIPAA-ready security, PWA offline support, and a polished UX across 1,094 tests and 7 patch releases. It demonstrates best-in-class SDLC practices: DDD architecture, TDD, gitflow, structured documentation, and checkpoint-based delivery.

**v2 evolves LemonDo from a task management app into Bruno's personal development command center.** The theme is "Bruno" — this platform becomes the unified hub for managing projects, communications, people, and AI agent workflows. The quality bar set in v1 (architecture, testing, documentation) carries forward as the foundation.

### Why v2?

The v1 SDLC practices (specs, guidelines, checkpoints, documentation hierarchy) are the best Bruno has ever implemented. Rather than starting fresh for personal tooling, v2 builds on this proven foundation to solve real daily pain points:

- **Scattered project management** — repos, worktrees, deployments, and tasks live in different tools
- **Fragmented communications** — important messages lost across Gmail, WhatsApp, Discord, Slack, LinkedIn
- **Disconnected people/company knowledge** — no central record of relationships, preferences, learnings
- **Manual agent orchestration** — Claude Code sessions require manual setup, task assignment, and monitoring

### v2 Success Criteria

> **Status**: Draft (v2)

| Metric | Target |
|--------|--------|
| Projects registered and actively managed | 3+ |
| Communication channels connected | 3+ (Gmail + Slack + Discord minimum) |
| Agent sessions completed successfully | 10+ per week |
| Tasks auto-created by agents | 50% of bug fix tasks |
| Time to find a person's communication history | < 10 seconds |
| Morning inbox review time | < 5 minutes (down from 15+) |
| Parallel agent development efficiency | 3x faster than sequential manual work |

### v2 Out of Scope

> **Status**: Draft (v2)

- Multi-user/multi-tenant support (this is Bruno's personal tool)
- Mobile-specific UX for new modules (desktop-first, responsive later)
- Full CRM features (sales pipeline, deal tracking)
- Calendar integration (potential v3)
- Billing/invoicing
- Video calling integration
- Social media posting/management

---

## Technology Considerations (v2)

> **Status**: Draft (v2)

| Concern | Approach | Notes |
|---------|----------|-------|
| Git operations | `simple-git` (Node.js) or direct CLI calls | Worktree management |
| Process management | Node.js `child_process` or PM2-like runner | Dev server start/stop |
| Gmail integration | Google APIs (OAuth2 + Gmail API) | Read/send emails |
| WhatsApp | WhatsApp Business API or Baileys bridge | Research needed |
| Discord | Discord.js or REST API | Bot token |
| Slack | Slack Bolt SDK or REST API | App token |
| Claude Code | CLI subprocess or Claude Agent SDK | Agent sessions |
| ngrok | ngrok npm package or API | Tunnel management |
| Real-time streaming | WebSocket (Socket.io or native) or SSE | Agent output |
| Local DB expansion | SQLite (existing) extended with new tables | Same provider strategy |

---

## Open Questions (v2)

> **Status**: Draft (v2)

1. **WhatsApp integration**: WhatsApp Business API requires a business account. Is Baileys (unofficial bridge) acceptable for personal use?
2. **LinkedIn**: No official messaging API. Scraping is fragile and ToS-violating. Defer or find alternative?
3. **Agent budget granularity**: Should budget be per-session, per-task, per-project, or per-day?
4. **Agent approval workflow**: Should agents auto-commit to feature branches, or require explicit approval for each commit?
5. **Technology pivot**: v1 is .NET + React. Should v2 modules use the same stack, or consider NestJS (per global CLAUDE.md tech stack) for new backend modules?
6. **Data model evolution**: How do we evolve the existing DB schema without breaking v1 features?
