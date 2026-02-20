# ADR-001: Planning & Delivery

> **Source**: Extracted from docs/architecture/decisions/trade-offs.md §Assignment Context, §Assumptions, §Planning & Delivery
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Assignment Context

This project is a **lemon.io take-home assignment** for a Full Stack Developer position at a healthcare startup that detects early-stage cancers across multiple organs through a single MRI scan. The assignment asked for a todo/task management app with .NET/SQLite backend, React frontend, and "any features you would add for a real production MVP."

The prompt "add any features for a real production MVP" was interpreted through the lens of the target company's domain: regulated healthcare. This means compliance awareness (HIPAA patterns, audit trails, field encryption), offline reliability (hospital networks are unstable), Azure deployment (matching the company's infrastructure), and product analytics (an MVP exists to validate hypotheses). Every trade-off in this document flows from that interpretation.

The branding is also deliberate — named after lemon.io to demonstrate the ability to adapt frontend design to any brand guidelines.

---

## Assumptions

- **Single developer, time-boxed**: The project is designed for incremental delivery with meaningful checkpoints, not waterfall completion.
- **SQLite is sufficient for MVP**: The data model is simple (tasks, boards, users). The repository pattern makes swapping to PostgreSQL a one-file change when scaling requires it.
- **Evaluators have .NET 10 SDK + Node.js 23+**: The project targets the latest LTS runtime. Aspire handles service orchestration so `dotnet run` starts everything.
- **Browser-first, not native**: LemonDo uses PWA over native apps. Service workers provide offline support without app store distribution.

---

## Planning & Delivery Trade-offs

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Delivery strategy** | 5 incremental checkpoints, each a runnable app | Build everything at once | If delivery stops at any checkpoint, there is something presentable; proves extensibility without over-building |
| **Auth timing** | Tasks first (CP1), auth second (CP2) | Auth-gated MVP from day one | Demonstrates architecture faster; adding user-scoping is a one-line repository change |
| **HIPAA** | Technical controls ("HIPAA-Ready") | Full certification | Certification requires legal/BAA framework beyond code scope |
| **Bounded contexts** | All 6 designed, 2-4 implemented per checkpoint | Implement all at once | Incremental delivery proves extensibility without over-building |
| **Quick-add as P0** | Title-only task creation (one tap) | Requiring title + description | Scenario analysis showed users create tasks in 2-second bursts; minimal friction is the killer feature |
