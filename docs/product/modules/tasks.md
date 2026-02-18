# Tasks Module

> **Source**: Extracted from docs/PRD.draft.md §2 FR-003, FR-005, docs/PRD.md §1 FR-003, FR-005
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## FR-003: Task Management

### Initial Requirements (PRD.draft.md)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-003.1 | Create task with title, description, priority, due date | P0 |
| FR-003.2 | Edit task properties | P0 |
| FR-003.3 | Delete task (soft delete with audit trail) | P0 |
| FR-003.4 | Mark task as complete/incomplete | P0 |
| FR-003.5 | Assign priority levels (None, Low, Medium, High, Critical) | P0 |
| FR-003.6 | Set due dates with reminder notifications | P1 |
| FR-003.7 | Add tags/labels to tasks | P1 |
| FR-003.8 | Task search and filtering | P1 |
| FR-003.9 | Bulk operations (complete, delete, move) | P2 |
| FR-003.10 | Task archiving | P1 |

### Refined Requirements (PRD.md — from Scenario Analysis)

| ID | Requirement | Priority | Change |
|----|-------------|----------|--------|
| FR-003.1 | Create task - minimum required: title only | P0 | MODIFIED - was title + description |
| FR-003.11 | Quick-add mode: single input field, Enter to create | P0 | NEW - Scenario S02 |
| FR-003.12 | Full-form mode: all fields visible for detailed tasks | P0 | NEW - Scenario S04 |

**Rationale**: Sarah needs quick-add (title only, instant). Marcus needs full-form (all fields). Both must be first-class.

### Task Creation Strategy

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

---

## FR-005: List View

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-005.1 | Sortable table/list of all tasks | P0 |
| FR-005.2 | Sort by title, priority, due date, status, created date | P0 |
| FR-005.3 | Inline editing of task properties | P1 |
| FR-005.4 | Grouping by status, priority, or due date | P1 |
| FR-005.5 | Pagination or infinite scroll | P0 |
