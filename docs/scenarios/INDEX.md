# Scenarios

> User storyboards and interaction flows covering all key user journeys in LemonDo, from first-time registration through power usage, compliance operations, and v2 module workflows.

---

## Contents

| Document | Description | Status |
|----------|-------------|--------|
| [onboarding.md](./onboarding.md) | S01 (first-time registration), S03 (email registration & password flow), S10 (full journey: discovery to weekly active user) | Active |
| [task-management.md](./task-management.md) | S02 (daily task management — Sarah), S04 (Kanban power user — Marcus) | Active |
| [admin-compliance.md](./admin-compliance.md) | S05 (admin audit & compliance — Diana) | Active |
| [mobile-offline.md](./mobile-offline.md) | S06 (mobile offline usage — Sarah on a flight) | Active |
| [settings-preferences.md](./settings-preferences.md) | S07 (theme and language switching), S09 (multi-factor authentication setup) | Active |
| [engagement.md](./engagement.md) | S08 (churn prevention — inactive user re-engagement) | Active |
| [project-management.md](./project-management.md) | S-PM-01 through S-PM-03 — v2 project registration, worktrees, demo setup | Draft (v2) |
| [communications.md](./communications.md) | S-CM-01 through S-CM-03 — v2 unified inbox, linking comms to work, person-centric history | Draft (v2) |
| [people-management.md](./people-management.md) | S-PP-01 through S-PP-02 — v2 meeting preparation, knowledge capture | Draft (v2) |
| [agent-workflows.md](./agent-workflows.md) | S-AG-01 through S-AG-04 — v2 batch feature dev, email-to-task automation, sequential pipeline, agent follow-up | Draft (v2) |

---

## Summary

LemonDo's v1 scenarios cover the three core personas (Sarah the freelancer, Marcus the team lead, and Diana the system administrator) across ten storyboards. The scenarios span the full user lifecycle: initial discovery and registration (S01, S03), daily task management (S02, S04), administrative compliance operations (S05), mobile and offline usage (S06), settings customization (S07, S09), and re-engagement flows (S08). Scenario S10 provides the complete journey from first visit through sustained weekly usage.

Each storyboard is written from the user's perspective, with explicit analytics event annotations at each step. This dual purpose — human narrative and analytics specification — makes scenarios the primary source of truth for both UX decisions and the analytics event taxonomy.

The v2 scenarios (marked Draft) originate from `PRD.2.draft.md` and describe four new modules: project management, unified communications, people and company relationship management, and AI agent session orchestration. These scenarios are written for Bruno as a single power user rather than the multi-persona v1 model. They represent planned behavior, not yet implemented.

The Analytics Collection Points Summary below maps lifecycle stages to their key events and measurement purpose. This table is the authoritative reference for analytics coverage across all scenarios.

---

## Analytics Collection Points Summary

| Lifecycle Stage | Key Events | Purpose |
|----------------|------------|---------|
| Acquisition | `landing_page_viewed`, `signup_cta_clicked` | Measure top of funnel |
| Registration | `registration_submitted`, `email_verified` | Measure conversion |
| Activation | `onboarding_completed`, `first_task_completed` | Measure first value |
| Engagement | `task_created`, `task_completed`, `session_started` | Measure daily usage |
| Retention | `session_started` (D1, D7, D30) | Measure stickiness |
| Feature Adoption | `view_switched`, `pwa_installed`, `theme_toggled` | Measure discovery |
| Churn Prevention | `churn_email_sent`, `re_engagement_success` | Measure recovery |
| Compliance | `admin_panel_opened`, `protected_data_revealed`, `audit_log_searched` | Measure admin usage |
