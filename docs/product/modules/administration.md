# Administration Module

> **Source**: Extracted from docs/PRD.draft.md §2 FR-006, FR-007, FR-009, FR-010, docs/PRD.md §1 FR-006, FR-007, FR-009, FR-010
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## FR-006: System Administration

### Initial Requirements (PRD.draft.md)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-006.1 | User management (view, deactivate, role assignment) | P0 |
| FR-006.2 | Complete audit log of all system actions | P0 |
| FR-006.3 | Audit log search and filtering | P1 |
| FR-006.4 | System health dashboard | P1 |
| FR-006.5 | User activity reports | P1 |
| FR-006.6 | Data export capabilities | P2 |

### Refined Requirements (PRD.md — from Scenario Analysis)

| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-006.7 | Protected data default-redacted in all admin views | P0 | NEW |
| FR-006.8 | "Reveal" button per field with confirmation dialog | P0 | NEW |
| FR-006.9 | Protected data reveal logged as audit event | P0 | NEW |

**Rationale**: Scenario S05 (Diana) establishes that protected data must be hidden by default even for admins.

---

## FR-007: HIPAA Compliance

### Initial Requirements (PRD.draft.md)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-007.1 | PII/PHI data encryption at rest | P0 |
| FR-007.2 | PII/PHI redaction in system logs | P0 |
| FR-007.3 | PII/PHI redaction in admin views (masked with reveal) | P0 |
| FR-007.4 | Comprehensive audit trail for data access | P0 |
| FR-007.5 | Data retention policies | P1 |
| FR-007.6 | Right to erasure (data deletion) | P1 |
| FR-007.7 | Access control audit reports | P1 |
| FR-007.8 | BAA (Business Associate Agreement) support structure | P2 |

### Refined Requirements (PRD.md — from Scenario Analysis)

| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-007.9 | Analytics events must hash all protected data | P0 | NEW |
| FR-007.10 | No protected data in structured log messages | P0 | NEW |
| FR-007.11 | Protected data reveal audit trail with IP, timestamp, admin ID | P0 | NEW |

### Protected Data Handling Strategy

```
At Rest:
  - Sensitive fields encrypted in database (AES-256)
  - Encryption key in environment variable / Azure Key Vault

In Transit:
  - HTTPS only
  - No protected data in URL parameters

In Logs:
  - Structured logging with protected data redaction middleware
  - Email: s***@example.com
  - Names: S*** L***
  - Custom Serilog destructuring policy

In Admin Views:
  - Default: masked (s***@example.com)
  - Reveal: explicit action -> audit log entry
  - Auto-hide after 30 seconds

In Analytics:
  - User ID: SHA-256 hash
  - No task content
  - No email addresses
  - Only behavioral data + hashed identifiers
```

---

## FR-009: Communication & Churn Prevention

### Initial Requirements (PRD.draft.md)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-009.1 | Welcome email after registration | P0 |
| FR-009.2 | Inactivity reminder emails (3, 7, 14 days) | P1 |
| FR-009.3 | Weekly task summary email (opt-in) | P2 |
| FR-009.4 | Achievement/milestone notifications | P2 |
| FR-009.5 | In-app notification center | P1 |

### Refined Requirements (PRD.md — from Scenario Analysis)

| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-009.6 | Deep links in emails open directly to user's board | P0 | NEW |
| FR-009.7 | "Welcome back!" toast on re-engagement | P1 | NEW |
| FR-009.8 | Emails show top 3 pending tasks (no protected data) | P1 | NEW |

**Rationale**: Scenario S08 shows deep links and contextual email content drive re-engagement.

---

## FR-010: Product Analytics

### Initial Requirements (PRD.draft.md)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-010.1 | Registration funnel tracking | P0 |
| FR-010.2 | Onboarding completion rates | P0 |
| FR-010.3 | Feature adoption tracking | P1 |
| FR-010.4 | Task completion rates and velocity | P1 |
| FR-010.5 | User retention cohort analysis | P1 |
| FR-010.6 | Session duration and frequency metrics | P2 |
| FR-010.7 | Conversion funnel from signup to active user | P0 |

### Refined Requirements (PRD.md — from Scenario Analysis)

| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-010.8 | All events follow consistent schema (see [analytics.md](../analytics.md) section 7) | P0 | NEW |
| FR-010.9 | Events include device context (type, locale, theme) | P1 | NEW |
| FR-010.10 | Offline events queued and synced | P1 | NEW |
