# LemonDo - Product Requirements Document

> **Date**: 2026-02-13
> **Status**: Active
> **Previous Version**: [PRD.draft.md](./PRD.draft.md) (Initial Draft)
> **Informed By**: [SCENARIOS.md](./SCENARIOS.md) (User Storyboards)

---

## Review Summary

After creating detailed user scenarios and personas (SCENARIOS.md), several insights emerged that refine the initial PRD:

### Key Insights from Scenario Analysis

1. **Quick-add is the killer feature**: Sarah's workflow shows that the #1 interaction is rapidly capturing tasks. The task creation UX must be lightning-fast (< 2 taps/clicks to add a task).

2. **Mobile-first is not optional**: Sarah uses her phone 60% of the time. Kanban drag-and-drop on mobile must be native-feeling (long-press + drag), not a desktop afterthought.

3. **Onboarding must be emotionally rewarding**: The create-then-complete loop in onboarding needs micro-celebrations (animations, feedback) to establish positive association.

4. **Protected data redaction is the default, not the exception**: Diana's admin workflow shows that revealing protected data should require explicit action. Default to redacted everywhere.

5. **Offline-first is table stakes for mobile**: Sarah's flight scenario proves PWA offline must work for core operations (view, create, complete tasks).

6. **Analytics must be privacy-first**: HIPAA requires all analytics events to hash or exclude protected data. The analytics architecture must be designed with this constraint from day one.

---

## 1. Revised Functional Requirements

### FR-001: User Authentication (Unchanged)

No changes from initial PRD. The scenario analysis confirms all requirements.

### FR-002: RBAC (Minor Addition)

**Added**:
| ID | Requirement | Priority |
|----|-------------|----------|
| FR-002.7 | Protected data reveal action requires SystemAdmin role and creates audit event | P0 |

**Rationale**: Scenario S05 (Diana) shows that protected data reveal is a privileged action that must be tracked.

### FR-003: Task Management (Refined)

**Added/Modified**:
| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-003.11 | Quick-add mode: single input field, Enter to create | P0 | NEW - Scenario S02 |
| FR-003.12 | Full-form mode: all fields visible for detailed tasks | P0 | NEW - Scenario S04 |
| FR-003.1 | Create task - minimum required: title only | P0 | MODIFIED - was title + description |

**Rationale**: Sarah needs quick-add (title only, instant). Marcus needs full-form (all fields). Both must be first-class.

### FR-004: Kanban Board (Refined)

**Added**:
| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-004.7 | Mobile: long-press to pick up, drag to column | P0 | NEW |
| FR-004.8 | Drop zone visual indicators during drag | P0 | NEW |
| FR-004.9 | Card count per column visible in header | P0 | NEW |

**Rationale**: Scenario S04 (Marcus) specifically describes needing visual feedback during drag operations.

### FR-005: List View (Unchanged)

No changes needed.

### FR-006: System Administration (Refined)

**Added**:
| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-006.7 | Protected data default-redacted in all admin views | P0 | NEW |
| FR-006.8 | "Reveal" button per field with confirmation dialog | P0 | NEW |
| FR-006.9 | Protected data reveal logged as audit event | P0 | NEW |

**Rationale**: Scenario S05 (Diana) establishes that protected data must be hidden by default even for admins.

### FR-007: HIPAA Compliance (Refined)

**Added**:
| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-007.9 | Analytics events must hash all protected data | P0 | NEW |
| FR-007.10 | No protected data in structured log messages | P0 | NEW |
| FR-007.11 | Protected data reveal audit trail with IP, timestamp, admin ID | P0 | NEW |

### FR-008: Onboarding System (Refined)

**Modified**:
| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-008.2 | Guided tour: create first task via quick-add | P0 | MODIFIED - specify quick-add |
| FR-008.7 | Micro-celebration on task completion (animation + sound option) | P0 | UPGRADED from P1 |
| FR-008.9 | Onboarding uses contextual hints, not modal overlays | P0 | NEW |

**Rationale**: Scenario S01 shows the emotional arc: capture -> complete -> celebrate. Modal overlays feel heavy on mobile.

### FR-009: Communication & Churn Prevention (Refined)

**Added**:
| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-009.6 | Deep links in emails open directly to user's board | P0 | NEW |
| FR-009.7 | "Welcome back!" toast on re-engagement | P1 | NEW |
| FR-009.8 | Emails show top 3 pending tasks (no protected data) | P1 | NEW |

**Rationale**: Scenario S08 shows deep links and contextual email content drive re-engagement.

### FR-010: Product Analytics (Refined)

**Added**:
| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-010.8 | All events follow consistent schema (see SCENARIOS.md section 7) | P0 | NEW |
| FR-010.9 | Events include device context (type, locale, theme) | P1 | NEW |
| FR-010.10 | Offline events queued and synced | P1 | NEW |

---

## 2. Revised Non-Functional Requirements

### NFR-002: Responsive Design (Refined)

**Added**:
| ID | Requirement | Target | Change |
|----|-------------|--------|--------|
| NFR-002.6 | Quick-add accessible via floating action button on mobile | Always visible | NEW |
| NFR-002.7 | Kanban columns scroll horizontally on mobile with snap | Native gesture | NEW |

### NFR-003: PWA (Elevated)

**Modified**:
| ID | Requirement | Target | Change |
|----|-------------|--------|--------|
| NFR-003.2 | Offline task viewing AND creation AND completion | Full offline CRUD | UPGRADED |
| NFR-003.5 | Offline change indicator on affected tasks | Visual sync status | NEW |
| NFR-003.6 | Automatic sync on reconnection with conflict resolution | Last-write-wins | NEW |

**Rationale**: Scenario S06 (airplane) shows offline must support full task lifecycle, not just viewing.

### NFR-011: Micro-Interactions & UX Polish (New Section)

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-011.1 | Task creation animation (slide-in) | < 300ms |
| NFR-011.2 | Task completion animation (strikethrough + fade) | < 500ms |
| NFR-011.3 | Drag-and-drop with ghost element and drop shadow | Real-time |
| NFR-011.4 | Theme switch transition (no white flash) | Instant |
| NFR-011.5 | Loading skeletons for all async content | Immediate |
| NFR-011.6 | Empty states with helpful illustrations/CTAs | All empty views |
| NFR-011.7 | Toast notifications for async operations | Non-blocking |

**Rationale**: Multiple scenarios emphasize that UX polish (animations, celebrations, feedback) is core to the product, not decoration.

---

## 3. Updated Architecture Decisions

### 3.1 Task Creation Strategy

Based on scenarios, the application requires two creation paths:

```
Quick-Add Path (Sarah):
  Input field -> Enter -> Task created (title only)
  - Always visible on board/list views
  - Mobile: floating action button opens quick-add
  - Keyboard shortcut: "n" opens quick-add on desktop

Full-Form Path (Marcus):
  "+" button -> Modal/Panel with all fields -> Save
  - Title, description (markdown), priority, due date, tags
  - Keyboard shortcut: "N" (shift+n) opens full form
```

### 3.2 Analytics Architecture

Privacy-first analytics (HIPAA):
- Frontend: Custom analytics service wrapping events
- Backend: Domain events published to analytics processor
- Storage: Separate analytics database (no protected data)
- All user identifiers are hashed (SHA-256)
- All task identifiers are hashed
- No task content (titles, descriptions) in analytics

### 3.3 Offline Strategy

```
Service Worker Cache:
  - App shell (HTML, CSS, JS) -> Cache-first
  - API responses -> Network-first with cache fallback
  - Task data -> IndexedDB for offline storage

Sync Strategy:
  - Offline operations queued in IndexedDB
  - On reconnection: replay queue in order
  - Conflict resolution: server timestamp wins
  - Visual indicator: "syncing..." -> "synced"
```

### 3.4 Protected Data Handling Strategy

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

## 4. Updated Success Criteria

Based on scenario analysis, the success metrics are refined as follows:

| Metric | Original Target | Revised Target | Rationale |
|--------|----------------|----------------|-----------|
| Time to first task created | Not defined | < 60 seconds from signup | S01: onboarding speed |
| Quick-add usage | Not defined | > 70% of tasks via quick-add | S02: quick capture dominance |
| Mobile session share | Not defined | > 40% of sessions | S02, S06: mobile-first thesis |
| Offline operations per week | Not defined | Measured (baseline) | S06: offline usage tracking |
| Protected data reveal frequency | Not defined | < 10% of admin sessions | S05: minimal protected data exposure |

---

## 5. Unchanged from Initial PRD

The following sections carry over without modification:
- Section 5: Success Metrics (original targets retained, new ones added above)
- Section 6: Out of Scope
- Section 7: Risks and Mitigations

Refer to [PRD.draft.md](./PRD.draft.md) for these sections.
