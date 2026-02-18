# Product

> Product requirements, vision, personas, non-functional requirements, analytics, and module specifications for LemonDo v1 and v2.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [vision.md](./vision.md) | Vision statement, value proposition, success criteria, out of scope, risks | Active |
| [personas/](./personas/INDEX.md) | All user personas: Sarah, Marcus, Diana (v1) and Bruno (v2) | Active |
| [nfr.md](./nfr.md) | Non-functional requirements: performance, PWA, security, i18n, UX polish | Active |
| [analytics.md](./analytics.md) | North star metric, analytics events, measurement architecture | Active |
| [modules/](./modules/) | Functional requirements organized by product module | — |

---

## Summary

LemonDo v1 is a HIPAA-compliant task management platform combining consumer-grade UX with enterprise-grade compliance. It targets three personas: Sarah (the overwhelmed freelancer who needs frictionless task capture), Marcus (the team lead who needs compliant kanban boards), and Diana (the system administrator who needs full audit visibility).

The product vision positions LemonDo at the intersection of simplicity and compliance — as simple as Todoist, as visual as Trello, as compliant as enterprise tools. The north star metric is **Weekly Active Task Completers (WATC)**: users who complete at least one task per week.

The v1 non-functional requirements cover ten areas: performance (p95 < 200ms), responsive design (mobile-first), PWA (full offline CRUD), API documentation, observability, CI/CD, UI/UX, internationalization (EN/PT/ES), containerization, and security (OWASP Top 10). NFR-011 adds micro-interactions as a first-class requirement after scenario analysis revealed that UX polish is core to the product, not decoration.

v2 evolves LemonDo from a task management app into Bruno's personal development command center, adding four new modules: Projects (git repository management), Comms (unified communication inbox), People (lightweight CRM), and Agents (Claude Code session orchestration). v2 is single-user, local-first, and designed for a solo developer power user. All v1 features and tests continue to pass as a non-breaking evolution.

Architecture decisions from the PRD (task creation strategy, analytics architecture, offline strategy, protected data handling) are documented in [docs/architecture/](../architecture/). See the relevant files there rather than duplicating here.
