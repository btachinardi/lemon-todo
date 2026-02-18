<!-- Source: New file (top-level index) -->
<!-- Status: Active -->
<!-- Last Updated: 2026-02-18 -->

# LemonDo Documentation

> The complete documentation for LemonDo: from v1 task management platform through v2 personal development command center.

---

LemonDo began as a HIPAA-compliant task management platform with consumer-grade UX and enterprise-grade compliance, targeting three personas: Sarah (the overwhelmed freelancer), Marcus (the team lead), and Diana (the system administrator). v1 shipped at v1.0.7 with 1,094 tests across backend, frontend, and E2E, a full Azure cloud deployment, and five progressive checkpoints from inception to production in five days.

v2 evolves LemonDo from a task management app into Bruno's personal development command center. The theme is "Bruno" — this platform becomes the unified hub for four new modules: **Projects** (git repository management, worktrees, local dev servers, ngrok tunnels), **Comms** (unified inbox across Gmail, WhatsApp, Discord, Slack, LinkedIn, and GitHub), **People** (lightweight CRM for relationship and company tracking), and **Agents** (Claude Code session orchestration, work queues, and budget management). v2 is single-user, local-first, and designed for a solo developer power user.

The documentation is organized into seven subdirectories. Product requirements live in `product/`, domain design in `domain/`, user storyboards in `scenarios/`, technical standards in `architecture/`, future plans in `roadmap/`, operational guides in `operations/`, and the development history in `journal/`. Every folder has an INDEX.md that summarizes the contents and provides links to individual files. There is no duplicate content — each piece of information lives in exactly one place.

v1 content across all directories is marked **Active**. v2 content is marked **Draft (v2)** until promoted to Active during implementation. Both coexist in the same structure — there are no versioned documents (`PRD-v1.md`, `PRD-v2.md`), only a single hierarchy that grows.

---

## Contents

| Directory | Purpose | Status |
|-----------|---------|--------|
| [product/](./product/INDEX.md) | Product vision, personas, functional requirements by module, NFRs, analytics | Active |
| [domain/](./domain/INDEX.md) | DDD bounded contexts, shared kernel, entity relationships, API design | Active |
| [scenarios/](./scenarios/INDEX.md) | User storyboards and interaction flows for v1 personas and v2 modules | Active + Draft (v2) |
| [architecture/](./architecture/INDEX.md) | Backend, frontend, testing, security, infrastructure standards, and ADRs | Active |
| [roadmap/](./roadmap/INDEX.md) | Nine capability tiers and v2 implementation checkpoints | Active + Draft (v2) |
| [operations/](./operations/INDEX.md) | Development setup, deployment, release process, technology research | Active |
| [journal/](./journal/INDEX.md) | Chronological development journal: v1 history and v2 decisions | Active + Draft (v2) |

---

## Quick Links

| What you need | Where to look |
|---------------|---------------|
| Project vision and goals | [product/vision.md](./product/vision.md) |
| User personas | [product/personas.md](./product/personas.md) |
| Module functional requirements | [product/modules/](./product/modules/) |
| Bounded context map | [domain/INDEX.md](./domain/INDEX.md) |
| Individual context designs | [domain/contexts/](./domain/contexts/) |
| API endpoint reference | [domain/api-design.md](./domain/api-design.md) |
| Backend architecture and conventions | [architecture/backend.md](./architecture/backend.md) |
| Frontend architecture and conventions | [architecture/frontend.md](./architecture/frontend.md) |
| Testing standards and pyramid | [architecture/testing.md](./architecture/testing.md) |
| Architecture decision records | [architecture/decisions/](./architecture/decisions/) |
| Local dev setup and CLI reference | [operations/development.md](./operations/development.md) |
| Release process (gitflow) | [operations/releasing.md](./operations/releasing.md) |
| Azure deployment and CI/CD | [operations/deployment.md](./operations/deployment.md) |
| Technology version lock and research | [operations/research.md](./operations/research.md) |
| Full v1 development history | [journal/v1.md](./journal/v1.md) |

---

## v2 Planning

v2 is in the planning phase. The current focus is documentation decomposition, v2 requirements expansion, and checkpoint-based implementation planning.

| Document | Purpose | Status |
|----------|---------|--------|
| [PLANNING.md](./PLANNING.md) | planning roadmap: phases, tasks, and delivery sequence | Active |
| [PRD.md](./PRD.md) | product requirements: vision, modules, scenarios, and domain design notes | Draft |

**Phase summary:**

| Phase | Focus | State |
|-------|-------|-------|
| Phase 0 | Workspace setup | Done |
| Phase 1 | Documentation decomposition | In progress |
| Phase 2 | v2 requirements expansion | TODO |
| Phase 3 | Implementation planning | TODO |
| Implementation | Build v2 modules (CP6–CP10) | TODO |

See [PLANNING.md](./PLANNING.md) for the detailed task list and per-phase status.
