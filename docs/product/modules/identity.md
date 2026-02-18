# Identity Module

> **Source**: Extracted from docs/PRD.draft.md §2 FR-001, FR-002, FR-008, docs/PRD.md §1 FR-001, FR-002, FR-008
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## FR-001: User Authentication

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-001.1 | Email/password registration with email verification | P0 |
| FR-001.2 | Email/password login with JWT token management | P0 |
| FR-001.3 | Social OAuth login (Google, Microsoft, GitHub) | P1 |
| FR-001.4 | Password reset via email link | P0 |
| FR-001.5 | Session management with refresh tokens | P0 |
| FR-001.6 | Multi-factor authentication (TOTP) | P1 |
| FR-001.7 | Account lockout after failed attempts | P0 |
| FR-001.8 | "Remember me" functionality | P2 |

No changes from initial PRD. The scenario analysis confirms all requirements.

---

## FR-002: Role-Based Access Control (RBAC)

### Initial Requirements (PRD.draft.md)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-002.1 | Predefined roles: User, Admin, SystemAdmin | P0 |
| FR-002.2 | Role assignment by SystemAdmin | P0 |
| FR-002.3 | Permission-based endpoint authorization | P0 |
| FR-002.4 | Role hierarchy (SystemAdmin > Admin > User) | P0 |
| FR-002.5 | Custom permission sets per role | P1 |
| FR-002.6 | Role-based UI element visibility | P0 |

### Refined Requirements (PRD.md — from Scenario Analysis)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-002.7 | Protected data reveal action requires SystemAdmin role and creates audit event | P0 |

**Rationale**: Scenario S05 (Diana) shows that protected data reveal is a privileged action that must be tracked.

---

## FR-008: Onboarding System

### Initial Requirements (PRD.draft.md)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-008.1 | Welcome screen after first registration | P0 |
| FR-008.2 | Guided tour: create first task | P0 |
| FR-008.3 | Guided tour: complete first task | P0 |
| FR-008.4 | Guided tour: explore Kanban board | P1 |
| FR-008.5 | Progress indicators during onboarding | P0 |
| FR-008.6 | Skip option for experienced users | P0 |
| FR-008.7 | Onboarding completion celebration | P1 |
| FR-008.8 | Re-trigger onboarding from settings | P2 |

### Refined Requirements (PRD.md — from Scenario Analysis)

| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-008.2 | Guided tour: create first task via quick-add | P0 | MODIFIED - specify quick-add |
| FR-008.7 | Micro-celebration on task completion (animation + sound option) | P0 | UPGRADED from P1 |
| FR-008.9 | Onboarding uses contextual hints, not modal overlays | P0 | NEW |

**Rationale**: Scenario S01 shows the emotional arc: capture -> complete -> celebrate. Modal overlays feel heavy on mobile.
