# LemonDo v2 - Planning & Execution Roadmap

> **Date**: 2026-02-18
> **Status**: Active
> **Branch**: `feature/v2-planning`
> **Purpose**: Define the phases and tasks for completing v2 planning, documentation reorganization, and implementation preparation.

---

## Context

v1 is complete (1,094 tests, 7 releases, cloud deployment). The v2 vision has been captured in [PRD.2.draft.md](./PRD.2.draft.md). Before implementation begins, we need to:

1. Set up the v2 worktree properly
2. Reorganize documentation for scalable expansion
3. Expand specs with v2 requirements
4. Create an implementation plan with checkpoints

---

## Phase 0: Workspace Setup

> **Goal**: Configure this worktree so all agents and sessions understand the v2 context.

| # | Task | Status | Notes |
|---|------|--------|-------|
| 0.1 | Create PRD.2.draft.md with v2 vision | DONE | Captures all 4 modules + integration points |
| 0.2 | Create this PLANNING.v2.md document | DONE | You're reading it |
| 0.3 | Create CLAUDE.local.md for v2 worktree | TODO | Customized from main repo, v2-aware |
| 0.4 | Create .claude/CLAUDE.md (project-level) for v2 | TODO | So Claude Code agents get v2 context automatically |

---

## Phase 1: Documentation Decomposition

> **Goal**: Break god documents into hierarchical, composable, indexed documentation that scales for v2 without becoming unmaintainable.

### 1.1 The Problem

Current documentation is flat files in `docs/`:

```
docs/
├── PRD.md              (258 lines — manageable today, will explode with 4 new modules)
├── PRD.draft.md        (original draft)
├── DOMAIN.md           (988 lines — already a god document)
├── SCENARIOS.md         (568 lines — will double with new modules)
├── ROADMAP.md          (221 lines — will grow significantly)
├── TRADEOFFS.md        (176 lines — will grow)
├── RESEARCH.md
├── JOURNAL.md
├── DEPLOYMENT.md
├── DEVELOPMENT.md
└── RELEASING.md
```

Plus root-level:
```
GUIDELINES.md           (very long — architecture, testing, naming, quality)
TASKS.md                (567 lines — will grow with v2 tasks)
```

### 1.2 The Solution: Hierarchical Decomposition with INDEX.md

**Principle**: ONE SINGLE SOURCE OF TRUTH through hierarchy, file composition, and indexing.

- Each major topic becomes a **folder**
- Each folder has an **INDEX.md** with a summarized view + links to subtopics
- Subtopics are further decomposed when they exceed ~300 lines
- No duplicate content — reference, don't copy
- v1 and v2 content coexist in the same structure (no versioned documents)

### 1.3 Target Structure

```
docs/
├── INDEX.md                          # Top-level docs index
│
├── product/                          # Product requirements
│   ├── INDEX.md                      # PRD summary + links to modules
│   ├── vision.md                     # Overall vision, success criteria
│   ├── personas.md                   # User personas (Sarah, Marcus, Diana, Bruno)
│   ├── modules/
│   │   ├── INDEX.md                  # Module overview map
│   │   ├── tasks.md                  # Task management module (v1 core)
│   │   ├── projects.md              # Project management module (v2)
│   │   ├── comms.md                 # Communication management (v2)
│   │   ├── people.md               # People & companies (v2)
│   │   └── agents.md               # Agent sessions (v2)
│   ├── nfr.md                       # Non-functional requirements (all versions)
│   └── analytics.md                 # Analytics events and measurement
│
├── domain/                           # DDD domain design
│   ├── INDEX.md                      # Context map + bounded context overview
│   ├── shared-kernel.md             # Shared types across contexts
│   ├── contexts/
│   │   ├── INDEX.md                 # Context relationship map
│   │   ├── identity.md              # Identity context (v1)
│   │   ├── tasks.md                 # Task context (v1)
│   │   ├── boards.md               # Board context (v1)
│   │   ├── administration.md        # Administration context (v1)
│   │   ├── onboarding.md           # Onboarding context (v1)
│   │   ├── analytics.md            # Analytics context (v1)
│   │   ├── notifications.md        # Notification context (v1)
│   │   ├── projects.md             # Project context (v2)
│   │   ├── comms.md                # Communication context (v2)
│   │   ├── people.md               # People & companies context (v2)
│   │   └── agents.md               # Agent sessions context (v2)
│   └── api-design.md               # API endpoint design (all contexts)
│
├── scenarios/                        # User storyboards
│   ├── INDEX.md                      # Scenario catalog + coverage matrix
│   ├── onboarding.md               # Registration, first-use (v1)
│   ├── task-management.md           # Daily task workflows (v1)
│   ├── admin.md                     # Admin & compliance (v1)
│   ├── mobile-offline.md           # Mobile, offline, PWA (v1)
│   ├── project-management.md       # Project workflows (v2)
│   ├── communications.md           # Communication workflows (v2)
│   ├── people-management.md        # People & company workflows (v2)
│   └── agent-workflows.md          # Agent orchestration (v2)
│
├── architecture/                     # Technical architecture
│   ├── INDEX.md                      # Architecture overview
│   ├── backend.md                   # DDD layers, patterns, conventions
│   ├── frontend.md                  # Architecture tiers, component taxonomy
│   ├── testing.md                   # Testing pyramid, conventions, coverage
│   ├── security.md                  # Auth, encryption, HIPAA patterns
│   ├── infrastructure.md            # Azure, Terraform, CI/CD, Docker
│   └── decisions/                   # Architecture Decision Records
│       ├── INDEX.md                 # ADR log
│       └── ...                      # Individual ADRs (migrate from TRADEOFFS.md)
│
├── roadmap/                          # Future planning
│   ├── INDEX.md                      # Roadmap summary
│   ├── v1-tiers.md                  # Original v1 capability tiers (from ROADMAP.md)
│   └── v2-checkpoints.md           # v2 implementation checkpoints
│
├── operations/                       # Operational docs
│   ├── INDEX.md
│   ├── deployment.md               # From DEPLOYMENT.md
│   ├── development.md              # From DEVELOPMENT.md
│   ├── releasing.md                # From RELEASING.md
│   └── research.md                 # From RESEARCH.md
│
└── journal/                          # Development journal
    ├── INDEX.md                      # Journal timeline
    ├── v1.md                        # v1 journal entries (from JOURNAL.md)
    └── v2.md                        # v2 journal entries (new)
```

### 1.4 Root-Level Changes

```
GUIDELINES.md → Slim down to a brief pointer to docs/architecture/
TASKS.md      → Remains at root (active tracker) but v2 tasks in separate section
CHANGELOG.md  → Remains at root (standard location)
README.md     → Remains at root (entry point)
```

### 1.5 INDEX.md Convention

Every INDEX.md follows this template:

```markdown
# [Topic Name]

> Brief description of what this section covers.

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [subtopic-a.md](./subtopic-a.md) | What it covers | Active/Draft |
| [subtopic-b.md](./subtopic-b.md) | What it covers | Active/Draft |

## Quick Summary

[2-5 paragraph summary of the key points across all subtopics,
 so a reader can understand the section without reading every file]
```

### 1.6 Decomposition Tasks

| # | Task | Status | Notes |
|---|------|--------|-------|
| 1.1 | Create `docs/INDEX.md` top-level index | DONE | |
| 1.2 | Decompose `PRD.md` + `PRD.draft.md` + `PRD.2.draft.md` → `docs/product/` | DONE | 14 files created; PRD.2.draft.md retained at top level as live draft |
| 1.3 | Decompose `DOMAIN.md` → `docs/domain/` | DONE | 15 files; one file per bounded context |
| 1.4 | Decompose `SCENARIOS.md` → `docs/scenarios/` | DONE | 11 files; grouped by domain area |
| 1.5 | Decompose `ROADMAP.md` → `docs/roadmap/` | DONE | 3 files; v1 tiers + v2 checkpoints |
| 1.6 | Decompose `TRADEOFFS.md` → `docs/architecture/decisions/` | DONE | Converted to ADR format |
| 1.7 | Move operational docs → `docs/operations/` | DONE | 5 files; DEPLOYMENT, DEVELOPMENT, RELEASING, RESEARCH |
| 1.8 | Move journal → `docs/journal/` | DONE | 3 files; v1 entries separated from v2 |
| 1.9 | Extract architecture content from `GUIDELINES.md` → `docs/architecture/` | DONE | 8 files; GUIDELINES.md slimmed to pointer |
| 1.10 | Create all INDEX.md files | DONE | Every folder has INDEX.md |
| 1.11 | Update all cross-references and links | DONE | 48 links verified OK, 2 stale links fixed |
| 1.12 | Update CLAUDE.local.md document references | DONE | Points to new locations |

> **Phase 1 complete** (2026-02-18). All 11 original flat docs deleted (PRD.md, DOMAIN.md, SCENARIOS.md, ROADMAP.md, TRADEOFFS.md, DEPLOYMENT.md, DEVELOPMENT.md, RELEASING.md, RESEARCH.md, JOURNAL.md, PRD.draft.md). Total files created across 7 folders: 59 (including all INDEX.md files). DECOMPOSITION-GUIDE.md retained at `docs/` as meta-reference.

---

## Phase 2: v2 Requirements Expansion

> **Goal**: Expand scenarios, domain design, and product requirements with v2 modules, fully integrated with existing v1 content.

| # | Task | Status | Notes |
|---|------|--------|-------|
| 2.1 | Add Bruno persona to `docs/product/personas.md` | DONE | Created docs/product/personas/bruno.md + decomposed all personas into per-file structure |
| 2.2 | Write project management scenarios → `docs/scenarios/project-management.md` | DONE | 4 scenarios (S-PM-01 to S-PM-04), 29 analytics events |
| 2.3 | Write communication management scenarios → `docs/scenarios/communications.md` | DONE | 4 scenarios (S-CM-01 to S-CM-04), 22 analytics events |
| 2.4 | Write people management scenarios → `docs/scenarios/people-management.md` | DONE | 4 scenarios (S-PP-01 to S-PP-04), 30 analytics events |
| 2.5 | Write agent workflow scenarios → `docs/scenarios/agent-workflows.md` | DONE | 4 scenarios (S-AG-01 to S-AG-04), 48 analytics events |
| 2.6 | Design Project bounded context → `docs/domain/contexts/projects.md` | DONE | 4 aggregates, 23 events, 27 use cases, 30 endpoints |
| 2.7 | Design Communication bounded context → `docs/domain/contexts/comms.md` | DONE | 2 aggregates, 19 events, 20 use cases, 17 endpoints |
| 2.8 | Design People & Companies bounded context → `docs/domain/contexts/people.md` | DONE | 2 aggregates, 25 events, 37 use cases, 39 endpoints |
| 2.9 | Design Agent Sessions bounded context → `docs/domain/contexts/agents.md` | DONE | 5 aggregates (AgentSession, WorkQueue, AgentTemplate, SessionMessageQueue, SessionChain), 40+ events, 30+ use cases, 40+ endpoints. Includes event-sourced sidecar architecture, bidirectional comms, session chains, and metrics modeling |
| 2.10 | Update context map with v2 relationships → `docs/domain/INDEX.md` | DONE | 11 bounded contexts, 13 v2 relationships mapped |
| 2.11 | Expand NFR for v2 → `docs/product/nfr.md` | DONE | 88 v2 requirements across 12 categories (agent performance, real-time streaming, budget accuracy, skills, comms adapters, security, bridges, data/storage, reliability, scalability, local-first, v1 non-regression) |
| 2.12 | Research v2 technology choices → `docs/operations/research.md` | DONE | 8 technologies researched: Gmail API, WhatsApp, Claude Agent SDK, ngrok, Discord, Slack, Redis Streams + decomposed into per-tech files in docs/operations/research/ |
| 2.13 | Finalize PRD.2 (promote from draft) → `docs/product/` module files | DONE | PRD.2.draft.md promoted to Active status. Agent FRs expanded to AG-050, bridge contexts added, NFR references updated, technology decisions finalized, all 6 open questions resolved |

> **Phase 2 complete** (2026-02-18). All 13 tasks done + major expansions: 10 agent workflow scenarios (S-AG-01 to S-AG-10) covering 50 FRs, 3 bridge contexts (ProjectAgentBridge, AgentTaskBridge, ProjectTaskBridge), Skills system with MemoryPill consolidation, structured activity stream, special tool call handling (AskUserQuestion, TodoWrite, PlanMode), auto-continue mode, session chains with voluntary handoff, and model selection per session. PRD promoted from Draft to Active.

---

## Phase 3: Implementation Planning

> **Goal**: Create prioritized, checkpoint-based implementation plan for v2.

| # | Task | Status | Notes |
|---|------|--------|-------|
| 3.1 | Prioritize v2 modules (which to build first) | TODO | Dependency analysis |
| 3.2 | Define v2 checkpoints (like v1's CP1-CP5) | TODO | Each checkpoint = runnable increment |
| 3.3 | Break checkpoints into tasks | TODO | Granular enough for agent assignment |
| 3.4 | Estimate effort and dependencies | TODO | Identify critical path |
| 3.5 | Create v2 TASKS section in TASKS.md | TODO | Or migrate to new structure |
| 3.6 | Identify technology spikes needed | TODO | Agent SDK, WhatsApp API, etc. |
| 3.7 | Define v2 verification gates | TODO | Adapted from v1's `./dev verify` |

---

## Phase Summary

| Phase | Focus | Depends On |
|-------|-------|------------|
| **Phase 0** | Workspace setup | Nothing |
| **Phase 1** | Documentation decomposition | Phase 0 |
| **Phase 2** | v2 requirements expansion | Phase 1 (needs new structure) |
| **Phase 3** | Implementation planning | Phase 2 (needs requirements) |
| **Implementation** | Build v2 modules | Phase 3 (needs plan) |

---

## Guiding Principles

1. **Single source of truth** — Never duplicate content. Reference, compose, index.
2. **Hierarchy over versioning** — No `PRD-v1.md`, `PRD-v2.md`. One structure that grows.
3. **INDEX.md everywhere** — Every folder tells you what's inside and where to look.
4. **Non-breaking evolution** — v1 features, tests, and docs remain valid throughout v2.
5. **Checkpoint delivery** — Every checkpoint produces something usable.
6. **Agent-friendly documentation** — Structure enables agents to find relevant context quickly.
