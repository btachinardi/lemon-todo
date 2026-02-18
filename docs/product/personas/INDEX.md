# Personas

> All user personas for LemonDo v1 and v2, each defined by role, pain points, goals, and job to be done.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [bruno.md](./bruno.md) | Persona A — Solo developer power user; primary persona for all v2 modules | Active |
| [sarah.md](./sarah.md) | Persona B — Overwhelmed freelancer; primary driver of frictionless task capture | Active |
| [marcus.md](./marcus.md) | Persona C — Engineering team lead; drives HIPAA compliance and kanban requirements | Active |
| [diana.md](./diana.md) | Persona D — System administrator; drives audit, visibility, and platform control requirements | Active |

---

## Module Coverage

| Persona | tasks | projects | comms | people | agents |
|---------|-------|----------|-------|--------|--------|
| Bruno | yes | yes | yes | yes | yes |
| Sarah | yes | — | — | — | — |
| Marcus | yes | — | — | — | — |
| Diana | yes | — | — | — | — |

---

## Summary

LemonDo v1 was shaped by three personas. Sarah, the overwhelmed freelancer, established the requirement for instant, frictionless task capture — she needs to dump a thought before it vanishes and see a clear picture of today's work without wrestling with a heavyweight tool. Marcus, the engineering team lead at a healthcare startup, drove the HIPAA compliance track: audit trails, access controls, and kanban boards that satisfy both sprint planning and a compliance officer's scrutiny. Diana, the system administrator, defined the platform control surface: who accessed what, when, and the ability to produce a full audit report on demand without navigating a maze of screens.

v2 introduces Bruno as the primary persona. Bruno is a solo full-stack developer managing multiple active projects simultaneously, using git worktrees, Claude Code agents, ngrok, Docker, and CI/CD pipelines as everyday tools. His core frustration is context fragmentation: repos live in one place, communications in six others, relationship history nowhere, and AI agent sessions are tracked only in his memory. Every v2 module is designed to eliminate one layer of that fragmentation — Projects unifies repo and deployment context, Comms collapses all inbound channels into one inbox, People keeps relationship history anchored to people rather than scattered across threads, and Agents gives him a dispatch board and budget tracker for Claude Code work.

The v1 personas remain fully relevant for the `tasks` module. The v2 build is a non-breaking evolution: Sarah, Marcus, and Diana continue to get the same task management experience they drove in v1.
