# Planning Playbook

> **Source**: Original — codifies the v2 planning process executed in `feature/v2-planning`
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Overview

This playbook documents the repeatable process for planning a new version, sprint, or major feature set in LemonDo. It codifies exactly what was executed during v2 planning and serves as the template for any future planning cycle.

The process runs in four sequential phases, each depending on the previous. Within phases, many tasks parallelise across specialist subagents. All work is delegated through `/orchestrate` — the orchestrator never executes directly.

---

## Specialist Agent Reference

Every activity in this playbook maps to a specialist subagent. Orchestrators delegate; they do not implement.

| Activity | Specialist |
|----------|-----------|
| Decompose documents into hierarchical folders | `docs-decomposition-specialist` |
| Write user scenarios and storyboards | `scenario-writer` |
| Design bounded contexts (entities, VOs, events) | `domain-design-specialist` |
| Research technology choices and compatibility | `tech-research-specialist` |
| Create atomic, conventional commits | `git-commit-specialist` |
| Create new specialist agents | `subagent-factory` |
| Improve existing specialist agents based on feedback | `subagent-improver` |

If a required specialist does not yet exist, delegate its creation to `subagent-factory` before beginning the phase that needs it.

---

## Phase 0: Vision Capture

> **Goal**: Establish the planning scope so every subsequent agent and session understands what is being planned.
> **Depends on**: Nothing — this is always the starting point.
> **Parallelisable**: No — these outputs gate everything else.

### Checklist

- [ ] **0.1** Write a PRD draft capturing:
  - The version's theme and target user
  - Each new module (name, one-line purpose, key capabilities)
  - Functional requirements per module
  - Key user scenarios (summary level — full scenarios come in Phase 2)
  - Integration points with existing modules and external systems
  - Non-functional requirement deltas (what changes from the previous version)
- [ ] **0.2** Create a planning roadmap document (like `docs/PLANNING.md`) with:
  - Phased task table (# | Task | Status | Notes)
  - Phase dependencies
  - Guiding principles for this planning cycle
- [ ] **0.3** Update `CLAUDE.local.md` with:
  - New scope context (new modules, current phase)
  - Updated key documents table
  - Any new v2-specific conventions or gotchas
- [ ] **0.4** Commit with `git-commit-specialist`: `docs(planning): capture v2 vision and planning scaffold`

### Deliverables

| Artifact | Location |
|----------|----------|
| PRD draft | `docs/PRD.[version].draft.md` |
| Planning roadmap | `docs/PLANNING.[version].md` |
| Updated agent instructions | `CLAUDE.local.md` |

### Notes

- The PRD draft is intentionally incomplete at this stage. It captures intent, not specifications. Specifications emerge in Phase 2.
- Keep the PRD draft at the top level of `docs/` until it is promoted to the product folder hierarchy in Phase 2.
- The planning roadmap is a living document — update task statuses throughout all phases.

---

## Phase 1: Documentation Structure

> **Goal**: Ensure the documentation structure can scale to hold v2 content without becoming unmaintainable.
> **Depends on**: Phase 0 (need to know the scope before designing the structure).
> **Parallelisable**: Decomposition tasks run in parallel (one `docs-decomposition-specialist` per document area).

### When to Run This Phase

Run Phase 1 when any of the following is true:

- Any existing doc file exceeds 300 lines
- A new module is being added that does not have a corresponding folder
- Flat files exist that should be hierarchical (no `INDEX.md`, no subfolder)

If the structure is already clean from a previous planning cycle, this phase may be abbreviated to "verify structure, add missing folders."

### Checklist

- [ ] **1.1** Audit the current `docs/` structure:
  - List all files and their line counts
  - Identify god documents (>300 lines or covering multiple topics)
  - Identify missing INDEX.md files
  - Identify folders needed for new modules but not yet created
- [ ] **1.2** Design the target structure:
  - Map each section of each god document to a target file
  - Ensure every new module has a folder
  - Ensure every folder has an INDEX.md in the plan
- [ ] **1.3** Decompose each god document in parallel:
  - Spawn one `docs-decomposition-specialist` per document area
  - Each agent receives: source file path, target folder, decomposition map, status labels
  - Agents run concurrently — they write to different folders, no conflicts
- [ ] **1.4** Verify the decomposed structure:
  - Every folder has an INDEX.md
  - All INDEX.md links resolve to real files
  - No stale cross-references remain
- [ ] **1.5** Delete original flat files (only after verification confirms all content moved)
- [ ] **1.6** Commit with `git-commit-specialist`: `docs(structure): decompose flat docs into hierarchical folder structure`

### Parallelisation Map

```
Phase 1 work (all run in parallel once the target structure is designed):

  docs-decomposition-specialist (PRD area)
  docs-decomposition-specialist (domain area)
  docs-decomposition-specialist (scenarios area)
  docs-decomposition-specialist (architecture area)
  docs-decomposition-specialist (operations area)
  docs-decomposition-specialist (roadmap area)
  docs-decomposition-specialist (journal area)
```

### Deliverables

| Artifact | Description |
|----------|-------------|
| Hierarchical `docs/` folder | Every topic in its own folder |
| INDEX.md at every level | Summary + contents table + 2-5 paragraph quick summary |
| Zero god documents | No file exceeds 300 lines |
| Verified cross-references | All internal links resolve |

### INDEX.md Convention

Every INDEX.md follows this template exactly:

```markdown
# [Section Title]

> [One-sentence description of what this section covers.]

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [file-a.md](./file-a.md) | Brief description | Active |

---

## Summary

[2-5 paragraphs summarising the key points across all documents in this folder.]
```

### Notes

- Decomposition is **move, not copy**. Content exists in exactly one place.
- The `docs-decomposition-specialist` handles link fixing automatically — trust it.
- After deletion, glob the source directory to confirm no orphaned originals remain.
- Evaluate `docs-decomposition-specialist` performance after this phase. Delegate improvements to `subagent-improver` if needed.

---

## Phase 2: Requirements Expansion

> **Goal**: Fully specify every new module with scenarios, domain design, technology choices, and updated NFRs — all integrated with existing v1 content.
> **Depends on**: Phase 1 (needs the folder structure to land content correctly).
> **Parallelisable**: Heavily. Three waves, each wave parallelised internally.

### Wave Structure

```
Wave 2a (no dependencies — all run in parallel):
  scenario-writer          → Bruno persona
  scenario-writer          → Module A scenarios
  scenario-writer          → Module B scenarios
  scenario-writer          → Module C scenarios
  scenario-writer          → Module D scenarios
  tech-research-specialist → Technology A
  tech-research-specialist → Technology B
  tech-research-specialist → Technology C

Wave 2b (depends on Wave 2a scenarios):
  domain-design-specialist → Bounded context for Module A
  domain-design-specialist → Bounded context for Module B
  domain-design-specialist → Bounded context for Module C
  domain-design-specialist → Bounded context for Module D
  [Manual] Expand NFRs for new scope

Wave 2c (depends on Wave 2b domain design):
  [Manual] Update context map with new bounded context relationships
  [Manual] Finalize PRD (promote from draft to product folder hierarchy)
```

### Wave 2a Checklist (Parallelisable)

- [ ] **2.1** Add/update personas:
  - Spawn `scenario-writer` to add new primary personas to `docs/product/personas.md`
  - Provide: persona name, role, goals, pain points, technical comfort level
- [ ] **2.2** Write scenarios for each new module:
  - Spawn one `scenario-writer` per module
  - Target file: `docs/scenarios/[module-name].md`
  - Each scenario file should cover: happy path, edge cases, error conditions, offline/mobile behaviour
- [ ] **2.3** Research technology choices:
  - Spawn one `tech-research-specialist` per technology area
  - Target file: `docs/operations/research.md` (append sections) or dedicated research files
  - Each research task covers: API maturity, auth model, rate limits, SDK availability, compatibility with existing stack, risks
- [ ] **2a commit**: `docs(requirements): add personas, scenarios, and tech research for v2 modules`

### Wave 2b Checklist (Depends on 2a)

- [ ] **2.4** Design bounded contexts:
  - Spawn one `domain-design-specialist` per new module
  - Target file: `docs/domain/contexts/[module].md`
  - Each context design covers: aggregate roots, entities, value objects, domain events, commands, queries, repository interfaces, integration points with other contexts
- [ ] **2.5** Expand NFRs:
  - Update `docs/product/nfr.md` with requirements driven by new modules
  - Consider: data volume, external API reliability, privacy/encryption, real-time latency
- [ ] **2b commit**: `docs(domain): add bounded context designs and NFR expansions for v2`

### Wave 2c Checklist (Depends on 2b)

- [ ] **2.6** Update context map:
  - Update `docs/domain/INDEX.md` with new bounded context relationships
  - Add new contexts to the relationship diagram
  - Document integration patterns (synchronous, event-driven, anti-corruption layers)
- [ ] **2.7** Finalize PRD:
  - Move content from the draft file into the `docs/product/` hierarchy (module files, vision, etc.)
  - The draft file can remain as a pointer or be deleted
  - Update the product INDEX.md to reflect the new structure
- [ ] **2c commit**: `docs(product): finalize PRD and context map for v2`

### Deliverables

| Artifact | Location | Owner |
|----------|----------|-------|
| New/updated personas | `docs/product/personas.md` | `scenario-writer` |
| Scenario files per module | `docs/scenarios/[module].md` | `scenario-writer` |
| Technology research | `docs/operations/research.md` | `tech-research-specialist` |
| Bounded context designs | `docs/domain/contexts/[module].md` | `domain-design-specialist` |
| Updated NFRs | `docs/product/nfr.md` | Manual |
| Updated context map | `docs/domain/INDEX.md` | Manual |
| Finalized PRD | `docs/product/` | Manual |

### Notes

- The wave dependency is strict: domain design requires scenarios to exist as input. Do not start Wave 2b until Wave 2a is committed.
- `tech-research-specialist` outputs can be read by `domain-design-specialist` in Wave 2b — explicitly include research file paths in the domain design agent's context.
- After each wave, evaluate subagent outputs. Delegate improvements to `subagent-improver` before the next wave if quality issues are found.

---

## Phase 3: Implementation Planning

> **Goal**: Produce a prioritised, checkpoint-based implementation plan with enough granularity for immediate agent assignment.
> **Depends on**: Phase 2 (needs complete requirements and domain design before planning implementation).
> **Parallelisable**: Partially — dependency analysis and checkpoint definition are sequential; task breakdown can be parallelised per checkpoint.

### Checklist

- [ ] **3.1** Prioritise modules:
  - Perform dependency analysis: which modules does each new module depend on?
  - Which module delivers the highest user value soonest?
  - Which module has the fewest external dependencies (APIs, SDKs)?
  - Produce a build order: Module A → Module B → Module C → Module D
- [ ] **3.2** Define checkpoints:
  - Each checkpoint = a runnable increment (something a user can actually use)
  - Follow v1's model: CP1 (foundation), CP2 (core feature), CP3 (integration), CP4 (polish), CP5 (release)
  - Every checkpoint has: clear goal, user-visible deliverable, verification gate
- [ ] **3.3** Break checkpoints into tasks:
  - Granular enough for agent assignment: "implement CreateProject command handler" not "build projects module"
  - Each task has: description, layer (domain / application / infrastructure / presentation / frontend), estimated complexity (S/M/L), dependencies
- [ ] **3.4** Identify technology spikes:
  - List any technology that requires a proof-of-concept before full implementation
  - Each spike has: goal, timebox, success criteria, output (yes/no proceed + findings doc)
  - Add spikes as the first tasks in the relevant checkpoint
- [ ] **3.5** Define verification gates:
  - Adapt v1's `./dev verify` for v2's scope
  - Each checkpoint gate: unit tests pass, integration tests pass, E2E scenarios pass, performance budget met
- [ ] **3.6** Write checkpoint plan:
  - Target file: `docs/roadmap/checkpoints.md`
  - Format: one section per checkpoint with goal, tasks table, verification gate
- [ ] **3.7** Commit with `git-commit-specialist`: `docs(roadmap): add v2 checkpoint plan and task breakdown`

### Checkpoint Template

Each checkpoint section in `docs/roadmap/checkpoints.md` follows this structure:

```markdown
## Checkpoint N: [Name]

> **Goal**: [One sentence — what a user can do after this checkpoint that they could not before.]
> **Modules**: [Which modules are touched]
> **Verification gate**: [What must pass before this checkpoint is considered done]

### Technology Spikes

| Spike | Goal | Timebox | Success Criteria |
|-------|------|---------|-----------------|
| ... | ... | ... | ... |

### Tasks

| # | Task | Layer | Size | Depends On |
|---|------|-------|------|-----------|
| N.1 | ... | Domain | S | — |
| N.2 | ... | Application | M | N.1 |
```

### Deliverables

| Artifact | Location |
|----------|----------|
| Module build order | `docs/PLANNING.[version].md` Phase 3 section |
| Checkpoint plan | `docs/roadmap/checkpoints.md` |
| Spike list | `docs/roadmap/checkpoints.md` (per checkpoint) |
| Verification gates | `docs/roadmap/checkpoints.md` (per checkpoint) |

---

## Cross-Cutting Concerns

These rules apply throughout all phases, not just within individual checklists.

### Commit Discipline

- After every wave or major milestone, delegate to `git-commit-specialist`
- Never accumulate more than one phase of changes in a single commit
- Follow Conventional Commits: `<type>(<scope>): <description>`
- Use scopes that match the work: `docs`, `planning`, `domain`, `product`, `roadmap`

### Agent Quality Loop

After every delegation round:

1. Read the output files produced by the subagent
2. Evaluate: Is the content complete? Accurate? In the right format? At the right depth?
3. If quality issues are found, delegate improvements to `subagent-improver` before proceeding
4. Do not start the next wave until the current wave's outputs are confirmed good

### Documentation Size Rule

Any file that grows beyond 300 lines during planning must be decomposed before the next phase begins. Spawn a `docs-decomposition-specialist` immediately when this threshold is crossed.

### Orchestrator Rule

The orchestrator never:
- Writes files directly
- Executes git commands directly
- Researches technologies directly
- Designs domains directly

The orchestrator always:
- Reads outputs to evaluate quality
- Delegates all execution to specialists
- Synthesises results across specialists for cross-cutting decisions (e.g., which module to build first)
- Updates the planning roadmap (`docs/PLANNING.[version].md`) task statuses after each delegation

### Session Continuity

At the end of every session:

- Update `docs/PLANNING.[version].md` with current task statuses
- Add an entry to `docs/journal/v2.md` with: date, what was done, key decisions made, what comes next
- Update `CLAUDE.local.md` "Current Phase" to reflect where the next session should begin

---

## Phase Dependency Diagram

```
Phase 0: Vision Capture
  └── Phase 1: Documentation Structure
        └── Phase 2: Requirements Expansion
              ├── Wave 2a (parallel): Personas + Scenarios + Tech Research
              │     └── Wave 2b (parallel): Domain Design + NFR Expansion
              │           └── Wave 2c (sequential): Context Map + PRD Finalization
              └── Phase 3: Implementation Planning
                    ├── Module Priority + Checkpoint Definition (sequential)
                    └── Task Breakdown per Checkpoint (parallel)
                          └── Implementation begins
```

---

## Guiding Principles

1. **Single source of truth** — Never duplicate content. Reference, compose, index.
2. **Hierarchy over versioning** — No `PRD-v1.md`, `PRD-v2.md`. One structure that grows.
3. **INDEX.md everywhere** — Every folder tells you what is inside and where to look.
4. **Non-breaking evolution** — Existing features, tests, and docs remain valid throughout planning.
5. **Checkpoint delivery** — Every checkpoint produces something runnable, not just a document.
6. **Agent-friendly documentation** — Structure enables agents to find relevant context quickly without reading everything.
7. **Always delegate** — Orchestrator activates specialists; it does not implement.
8. **Commit incrementally** — Atomic commits after each wave. Never accumulate a full phase's worth of uncommitted changes.
