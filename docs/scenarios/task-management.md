# Task Management Scenarios

> **Source**: Extracted from docs/SCENARIOS.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Scenario S02: Daily Task Management (Sarah)

**Context**: It's Monday morning. Sarah opens LemonDo to plan her day.

```
Step 1: Sarah opens LemonDo (PWA on her phone)
  -> App loads instantly (cached via service worker)
  -> She sees her Kanban board with tasks from last week
  -> "To Do" has 3 tasks, "In Progress" has 1, "Done" has 5
  [analytics: session_started, device: mobile, view: kanban]

Step 2: Sarah adds new tasks quickly
  -> She taps the "+" button at the top
  -> Quick-add: types "Client call at 2pm" -> Enter
  -> Quick-add: types "Invoice Acme Corp" -> Enter
  -> Quick-add: types "Review brand guidelines" -> Enter
  -> Each appears instantly in "To Do"
  [analytics: task_created x3, source: quick_add]

Step 3: Sarah prioritizes by dragging
  -> She long-presses "Client call at 2pm" and drags it to "In Progress"
  -> She sets "Invoice Acme Corp" to High priority via a quick-tap on priority icon
  [analytics: task_moved, task_edited]

Step 4: During the day, Sarah completes tasks
  -> After each client call, she swipes the task to complete
  -> Satisfying completion animation each time
  -> End of day: 4 tasks completed
  [analytics: task_completed x4]

Step 5: Sarah switches to list view for a quick overview
  -> She taps the list icon in the top bar
  -> Sees all tasks sorted by priority
  -> Filters to show only "To Do" tasks
  [analytics: view_switched, filter_applied]
```

**Expected Emotion**: Productive, in control. "I can see everything I need."

---

## Scenario S04: Kanban Power User (Marcus)

**Context**: Marcus manages his team's sprint using the Kanban board on his desktop.

```
Step 1: Marcus logs in on desktop
  -> Full-width Kanban board fills the screen
  -> Columns: Backlog | To Do | In Progress | In Review | Done
  -> Tasks show priority badges, due dates, and tags
  [analytics: session_started, device: desktop, view: kanban]

Step 2: Marcus creates tasks with full details
  -> Clicks "+" on "Backlog" column
  -> Full task form: title, description (markdown), priority, due date, tags
  -> Adds "Implement patient data API" with High priority, tags: [backend, sprint-12]
  [analytics: task_created, has_description: true, has_due_date: true, has_tags: true]

Step 3: Marcus drags tasks into sprint columns
  -> Moves 5 tasks from Backlog to To Do
  -> Drag-and-drop is smooth, with drop zone indicators
  -> Column card count updates in real-time
  [analytics: task_moved x5]

Step 4: Marcus uses search and filters
  -> Types "patient" in search bar
  -> Results highlight matching tasks across all columns
  -> Filters by tag "backend" to see only backend tasks
  [analytics: search_performed, filter_applied]

Step 5: Marcus switches to list view for reporting
  -> List view shows all tasks with their columns as a "Status" field
  -> Sorts by due date to see what's coming up
  -> Exports view (future feature) for standup notes
  [analytics: view_switched, to: list]
```
