# Journal

> Chronological development journal capturing decisions, lessons learned, and release history.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [v1.md](./v1.md) | Complete v1 development journal: from inception through v1.0.7, covering all 5 checkpoints, domain redesigns, infrastructure work, and post-release improvements | Active |
| [v2.md](./v2.md) | v2 development journal — to be populated during v2 work (CP6 onward) | Draft (v2) |

---

## Summary

The v1 journal covers the entire LemonDo v1 development from February 13 to February 17, 2026 — five days from conception to a stable production release.

**Planning Phase (Feb 13)** documents the reasoning chain behind the technical and product decisions made before writing a single line of code: why healthcare compliance drove HIPAA-aware patterns, why Azure was chosen, why SQLite is used for development alongside SQL Server for production, and how the checkpoint-based delivery model emerged from a hard review of the original "build everything at once" plan.

**Checkpoint 1 (Feb 14)** covers the full-stack DDD implementation: domain model with two bounded contexts (Tasks, Boards), application layer with 14 handlers, infrastructure with EF Core + SQLite, 18 API endpoints, and a complete React frontend. This phase also documents two significant redesigns — the Column-Status Invariant (eliminating dual sources of truth) and the Task/Board Bounded Context Split (removing spatial concerns from the Task entity).

**Checkpoint 2 (Feb 15)** documents the authentication system: ASP.NET Core Identity, JWT + HttpOnly cookie split-token architecture, memory-only Zustand auth store, and the E2E test stabilization that took flaky tests from "1-2 random failures per run" to 3/3 consecutive green runs by switching to unique users per describe block.

**Checkpoint 3 (Feb 15)** covers the Rich UX layer: @dnd-kit drag-and-drop, Task Detail Sheet, Filters & Search (dual client/server approach), Dark/Light theme, responsive design, loading skeletons, empty states, toast notifications, and route error boundaries.

**Checkpoint 4 (Feb 16)** documents Production Hardening: Serilog with PII masking, Administration bounded context with audit trail, admin panel, AES-256-GCM field encryption, the PII→ProtectedData rename, Task SensitiveNote feature, dual EF Core migration assemblies, the developer CLI, Terraform Azure infrastructure (App Service → Container Apps migration), and the full CI/CD pipeline.

**Checkpoint 5 (Feb 16)** covers Advanced & Delight: mobile responsiveness overhaul, Lemon.DO branding, PWA with offline support (read + mutation queue), onboarding tooltip tour, notification system (in-app + Web Push), analytics, Spanish i18n, password strength meter, and multi-browser E2E with visual regression.

**Post-Release Work (Feb 17)** documents improvements after v1.0.0: offline queue startup drain bug fix, admin E2E coverage (20 new tests), React StrictMode refresh token race condition fix, OpenAPI-based TypeScript type generation pipeline, and theme/visual polish.

The v2 journal will begin when v2 development starts and follow the same format: one entry per significant decision or phase, with explicit "Lessons Learned" sections for every non-trivial issue encountered.
