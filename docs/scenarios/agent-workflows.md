# Agent Workflow Scenarios

> **Source**: Extracted from docs/PRD.2.draft.md §7.4
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## Scenario S-AG-01: Batch Feature Development (Bruno)

**Context**: It's the start of a new development cycle. Bruno has seven tasks sitting in the backlog for the next minor release of his project. Rather than work through them one by one himself, he wants to spin up parallel agent sessions — one per task — and let them run while he focuses on planning and communication.

```
Step 1: Bruno opens LemonDo and navigates to the Tasks board for "lemon-todo-v2"
  -> The Kanban board loads with the project filter already applied
  -> The Backlog column shows 12 tasks; 7 are tagged [v2-next]
  -> Bruno switches to List view for easier multi-select
  [analytics: session_started, device: desktop, view: kanban]
  [analytics: view_switched, from: kanban, to: list]
  [analytics: filter_applied, filter_type: project, project_id: hashed]

Step 2: Bruno selects the 7 [v2-next] tasks using checkboxes
  -> Each checkbox click adds the task to a selection tray at the bottom of the screen
  -> The tray reads "7 tasks selected" with action buttons: Assign, Move, Start Agents
  [analytics: task_selected, count: 7, source: list_view]

Step 3: Bruno clicks "Start Agents" in the selection tray
  -> A configuration panel slides in from the right
  -> It shows: Execution Mode (Parallel / Sequential), Budget per session, Total budget cap
  -> Project "lemon-todo-v2" is pre-filled from the board's current filter
  -> Execution Mode defaults to "Parallel" because 7 independent tasks were selected
  [analytics: agent_batch_panel_opened, task_count: 7, project_id: hashed]

Step 4: Bruno reviews and adjusts the budget settings
  -> Per-session budget shows $2.00 (default from project settings)
  -> Total budget cap auto-calculates to $14.00 (7 x $2.00)
  -> Bruno changes per-session budget to $3.00; total updates to $21.00
  -> He leaves the "Auto-create worktree" toggle on
  [analytics: agent_budget_configured, per_session: 3.00, total_cap: 21.00, session_count: 7]

Step 5: Bruno clicks "Create Worktrees and Start"
  -> A progress overlay appears: "Creating 7 worktrees..."
  -> Each worktree creation logs inline: "feature/ag-session-001 — ready"
  -> After all 7 are created: "Starting 7 agent sessions..."
  -> The overlay closes and the Agents dashboard opens automatically
  [analytics: agent_batch_started, mode: parallel, worktree_count: 7, project_id: hashed]
  [analytics: worktree_created x7, source: agent_batch, project_id: hashed]
  [analytics: agent_session_started x7, mode: parallel, has_budget: true]
  [cross-module: Projects — worktrees created via PM-004 (Create and manage git worktrees)]

Step 6: Bruno watches the Agents dashboard update in real-time
  -> Seven session cards fill the dashboard, each showing: task title, worktree name, status badge, token count, elapsed time
  -> Status badges cycle: Queued -> Running -> (for faster tasks) Completed
  -> Three sessions show "Running"; four show "Queued" (system throttling to stay within budget)
  -> The total budget tracker at the top reads "$3.41 of $21.00 used"
  [analytics: agent_dashboard_viewed, active_sessions: 7, view: grid]
  [analytics: agent_budget_tracker_viewed, spent: 3.41, cap: 21.00]

Step 7: Bruno clicks into one of the running sessions to see its output
  -> The session detail panel opens beside the dashboard (split view)
  -> A live terminal stream shows the agent's current work: file reads, edits, test runs
  -> The agent has just run the test suite: "All 403 tests passed"
  -> Bruno reads through the output and feels confident the agent is on track
  [analytics: agent_session_detail_viewed, session_id: hashed, output_lines_visible: 47]
  [analytics: agent_log_scrolled, session_id: hashed]

Step 8: One session completes; Bruno receives a desktop notification
  -> Notification: "Agent finished: Implement rate limiting middleware — Review ready"
  -> Bruno clicks the notification and is taken directly to the review panel
  -> The review panel shows: files changed (3), tests added (12), tests passing (415/415), diff preview
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 3, tests_added: 12]

Step 9: Bruno reviews the diff and approves the merge
  -> He scrolls through the diff; the changes look correct
  -> He clicks "Approve and Merge"
  -> LemonDo merges the worktree branch into the project's develop branch
  -> The task card on the board moves from "In Progress" to "Done" automatically
  -> The worktree is cleaned up; the session card shows "Merged"
  [analytics: agent_change_approved, session_id: hashed, files_changed: 3]
  [analytics: agent_worktree_merged, session_id: hashed, project_id: hashed]
  [analytics: task_completed, source: agent_merge, session_id: hashed]
  [cross-module: Tasks — task auto-moved to Done (task_completed event)]
  [cross-module: Projects — worktree removed after merge (PM-004)]

Step 10: Three hours later, all 7 sessions complete
  -> Dashboard shows all 7 cards in "Merged" or "Completed" state
  -> Total cost tracker reads "$17.83 of $21.00 used" — under budget
  -> Bruno opens the session history to see the full audit trail
  [analytics: agent_batch_completed, session_count: 7, total_cost: 17.83, cap: 21.00, tasks_merged: 6, tasks_rejected: 1]
  [analytics: agent_history_viewed, session_count: 7]
```

**Expected Emotion**: Leverage and calm. "I just shipped seven features while drinking my morning coffee."

---

## Scenario S-AG-02: Email-to-Task Automation (Bruno)

**Context**: Bruno has been manually triaging customer feedback emails every morning — an hour of copy-pasting email subjects into tasks, tagging them, and assigning them to the right project. He has set up an automation agent that runs on a schedule each morning. Today he's checking it for the first time after letting it run overnight.

```
Step 1: Bruno opens LemonDo on his desktop at 9:00am
  -> The Agents dashboard tab shows a badge: "1 scheduled run completed"
  -> He clicks the Agents tab
  [analytics: session_started, device: desktop, view: agents_dashboard]
  [analytics: agent_schedule_badge_seen, completed_runs: 1]

Step 2: Bruno opens the completed automation run
  -> The run card shows: "Morning Inbox Triage — Completed at 07:14am"
  -> Summary: "Processed 23 emails. Created 8 tasks. Skipped 15 (newsletters, automated alerts)."
  -> An expandable log shows each email processed with its classification decision
  [analytics: agent_scheduled_run_opened, session_id: hashed, emails_processed: 23, tasks_created: 8]

Step 3: Bruno reviews the generated tasks
  -> He clicks "View Created Tasks" — this opens the Tasks board filtered to today's agent-created tasks
  -> Eight task cards appear, each with: title (from email subject), tag "customer-feedback", assigned project, source (email sender — hashed), priority (AI-suggested)
  -> Three are tagged "bug" (P1), three are "feature-request" (P2), two are "question" (P3)
  [analytics: agent_created_tasks_viewed, task_count: 8, source: email_automation]
  [analytics: filter_applied, filter_type: source, source: agent_automation]
  [cross-module: Comms — email threads linked to each task (CM-012)]

Step 4: Bruno spots one task he wants to adjust
  -> The task reads "Update pricing page" — tagged as "feature-request P2"
  -> Bruno thinks this is actually urgent (customer threatening churn)
  -> He opens the task, changes priority to P0, edits the title to "URGENT: Pricing page confusion causing churn risk"
  -> He links the original email thread from the Comms panel (already auto-linked by the agent)
  [analytics: task_edited, fields_changed: priority, title, source: agent_created_task]
  [analytics: task_priority_changed, from: P2, to: P0, task_id: hashed]

Step 5: Bruno approves the remaining tasks with no changes
  -> He batch-selects the other 7 tasks and clicks "Confirm — Move to Backlog"
  -> All 7 move from the agent staging area into the active Backlog
  [analytics: agent_tasks_approved, task_count: 7, destination: backlog]

Step 6: Bruno reviews the skipped emails to make sure nothing was missed
  -> He clicks "View Skipped (15)" in the run log
  -> The list shows email subjects and the agent's reason: "Newsletter", "GitHub Actions notification", "Automated billing receipt"
  -> One item catches his eye: "Re: Integration broken" — classified as "automated alert"
  -> Bruno clicks it, reads the email in the Comms panel, and manually creates a P0 bug task
  [analytics: agent_skipped_emails_reviewed, count: 15]
  [analytics: agent_skip_decision_overridden, session_id: hashed, email_id: hashed]
  [analytics: task_created, source: manual, triggered_by: agent_review, priority: P0]
  [cross-module: Comms — email viewed in unified inbox (CM-007)]

Step 7: Bruno notes the agent misclassified that email and updates the automation configuration
  -> He opens the automation's settings panel
  -> He adds a rule: "Emails with subject containing 'broken' or 'not working' -> classify as bug, priority P1 minimum"
  -> He saves the configuration; the agent will use this rule from tomorrow's run
  [analytics: agent_config_updated, session_id: hashed, rules_added: 1]
  [analytics: agent_template_saved, template_id: hashed]
```

**Expected Emotion**: Efficiency with control. "It did 80% of the work. I just course-corrected the edge cases."

---

## Scenario S-AG-03: Sequential Quality Pipeline (Bruno)

**Context**: Bruno has a project with a strict linear dependency chain — each feature relies on the database migrations from the previous one. Parallel agents would create merge conflicts. He sets up a sequential queue: five tasks, one agent at a time, each running the full verification gate before passing control to the next.

```
Step 1: Bruno navigates to the project "lemon-todo-v2" task board
  -> List view shows 5 tasks in the Backlog tagged [pipeline-v2.1], ordered by dependency
  -> He multi-selects all 5 tasks
  [analytics: session_started, device: desktop, view: list]
  [analytics: task_selected, count: 5, source: list_view]

Step 2: Bruno clicks "Start Agents" and configures a sequential queue
  -> The configuration panel opens
  -> He switches Execution Mode from "Parallel" to "Sequential"
  -> A reordering list appears showing the 5 tasks in priority order
  -> He verifies the order matches the dependency chain and leaves it unchanged
  -> Budget: $5.00 per session, $25.00 total cap
  -> He enables "Require verification gate before handoff" (runs test suite before agent signals done)
  [analytics: agent_queue_panel_opened, task_count: 5, mode: sequential, project_id: hashed]
  [analytics: agent_execution_mode_selected, mode: sequential]
  [analytics: agent_budget_configured, per_session: 5.00, total_cap: 25.00, session_count: 5]
  [analytics: agent_verification_gate_enabled, session_id: hashed]

Step 3: Bruno clicks "Start Queue"
  -> LemonDo creates a single worktree "feature/pipeline-v2.1" for the entire queue
  -> Agent session #1 starts immediately; sessions 2-5 show "Queued"
  -> A queue progress bar appears at the top of the Agents dashboard: "Task 1 of 5"
  [analytics: agent_queue_started, mode: sequential, task_count: 5, project_id: hashed]
  [analytics: agent_session_started, mode: sequential, position: 1, has_budget: true]
  [analytics: worktree_created, count: 1, source: agent_queue, project_id: hashed]
  [cross-module: Projects — worktree created via PM-004]

Step 4: Bruno monitors Agent #1 working on "Add user preferences schema migration"
  -> He clicks into the session to see the live output
  -> The agent creates the migration file, updates the EF Core DbContext, writes 18 new tests
  -> The terminal stream shows: "Running verification gate... pnpm verify ... 421/421 tests passing"
  -> The agent signals completion; the session status changes to "Verification Passed"
  [analytics: agent_session_detail_viewed, session_id: hashed, position: 1]
  [analytics: agent_verification_gate_passed, session_id: hashed, tests_passed: 421]

Step 5: Agent #2 starts automatically after #1's verification passes
  -> Bruno sees the queue bar advance: "Task 2 of 5"
  -> A handoff note appears: "Agent #2 started with Agent #1's committed changes included"
  -> Agent #2 has the updated migration and schema available immediately
  -> Bruno does not need to do anything — the handoff is automatic
  [analytics: agent_queue_handoff, from_session: hashed, to_session: hashed, position: 2]
  [analytics: agent_session_started, mode: sequential, position: 2, has_budget: true]

Step 6: Agent #3 fails its verification gate
  -> Bruno receives a desktop notification: "Agent session failed: Implement preferences UI — 3 tests failing"
  -> He clicks through to the session detail
  -> The terminal shows the specific failing tests: three snapshot tests that need updating
  -> The session status is "Verification Failed — Awaiting review"
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_verification_gate_failed, session_id: hashed, tests_failing: 3, position: 3]
  [analytics: agent_review_panel_opened, session_id: hashed, failure_reason: test_failures]

Step 7: Bruno reviews the failure and decides how to handle it
  -> He reads the failing test output — the snapshots are outdated, not a real bug
  -> He clicks "Retry with instruction" and types: "Update snapshot baselines before running the verification gate"
  -> The agent session restarts with this additional instruction
  [analytics: agent_retry_requested, session_id: hashed, instruction_added: true]
  [analytics: agent_session_restarted, session_id: hashed, position: 3, reason: verification_failed]

Step 8: Agent #3 passes on retry; the queue resumes
  -> The terminal shows: "Updating 3 snapshots... Running verification gate... 424/424 tests passing"
  -> The queue bar advances; agents #4 and #5 proceed without issue
  [analytics: agent_verification_gate_passed, session_id: hashed, tests_passed: 424, was_retry: true]
  [analytics: agent_queue_resumed, position: 4]

Step 9: All 5 tasks complete; Bruno reviews the full queue output
  -> The Agents dashboard shows all 5 sessions as "Verification Passed"
  -> A "Review All and Merge" button appears
  -> Bruno clicks it — a consolidated diff view shows all changes across the 5 sessions
  -> Total cost: $18.70 of $25.00 used
  -> He approves the merge; LemonDo merges the worktree branch into develop
  -> All 5 task cards move to "Done" on the board
  [analytics: agent_queue_completed, session_count: 5, total_cost: 18.70, cap: 25.00]
  [analytics: agent_batch_reviewed, session_count: 5, files_changed: 23]
  [analytics: agent_change_approved, session_count: 5, project_id: hashed]
  [analytics: agent_worktree_merged, project_id: hashed]
  [analytics: task_completed x5, source: agent_merge]
  [cross-module: Tasks — 5 tasks moved to Done]
  [cross-module: Projects — worktree removed after merge (PM-004)]
```

**Expected Emotion**: Precision and trust. "It ran exactly how I designed it. No surprises."

---

## Scenario S-AG-04: Agent Creates Follow-Up Work (Bruno)

**Context**: Bruno has a single agent session running to implement a new API endpoint. Midway through, the agent discovers that an existing validation function has a latent bug that would affect the new endpoint. Rather than interrupting Bruno, the agent calls the LemonDo API to log the finding as a new bug task. Bruno sees it appear on his board without the session ever pausing.

```
Step 1: Bruno starts a single agent session for "Implement /projects/:id/status endpoint"
  -> He opens the Agents tab, clicks "New Session"
  -> He selects the project "lemon-todo-v2", the task from the board, and sets a $4.00 budget
  -> He clicks "Start" — the session begins and he navigates away to review emails
  [analytics: agent_session_panel_opened, source: manual]
  [analytics: agent_session_started, mode: single, has_budget: true, project_id: hashed]
  [analytics: worktree_created, count: 1, source: agent_single, project_id: hashed]
  [cross-module: Projects — worktree created for session (PM-004, PM-005)]

Step 2: The agent works autonomously on the endpoint implementation
  -> Bruno is in the Comms tab reviewing emails
  -> The Agents tab shows a small "1 running" badge but Bruno has not looked at it
  -> The agent reads the codebase, implements the controller, writes integration tests
  [analytics: agent_session_running, session_id: hashed, elapsed_minutes: 12]

Step 3: The agent discovers a bug in an existing shared validator
  -> While writing tests for the new endpoint, the agent finds that ProjectValidator.ValidateStatus()
     returns true for an invalid status value "archived_pending" — a value not in the enum
  -> The agent cannot fix this in scope without risking the current task's focus
  -> It calls the LemonDo Agent API: POST /api/agent/tasks with the bug details
  [analytics: agent_api_called, endpoint: POST_tasks, session_id: hashed, project_id: hashed]
  [cross-module: Agent API (AG-012) — agent creates a task programmatically]

Step 4: LemonDo receives the API call and creates the task
  -> A new task appears on Bruno's board: "Bug: ProjectValidator accepts invalid 'archived_pending' status"
  -> Tags: [bug, agent-discovered, validators, lemon-todo-v2]
  -> Priority: P1 (set by the agent based on the bug's impact)
  -> Linked to the current session's task as context ("Discovered while implementing /projects/:id/status")
  -> The session card in the Agents tab shows a small note: "1 task created via Agent API"
  [analytics: task_created, source: agent_api, session_id: hashed, project_id: hashed, priority: P1]
  [analytics: agent_api_task_created, session_id: hashed, task_id: hashed, linked_to: hashed]

Step 5: Bruno notices the new task appear in his board — without any notification
  -> He's still in the Comms tab; a subtle badge appears on the Tasks tab: "+1"
  -> He clicks over and sees the new bug task at the top of his P1 list
  -> He reads the description: it includes the exact line of code, the invalid value, and a suggested fix
  -> He thinks "Oh, I never would have caught that in code review"
  [analytics: task_viewed, task_id: hashed, source: agent_created]

Step 6: Bruno reads the agent's session note about the discovery
  -> He clicks the session link on the task card to open the Agents panel for that session
  -> In the session log, he finds the agent's note: "Discovered during test setup for step 3.
     ValidateStatus() does not guard against 'archived_pending'. Logged as separate task to avoid scope creep."
  -> Bruno appreciates that the agent stayed focused on its assigned task
  [analytics: agent_session_detail_viewed, session_id: hashed, source: task_link]
  [analytics: agent_log_scrolled, session_id: hashed]

Step 7: The agent completes its original task and notifies Bruno
  -> Desktop notification: "Agent finished: Implement /projects/:id/status endpoint — Review ready"
  -> Bruno clicks through to the review panel
  -> Files changed: 4, Tests added: 9, All 424 tests passing, Cost: $2.87 of $4.00 budget
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 4, tests_added: 9]
  [analytics: agent_budget_tracker_viewed, spent: 2.87, cap: 4.00]

Step 8: Bruno approves the merge and schedules the discovered bug for the next session
  -> He approves the main task's diff — the endpoint implementation looks solid
  -> He drags the new bug task into the "Up Next" column on his board
  -> He creates a new single agent session for the bug fix and sets it to start in 30 minutes
  [analytics: agent_change_approved, session_id: hashed, files_changed: 4]
  [analytics: agent_worktree_merged, session_id: hashed, project_id: hashed]
  [analytics: task_completed, source: agent_merge, task_id: hashed]
  [analytics: task_moved, task_id: hashed, from_column: backlog, to_column: up_next]
  [analytics: agent_session_scheduled, session_id: hashed, delay_minutes: 30]
  [cross-module: Tasks — original task completed; bug task queued]
  [cross-module: Projects — worktree merged and cleaned up]
```

**Expected Emotion**: Surprise and trust. "The agent found a bug I didn't even know existed. That's genuinely useful."

---

## Scenario Coverage Matrix

| FR ID | Requirement | Scenarios |
|-------|-------------|-----------|
| AG-001 | Start a new Claude Code agent session from UI | S-AG-01 (Step 5), S-AG-03 (Step 3), S-AG-04 (Step 1) |
| AG-002 | View active agent sessions and their status | S-AG-01 (Step 6), S-AG-03 (Steps 4, 5, 6), S-AG-04 (Step 2) |
| AG-003 | View agent session output/logs in real-time | S-AG-01 (Step 7), S-AG-03 (Step 4), S-AG-04 (Step 6) |
| AG-004 | Assign a task to an agent session | S-AG-01 (Steps 2-5), S-AG-03 (Steps 1-3), S-AG-04 (Step 1) |
| AG-005 | Auto-create worktree for agent session | S-AG-01 (Step 5), S-AG-03 (Step 3), S-AG-04 (Step 1) |
| AG-006 | Select multiple tasks and batch-assign to agent queue | S-AG-01 (Steps 2-3), S-AG-03 (Steps 1-2) |
| AG-007 | Parallel execution mode: multiple agents on separate worktrees | S-AG-01 (Steps 4-6) |
| AG-008 | Sequential execution mode: one agent finishes, next starts | S-AG-03 (Steps 2-5, 8) |
| AG-009 | Work queue with priority ordering | S-AG-03 (Step 2), S-AG-02 (Step 3) |
| AG-010 | Budget management: set token/cost limits per session | S-AG-01 (Step 4), S-AG-03 (Step 2), S-AG-04 (Step 1) |
| AG-011 | Budget management: set limits per queue | S-AG-01 (Step 4), S-AG-03 (Step 2) |
| AG-012 | Agent API: expose REST endpoints for agents to create/manage tasks | S-AG-04 (Steps 3-4) |
| AG-013 | Agent API: expose endpoints for agents to update project state | S-AG-04 (Step 6) — agent logs decision via session note |
| AG-014 | Agent API: expose endpoints for agents to add people/comms learnings | Not directly covered — deferred (no people/comms interaction in these scenarios) |
| AG-015 | Agent-driven email-to-task automation | S-AG-02 (all steps) |
| AG-016 | Agent session templates (predefined objectives, context, constraints) | S-AG-02 (Step 7) — automation config as reusable template |
| AG-017 | Claude Agent SDK integration for custom agent behaviors | S-AG-02 (Step 2) — scheduled automation implies SDK-level agent |
| AG-018 | Session history and audit trail | S-AG-01 (Step 10), S-AG-02 (Step 2), S-AG-03 (Step 9) |
| AG-019 | Approve/reject agent-proposed changes before merge | S-AG-01 (Steps 8-9), S-AG-03 (Step 9), S-AG-04 (Step 8) |
| AG-020 | Agent notification on completion/failure | S-AG-01 (Step 8), S-AG-03 (Steps 6, 8), S-AG-04 (Step 7) |
