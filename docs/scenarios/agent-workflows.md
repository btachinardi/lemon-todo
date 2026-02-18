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

## Scenario S-AG-05: Real-Time Session Monitoring and Steering (Bruno)

**Context**: Bruno has started a complex agent session to refactor the authentication module's token handling across the full stack — frontend, API, and database migration included. This is high-stakes work and he wants to stay close to it, watching the agent's decisions in real time rather than walking away.

```
Step 1: Bruno opens the running session's detail view from the Agents dashboard
  -> The session card shows: "Refactor token handling — Running — 8m elapsed — 23% context"
  -> He clicks the card to open the full session detail panel
  -> The panel opens in split view alongside the dashboard
  [analytics: agent_session_detail_viewed, session_id: hashed, context_pct: 23, elapsed_minutes: 8]
  [analytics: agent_dashboard_viewed, active_sessions: 1, view: split]

Step 2: Bruno sees the structured activity stream flowing in real time
  -> The stream shows compact items stacked chronologically, newest at the bottom:
       [file_read]  src/auth/token.service.ts  (2.1kb)
       [message]    "Reading token service — checking current refresh logic"
       [file_read]  src/auth/token.store.ts  (1.4kb)
       [bash]       pnpm test --filter=auth  (running...)
  -> Each item shows an icon indicating its type, a short label, and elapsed time
  -> The stream auto-scrolls as new items arrive
  [analytics: agent_activity_stream_viewed, session_id: hashed, items_visible: 4]
  [analytics: agent_stream_auto_scroll_enabled, session_id: hashed]

Step 3: Bruno expands a file_read item to inspect the agent's view of the file
  -> He clicks the [file_read] item for token.service.ts
  -> The item expands in place, showing: filename, file size, a preview of the first 30 lines
  -> A "View full file" link opens the file in a side panel
  -> The tool call's raw input/output is accessible via a "Show raw" toggle (collapsed by default)
  [analytics: agent_tool_call_expanded, session_id: hashed, tool_type: file_read, had_preview: true]

Step 4: A bash item completes and Bruno expands it to see the test output
  -> The [bash] item for pnpm test updates its status: "Completed — 403 passed"
  -> Bruno expands it: the expanded view shows the command, exit code, and truncated stdout (last 50 lines)
  -> A "Show full output" toggle reveals the complete output
  -> He confirms the tests are green and collapses the item
  [analytics: agent_tool_call_expanded, session_id: hashed, tool_type: bash, had_preview: true]
  [analytics: agent_tool_call_collapsed, session_id: hashed, tool_type: bash]

Step 5: An edit item appears and Bruno inspects the diff inline
  -> A new [edit] item appears: "Modified src/auth/token.service.ts"
  -> Bruno expands it: the expanded view shows a side-by-side diff with syntax highlighting
  -> He scans the diff — the agent is modifying the refreshToken() method
  -> The change looks correct; he collapses the item and continues watching
  [analytics: agent_tool_call_expanded, session_id: hashed, tool_type: edit, had_diff: true]
  [analytics: agent_diff_viewed, session_id: hashed, file_id: hashed, lines_changed: 14]

Step 6: Bruno notices the agent is about to edit the wrong file
  -> A new [file_read] item appears: "src/auth/legacy-token.service.ts"
  -> Then: [message] "Updating legacy token service to match new refresh logic"
  -> Bruno knows this file is deprecated and should not be touched — it will be deleted next sprint
  -> He clicks the "Steer" button that appears in the toolbar at the top of the activity stream
  [analytics: agent_steer_button_clicked, session_id: hashed, context_pct: 31]

Step 7: Bruno sends an immediate steering message
  -> A text input appears inline at the top of the activity stream with the label "Immediate message — injected now"
  -> He types: "Do not touch legacy-token.service.ts — it is scheduled for deletion. Focus only on token.service.ts and token.store.ts"
  -> He clicks "Send Now"
  -> The agent receives the message mid-turn; a [steering_message] item appears in the stream
  -> The next [message] item from the agent reads: "Understood — skipping legacy file, refocusing on token.service.ts"
  [analytics: agent_steering_message_sent, session_id: hashed, context_pct: 31, message_length: 89]
  [analytics: agent_steering_acknowledged, session_id: hashed]

Step 8: Bruno queues a follow-up message for after the current turn
  -> Satisfied the agent is back on track, Bruno wants to add one more file to refactor
  -> He clicks "Queue message" in the toolbar
  -> A text input appears with the label "Queued message — delivered after current turn completes"
  -> He types: "After finishing token.service.ts and token.store.ts, also refactor src/api/middleware/auth.middleware.ts to use the new token interface"
  -> He clicks "Queue"
  -> A small "1 queued" badge appears in the toolbar, confirming the message is scheduled
  [analytics: agent_queued_message_added, session_id: hashed, queue_depth: 1, message_length: 144]

Step 9: Bruno checks the context window indicator
  -> The session header shows a context bar: "65% used — ~35% remaining"
  -> A tooltip on the bar reads: "Estimated remaining capacity: ~28,000 tokens"
  -> Bruno thinks through whether to add more queued work — the auth middleware file is moderate in size
  -> He decides 35% is enough for the middleware refactor and adds one more queued message:
     "After the middleware, run the full verification gate and signal done"
  -> The "2 queued" badge updates in the toolbar
  [analytics: agent_context_window_viewed, session_id: hashed, context_pct: 65]
  [analytics: agent_queued_message_added, session_id: hashed, queue_depth: 2, message_length: 61]

Step 10: The agent completes its current turn; the first queued message is delivered automatically
  -> The activity stream shows a [turn_complete] marker, then immediately a [queued_message_delivered] item
  -> The agent reads the queued message and begins work on auth.middleware.ts
  -> A new wave of [file_read] and [edit] items flows into the stream
  [analytics: agent_queued_message_delivered, session_id: hashed, queue_depth_remaining: 1]
  [analytics: agent_activity_stream_viewed, session_id: hashed, items_visible: 18]

Step 11: The session completes successfully after processing both queued messages
  -> Bruno receives a desktop notification: "Agent finished: Refactor token handling — Review ready"
  -> The review panel shows: 3 files changed, 0 tests failing, context used: 89%
  -> He opens the diff — the steering message effect is visible: legacy-token.service.ts is untouched
  -> He approves the merge
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 3, context_pct_final: 89]
  [analytics: agent_change_approved, session_id: hashed, files_changed: 3]
  [analytics: agent_worktree_merged, session_id: hashed, project_id: hashed]
  [cross-module: Projects — worktree merged and cleaned up (PM-004)]
```

**Expected Emotion**: Confident control. "I'm not just watching it work — I'm shaping it in real time without breaking its flow."

---

## Scenario S-AG-06: Skills and Custom Tools Management (Bruno)

**Context**: Bruno is setting up the agent ecosystem for his projects for the first time. He wants agents to arrive at a session already knowing his code standards, having the right tools available, and with the right subagents pre-configured — without him having to paste instructions into every session manually.

```
Step 1: Bruno navigates to the Skills section in the Agents tab
  -> The left sidebar of the Agents tab shows sections: Sessions, Queue, Skills, Templates
  -> He clicks "Skills" — a grid of skill cards loads, currently empty with a prompt: "Create your first skill"
  -> He clicks "New Skill"
  [analytics: agent_skills_section_opened, session_id: hashed, skill_count: 0]
  [analytics: agent_skill_create_started]

Step 2: Bruno creates the "Code Review" skill
  -> A skill editor opens with fields: Name, Description, Instructions (rich text), Tools, Subagent Definitions
  -> He fills in:
       Name: "Code Review"
       Description: "Standards and tools for reviewing code in my projects"
       Instructions: (his full code standards document — file naming conventions, DDD rules, testing requirements)
  -> He saves the instructions section
  [analytics: agent_skill_name_set, session_id: hashed, skill_id: hashed]
  [analytics: agent_skill_instructions_saved, session_id: hashed, skill_id: hashed, instruction_length: 2400]

Step 3: Bruno adds tools to the "Code Review" skill
  -> In the Tools section of the skill editor, he clicks "Add Tool"
  -> He sees a list of available tool types: File System, Bash, LemonDo API, HTTP, Custom
  -> He adds two tools:
       Tool 1: "Run Linter" (Bash) — command: "pnpm lint --format=json"
       Tool 2: "Read File" (File System) — scoped to the project worktree root
  -> Each tool has configurable permissions (read-only vs. write, path restrictions)
  [analytics: agent_skill_tool_added, session_id: hashed, skill_id: hashed, tool_type: bash]
  [analytics: agent_skill_tool_added, session_id: hashed, skill_id: hashed, tool_type: file_system]

Step 4: Bruno adds a subagent definition to the "Code Review" skill
  -> In the Subagent Definitions section, he clicks "Add Subagent"
  -> He configures:
       Name: "test-runner"
       Role: "Runs the test suite and reports failures with file/line context"
       Trigger: "When asked to verify a change"
       Budget cap: $0.50 per invocation
  -> The subagent definition is scoped to this skill — it will not appear in the global agent config
  [analytics: agent_skill_subagent_added, session_id: hashed, skill_id: hashed, subagent_name_hashed: hashed]

Step 5: Bruno saves the "Code Review" skill and creates a second skill
  -> He clicks "Save Skill" — the skill card appears in the grid with a version badge: "v1"
  -> He clicks "New Skill" again and creates "Task Management":
       Name: "Task Management"
       Description: "Tools for agents to create and update tasks in LemonDo"
       Instructions: "Use the LemonDo API tools to log findings as tasks rather than interrupting the current session"
       Tools: LemonDo API — POST /api/agent/tasks, PATCH /api/agent/tasks/:id
  [analytics: agent_skill_saved, session_id: hashed, skill_id: hashed, version: 1]
  [analytics: agent_skill_create_started]
  [analytics: agent_skill_saved, session_id: hashed, skill_id: hashed, version: 1]

Step 6: Bruno starts a new agent session and selects which skills to enable
  -> He clicks "New Session" in the Sessions section
  -> The session configuration panel includes a "Skills" section listing all available skills
  -> He toggles on "Code Review" and "Task Management"; leaves others off
  -> The panel shows a preview: "This session will have: 4 tools, 1 subagent, ~2,800 instruction tokens"
  [analytics: agent_session_panel_opened, source: manual]
  [analytics: agent_skill_enabled, session_id: hashed, skill_id: hashed, skill_name_hashed: hashed]
  [analytics: agent_skill_enabled, session_id: hashed, skill_id: hashed, skill_name_hashed: hashed]
  [analytics: agent_session_composed_preview_viewed, session_id: hashed, tool_count: 4, subagent_count: 1, instruction_tokens: 2800]

Step 7: Bruno starts the session and observes the composed configuration in action
  -> The session starts; the activity stream immediately shows:
       [system]  Skills loaded: Code Review v1, Task Management v1
       [system]  Tools available: Run Linter, Read File, LemonDo Task API (2 endpoints)
       [system]  Subagents available: test-runner
  -> The agent begins its work with all the right capabilities pre-loaded
  -> Bruno does not need to paste any instructions into the session
  [analytics: agent_session_started, mode: single, skill_count: 2, has_budget: true]
  [analytics: agent_skills_composed, session_id: hashed, tool_count: 4, subagent_count: 1]

Step 8: Bruno creates a "Git Operations" skill and adds it to his default session template
  -> He creates a third skill: "Git Operations" (commit conventions, branch naming rules, merge checklist)
  -> He navigates to Templates, opens his "Standard Development" template
  -> In the template's Skills section, he toggles "Git Operations" to "Default on"
  -> He saves the template
  -> From this point, every new session using "Standard Development" automatically includes Git Operations
  [analytics: agent_skill_saved, session_id: hashed, skill_id: hashed, version: 1]
  [analytics: agent_template_opened, template_id: hashed]
  [analytics: agent_template_default_skill_added, template_id: hashed, skill_id: hashed]
  [analytics: agent_template_saved, template_id: hashed, default_skill_count: 1]
```

**Expected Emotion**: Composability and ownership. "Now every agent I start arrives already knowing how I work."

---

## Scenario S-AG-07: Memory Pills and Skill Consolidation (Bruno)

**Context**: Bruno has been running Code Review skill agents for a week across several sessions. The agents have been accumulating memory pills — small structured notes about things they learned, mistakes they almost made, and project-specific conventions they discovered. Now Bruno wants to fold that accumulated knowledge back into the skill itself.

```
Step 1: Bruno opens the "Code Review" skill in the Skills section
  -> The skill card shows a badge: "7 unreviewed pills"
  -> He clicks the card to open the skill detail view
  -> The detail view has tabs: Instructions, Tools, Subagents, Memory Pills, History
  [analytics: agent_skill_detail_opened, skill_id: hashed, unreviewed_pill_count: 7]

Step 2: Bruno opens the Memory Pills tab
  -> Seven pill cards are displayed, each showing: category badge, short summary, session source (hashed), timestamp
  -> Category breakdown: 3 "convention", 2 "mistake-avoided", 1 "tip", 1 "finding"
  -> Pills are sorted newest first; the oldest is from 6 days ago
  [analytics: agent_skill_pills_viewed, skill_id: hashed, pill_count: 7, filter: all]

Step 3: Bruno reads through the pills
  -> "Convention (6d ago): Import paths in this project always use @/ aliases — never relative paths with ../
  -> "Mistake-avoided (4d ago): I almost modified legacy-token.service.ts — Bruno confirmed it is deprecated"
  -> "Convention (3d ago): All new domain entities require a corresponding .arbitrary.ts file for property testing"
  -> "Tip (2d ago): Running pnpm lint before pnpm test catches formatting issues faster"
  -> "Finding (1d ago): TypeScript strict mode is enabled — noImplicitAny and strictNullChecks are enforced"
  -> "Mistake-avoided (1d ago): Snapshot tests must be updated with --updateSnapshot before the verification gate"
  -> "Convention (today): Controller files must be co-located with their DTOs in the presentation/ folder"
  -> Bruno recognizes all of these as genuine learnings the agents discovered on their own
  [analytics: agent_skill_pill_read, skill_id: hashed, pill_id: hashed]
  [analytics: agent_skill_pill_read, skill_id: hashed, pill_count_read: 7]

Step 4: Bruno clicks "Consolidate" to fold the pills into the skill instructions
  -> A confirmation dialog appears: "This will start a Skill Improvement agent session.
     The agent will read all 7 memory pills and update the Code Review skill's instructions.
     Estimated cost: $0.30 — $0.60. Proceed?"
  -> Bruno clicks "Start Consolidation"
  [analytics: agent_skill_consolidation_initiated, skill_id: hashed, pill_count: 7]
  [analytics: agent_session_started, mode: skill_consolidation, skill_id: hashed, has_budget: true]

Step 5: The consolidation agent session starts with the "Skill Improvement" meta-skill
  -> A special session card appears in the Sessions list: "Skill Consolidation — Code Review — Running"
  -> The session's activity stream shows the agent reading each pill in sequence
  -> [file_read] Code Review v1 instructions
  -> [message] "Analyzing 7 pills — grouping by theme: import conventions (2), TypeScript rules (2), test workflow (2), file organization (1)"
  [analytics: agent_skill_consolidation_session_viewed, skill_id: hashed, session_id: hashed]
  [analytics: agent_activity_stream_viewed, session_id: hashed, items_visible: 5]

Step 6: The consolidation agent drafts updates to the skill instructions
  -> The activity stream shows [edit] items as the agent writes changes to a draft of the skill instructions:
       Added section: "Import path conventions — always use @/ aliases"
       Added section: "TypeScript strict mode — noImplicitAny and strictNullChecks active"
       Added section: "Test workflow — run pnpm lint before pnpm test; update snapshots before verification gate"
       Updated section: "File organization — DTOs co-located with controllers in presentation/"
       Added note: "legacy-token.service.ts is deprecated — do not modify"
  -> The agent marks all 7 pills as "consolidated"
  [analytics: agent_skill_draft_updated, session_id: hashed, skill_id: hashed, sections_added: 4, sections_updated: 1]
  [analytics: agent_skill_pills_consolidated, session_id: hashed, skill_id: hashed, pill_count: 7]

Step 7: The consolidation session completes; Bruno reviews the proposed changes
  -> A notification arrives: "Skill Consolidation complete — Code Review — Review ready"
  -> Bruno opens the review panel: it shows a diff of the skill's instructions (before vs. after)
  -> New sections are highlighted in green; one section has an in-line edit (updated wording)
  -> The estimated token cost for the new instructions is shown: "Instructions grew from 2,400 to 3,150 tokens"
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_skill_consolidation_review_opened, skill_id: hashed, instruction_tokens_before: 2400, instruction_tokens_after: 3150]

Step 8: Bruno approves the consolidation
  -> He reads through the diff — everything looks correct and well-organized
  -> He clicks "Apply and Increment Version"
  -> The skill updates to v2; all 7 pills are marked "Consolidated — included in v2"
  -> The skill card in the grid now shows "v2" and "0 unreviewed pills"
  -> Future sessions using the Code Review skill will automatically load v2
  [analytics: agent_skill_consolidation_approved, skill_id: hashed, from_version: 1, to_version: 2]
  [analytics: agent_skill_version_incremented, skill_id: hashed, version: 2, pills_consolidated: 7]

Step 9: Bruno checks the skill history to see the full version trail
  -> He opens the History tab on the skill detail view
  -> It shows: v1 (created manually), v2 (consolidated from 7 pills — 2026-02-18)
  -> Each version entry links to the consolidation session for full traceability
  [analytics: agent_skill_history_viewed, skill_id: hashed, version_count: 2]
```

**Expected Emotion**: Compounding value. "The agents are teaching themselves how to work better in my codebase. It keeps getting smarter."

---

## Scenario S-AG-08: Auto-Continue Mode and Voluntary Handoff (Bruno)

**Context**: Bruno is experimenting with two advanced session behaviors — auto-continue (where the system keeps pushing the agent until a quality bar is met) and voluntary handoff (where the agent itself decides to start fresh rather than continue in a polluted context). Both reduce the need for Bruno to babysit sessions.

```
Step 1: Bruno configures an agent session with auto-continue mode enabled
  -> He opens the "New Session" panel and selects the task: "Implement user profile settings page with full test coverage"
  -> In the Advanced section he toggles "Auto-continue" on
  -> A sub-panel expands: "Validation criteria — session will auto-continue until all criteria pass"
  -> He configures two criteria:
       Criterion 1: All tests passing (pnpm test exits 0)
       Criterion 2: Coverage threshold — 80% line coverage on changed files
  -> Budget: $6.00, auto-continue budget extension: up to $2.00 additional per retry
  [analytics: agent_session_panel_opened, source: manual]
  [analytics: agent_auto_continue_enabled, session_id: hashed]
  [analytics: agent_validation_criteria_configured, session_id: hashed, criteria_count: 2, criteria_types: tests_passing|coverage_threshold]
  [analytics: agent_budget_configured, per_session: 6.00, auto_continue_extension: 2.00]

Step 2: The agent works through the profile settings page implementation
  -> Bruno navigates away; the session runs autonomously
  -> The agent implements the ProfileSettings component, the API endpoint, and writes unit tests
  -> After 22 minutes the agent signals completion and LemonDo runs the validation criteria
  [analytics: agent_session_running, session_id: hashed, elapsed_minutes: 22]
  [analytics: agent_validation_gate_triggered, session_id: hashed, criteria_count: 2]

Step 3: Validation fails — coverage is at 72%, below the 80% threshold
  -> LemonDo evaluates: tests passing — yes (431/431); coverage — 72% (threshold: 80%) — FAIL
  -> Instead of notifying Bruno, auto-continue mode kicks in automatically
  -> A "Continue" message is injected into the session: "Coverage at 72% — below the 80% threshold.
     Write additional tests to bring coverage above 80% before signaling done again."
  -> The session status on the dashboard updates to: "Auto-continuing (attempt 2 of 3)"
  [analytics: agent_validation_gate_result, session_id: hashed, passed: false, criteria_failed: coverage_threshold, coverage_pct: 72]
  [analytics: agent_auto_continue_triggered, session_id: hashed, attempt: 2, reason: coverage_below_threshold]

Step 4: The agent writes more tests on the second attempt and coverage reaches 83%
  -> The agent adds 11 more tests covering edge cases in the preferences form validation
  -> LemonDo re-runs the validation criteria: tests passing — yes (442/442); coverage — 83% — PASS
  -> Both criteria pass; the session status changes to "Validation Passed — awaiting review"
  -> Bruno receives a desktop notification: "Agent finished (2 attempts): Profile settings page — Review ready"
  [analytics: agent_validation_gate_result, session_id: hashed, passed: true, coverage_pct: 83, attempt: 2]
  [analytics: agent_auto_continue_resolved, session_id: hashed, total_attempts: 2]
  [analytics: agent_completion_notification_clicked, session_id: hashed]

Step 5: Bruno reviews and approves the session output
  -> The review panel notes: "Auto-continued once — coverage improved from 72% to 83%"
  -> Files changed: 6, Tests added: 31 total (20 first attempt + 11 on retry)
  -> He approves the merge; the task moves to Done
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 6, tests_added: 31, auto_continued: true]
  [analytics: agent_change_approved, session_id: hashed, files_changed: 6]
  [analytics: agent_worktree_merged, session_id: hashed, project_id: hashed]
  [analytics: task_completed, source: agent_merge, task_id: hashed]
  [cross-module: Tasks — task moved to Done (task_completed event)]
  [cross-module: Projects — worktree merged and cleaned up (PM-004)]

Step 6: Bruno starts a new session for a larger task — without auto-continue
  -> He configures a new session: task "Refactor the entire authentication module"
  -> He does not enable auto-continue for this one — it's too open-ended for validation criteria
  -> Budget: $8.00. He starts the session and returns to his email
  [analytics: agent_session_panel_opened, source: manual]
  [analytics: agent_session_started, mode: single, has_budget: true, project_id: hashed, auto_continue: false]
  [analytics: worktree_created, count: 1, source: agent_single, project_id: hashed]

Step 7: The agent completes the first half of the refactor and decides to voluntarily hand off
  -> The agent has replaced the token storage mechanism (the first half of the refactor)
  -> Context used: 55% — plenty remaining in absolute terms
  -> However, the agent determines that its context is now heavily saturated with the completed
     implementation details, making it harder to reason clearly about the second half
  -> It calls: POST /api/agent/handoff with a structured handoff document:
       - What was done: token storage replaced, 8 files modified, all tests still green
       - What remains: session middleware, cookie handling, frontend token refresh logic
       - Key decisions made: chose HttpOnly cookies over localStorage, kept existing interface
       - Files to focus on next: listed with current state notes
  [analytics: agent_voluntary_handoff_initiated, session_id: hashed, context_pct: 55, reason: context_pollution]
  [analytics: agent_handoff_document_created, session_id: hashed, handoff_id: hashed]
  [cross-module: Agent API (AG-036) — agent calls handoff endpoint to record structured summary]

Step 8: LemonDo receives the voluntary handoff and chains a new session
  -> The original session card in the dashboard updates to: "Handed off (voluntary) — 55% context used"
  -> A chain indicator appears linking it to a new session card: "Auth module refactor (cont.) — Starting"
  -> The new session starts with the handoff document pre-loaded as its first context item
  -> The activity stream of the new session shows:
       [system]  Resuming from voluntary handoff — session chain #2
       [file_read] handoff-doc-session-001.md
       [message]  "Picking up where the previous session left off — starting with session middleware"
  [analytics: agent_session_chained, from_session: hashed, to_session: hashed, handoff_type: voluntary]
  [analytics: agent_session_started, mode: chained, chain_position: 2, has_budget: true]
  [analytics: agent_handoff_document_loaded, session_id: hashed, handoff_id: hashed]

Step 9: Bruno sees the session chain in the dashboard and inspects the handoff document
  -> He navigates to the Agents dashboard and notices a chain view: two session cards linked with an arrow
  -> Chain header: "Auth module refactor — Session chain (2 sessions)"
  -> He clicks the chain connector (the arrow between the cards) to view the handoff document
  -> The document shows the structured summary the first agent wrote — what was done, what remains, key decisions
  -> Bruno reads it and thinks it's more thorough than most human handoff notes
  [analytics: agent_session_chain_viewed, chain_id: hashed, session_count: 2]
  [analytics: agent_handoff_document_viewed, handoff_id: hashed, source: chain_connector]

Step 10: The second session completes the refactor; Bruno reviews the full chain output
  -> A notification arrives: "Agent finished: Auth module refactor (cont.) — Review ready"
  -> The review panel spans both sessions: total files changed (14), total tests added (27), total cost ($6.83)
  -> A timeline shows the two sessions with the handoff document as the bridge between them
  -> Bruno approves the merge; both session cards update to "Merged"
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 14, tests_added: 27, session_chain_length: 2]
  [analytics: agent_change_approved, session_id: hashed, files_changed: 14]
  [analytics: agent_worktree_merged, session_id: hashed, project_id: hashed]
  [analytics: task_completed, source: agent_merge, task_id: hashed]
  [cross-module: Tasks — task moved to Done]
  [cross-module: Projects — worktree merged and cleaned up (PM-004)]
```

**Expected Emotion**: Sophisticated autonomy. "It knew when to start fresh without me telling it to. That's the level of judgment I want from an AI agent."

---

## Scenario S-AG-09: Interactive Agent Feedback — Questions, Progress, and Plan Review (Bruno)

**Context**: Bruno kicks off a complex agent session to design and implement a new notification system. He wants visibility into every stage of the agent's process — its plan before it writes a line of code, its internal progress as it works, and any questions it needs answered along the way — without having to read through raw logs.

```
Step 1: Bruno starts a new session from the Agents tab
  -> He clicks "New Session," selects the task "Design and implement new notification system," and sets a $7.00 budget
  -> He enables the "Plan before implementing" option in the session configuration panel
  -> He clicks "Start"
  [analytics: agent_session_panel_opened, source: manual]
  [analytics: agent_plan_mode_required, session_id: hashed]
  [analytics: agent_session_started, mode: single, has_budget: true, plan_mode_required: true, project_id: hashed]

Step 2: The agent enters plan mode; a plan document panel appears in the session UI
  -> The session detail view splits: the activity stream is on the left, a new "Plan" panel opens on the right
  -> The Plan panel header reads: "Agent planning — reviewing requirements and designing approach"
  -> A loading indicator pulses in the Plan panel as the agent reads the codebase and formulates its plan
  -> The activity stream shows [file_read] items as the agent surveys the existing notification infrastructure
  [analytics: agent_plan_mode_entered, session_id: hashed]
  [analytics: agent_plan_panel_opened, session_id: hashed]
  [analytics: agent_activity_stream_viewed, session_id: hashed, items_visible: 6]

Step 3: The plan document populates as the agent writes it
  -> The Plan panel fills in with structured sections as the agent composes them in real time:
       ## Approach
       Use SignalR for real-time delivery to avoid polling overhead.
       ## Components
       1. NotificationHub (SignalR)  2. NotificationService  3. NotificationPreferences (per-project)
       ## Database changes
       Add Notifications table, NotificationPreferences table (FK to Project)
       ## Sequence
       Step 1: DB migration  Step 2: Domain entities  Step 3: Hub + service  Step 4: Frontend hook  Step 5: E2E tests
  -> Bruno reads the plan as it forms and notices the agent has defaulted to polling, not SignalR
  [analytics: agent_plan_document_viewed, session_id: hashed, sections_visible: 4]

Step 4: The agent signals it has finished planning and presents the plan for approval
  -> The Plan panel header changes to: "Plan ready — awaiting your approval"
  -> Two buttons appear at the bottom of the Plan panel: "Approve Plan" and "Request Changes"
  -> The session status on the dashboard card updates to: "WaitingForApproval"
  -> Bruno does not yet click Approve — he wants to request a change
  [analytics: agent_plan_presented_for_approval, session_id: hashed]
  [analytics: agent_session_status_changed, session_id: hashed, status: WaitingForApproval]

Step 5: Bruno requests a change to the plan
  -> He clicks "Request Changes" — a text input appears below the plan document
  -> He types: "Use SignalR for real-time delivery instead of polling. Notification preferences should be per-project, not global."
  -> He clicks "Send Revision Request"
  -> The Plan panel header returns to: "Agent revising plan..."
  -> The agent updates the plan document; the Approach section rewrites to reference SignalR
  [analytics: agent_plan_revision_requested, session_id: hashed, message_length: 102]
  [analytics: agent_plan_document_revised, session_id: hashed, sections_updated: 2]

Step 6: Bruno reviews the revised plan and approves it
  -> The revised plan now reads: "Use SignalR hub for real-time push delivery. NotificationPreferences scoped per Project (FK: project_id)."
  -> The plan accurately reflects both of his requests
  -> He clicks "Approve Plan"
  -> The Plan panel collapses into a summary strip at the top of the session view: "Plan approved — implementing"
  -> The agent exits plan mode and begins implementation
  [analytics: agent_plan_approved, session_id: hashed, revision_count: 1]
  [analytics: agent_plan_mode_exited, session_id: hashed, outcome: approved]
  [analytics: agent_session_status_changed, session_id: hashed, status: Running]

Step 7: The agent uses TodoWrite; a progress tracker sidebar appears
  -> The session detail view now shows a third panel on the far right: "Progress"
  -> The Progress panel lists 5 tasks the agent has written internally:
       [done]        DB migration — notifications + preferences tables
       [in_progress] Domain entities: Notification, NotificationPreferences
       [pending]     SignalR hub + NotificationService
       [pending]     Frontend useNotifications hook
       [pending]     E2E tests
  -> Bruno can see exactly where the agent is without reading the activity stream
  [analytics: agent_todo_write_rendered, session_id: hashed, task_count: 5, done: 1, in_progress: 1, pending: 3]
  [analytics: agent_progress_panel_viewed, session_id: hashed]

Step 8: The progress tracker updates in real time as tasks complete
  -> A few minutes pass; the Progress panel refreshes:
       [done]        DB migration — notifications + preferences tables
       [done]        Domain entities: Notification, NotificationPreferences
       [in_progress] SignalR hub + NotificationService
       [pending]     Frontend useNotifications hook
       [pending]     E2E tests
  -> Bruno glances at it and sees the agent is on task 3 of 5 — no need to read logs
  [analytics: agent_todo_progress_updated, session_id: hashed, done: 2, in_progress: 1, pending: 2]

Step 9: The agent calls AskUserQuestion; the session pauses and a question card appears
  -> The session status changes to: "WaitingForInput"
  -> The activity stream shows a new item type: [question]
  -> A structured card renders prominently above the activity stream:
       Question: "Should notification preferences be configurable per-project, or should there be
                  one global preference set that applies to all projects?"
       Options:  (A) Per-project  (B) Global  (C) Let me decide — I'll type a custom answer
  -> The session dashboard card shows a pulsing amber badge: "Waiting for your input"
  [analytics: agent_ask_user_question_rendered, session_id: hashed]
  [analytics: agent_session_status_changed, session_id: hashed, status: WaitingForInput]
  [analytics: agent_question_card_viewed, session_id: hashed, has_options: true, option_count: 3]

Step 10: Bruno answers the question and the session resumes
  -> Bruno clicks option "(A) Per-project"
  -> The question card collapses and shows: "You answered: Per-project — session resuming"
  -> The session status returns to "Running"
  -> The activity stream shows a [user_answer] item: "Per-project" followed immediately by the agent's next [message]: "Confirmed — scoping NotificationPreferences to project_id as already planned."
  [analytics: agent_question_answered, session_id: hashed, answer_type: option_selected, option_index: 0]
  [analytics: agent_session_status_changed, session_id: hashed, status: Running]
  [analytics: agent_session_resumed_after_input, session_id: hashed]

Step 11: The agent completes all 5 tasks; the progress tracker shows all done
  -> The Progress panel updates to:
       [done]  DB migration — notifications + preferences tables
       [done]  Domain entities: Notification, NotificationPreferences
       [done]  SignalR hub + NotificationService
       [done]  Frontend useNotifications hook
       [done]  E2E tests
  -> A desktop notification arrives: "Agent finished: Implement notification system — Review ready"
  -> Bruno clicks through; files changed: 11, tests added: 34, all passing, cost: $5.60 of $7.00
  [analytics: agent_todo_progress_updated, session_id: hashed, done: 5, in_progress: 0, pending: 0]
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 11, tests_added: 34]
  [analytics: agent_budget_tracker_viewed, spent: 5.60, cap: 7.00]
```

**Expected Emotion**: Informed collaboration. "I saw the plan before a single line was written, caught the SignalR detail early, answered one question, and watched it tick through all five tasks. That's exactly the level of visibility I want."

---

## Scenario S-AG-10: Dynamic Session Configuration — Model Selection and Skill Hot-Loading (Bruno)

**Context**: Bruno is running two very different agent sessions today — one trivial formatting task and one complex architecture session. He wants to right-size the AI model for each to manage cost and quality. Midway through the architecture session, he realizes it needs specialized tools he didn't configure at the start, and he wants to add them without canceling the session.

```
Step 1: Bruno starts a session for a quick code formatting task
  -> He clicks "New Session" and selects the task "Apply consistent import ordering across all files"
  -> The session configuration panel shows the model selector: "Claude Sonnet (default from template)"
  -> Beside the model name a label reads: "Recommended for: standard dev tasks — balanced cost/quality"
  [analytics: agent_session_panel_opened, source: manual]
  [analytics: agent_model_selector_viewed, session_id: hashed, default_model: claude_sonnet, source: template_default]

Step 2: Bruno changes the model to Claude Haiku for this trivial task
  -> He clicks the model selector dropdown; it shows three options with cost/capability information:
       Claude Opus   — "Best reasoning, highest cost — ~$0.80/1k tokens"
       Claude Sonnet — "Balanced quality and cost — ~$0.18/1k tokens" (current template default)
       Claude Haiku  — "Fast, lowest cost, great for simple tasks — ~$0.03/1k tokens"
  -> He selects "Claude Haiku" — the model label in the panel updates immediately
  -> A note appears: "This overrides the template default for this session only"
  [analytics: agent_model_changed, session_id: hashed, from_model: claude_sonnet, to_model: claude_haiku, reason_context: simple_task]
  [analytics: agent_model_cost_info_viewed, session_id: hashed, models_shown: 3]

Step 3: Bruno starts the formatting session; it completes quickly at low cost
  -> He clicks "Start" — the session runs and finishes in 4 minutes
  -> Total cost: $0.12
  -> A desktop notification arrives: "Agent finished: Apply consistent import ordering — Review ready"
  -> He approves the diff — 28 files updated, all linting passes
  [analytics: agent_session_started, mode: single, model: claude_haiku, has_budget: true, project_id: hashed]
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 28, cost: 0.12]
  [analytics: agent_change_approved, session_id: hashed, files_changed: 28]

Step 4: Bruno starts a complex architecture session with Claude Opus
  -> He opens "New Session" and selects the task "Design the Projects module bounded context — entities, repositories, use cases"
  -> He selects "Claude Opus" from the model selector — the cost/capability note reads: "Best for deep design work, complex reasoning"
  -> He enables the "Code Review" skill; leaves "Database Migration" skill off (didn't think he'd need it)
  -> Budget: $10.00. He clicks "Start" and returns to other work
  [analytics: agent_session_panel_opened, source: manual]
  [analytics: agent_model_changed, session_id: hashed, from_model: claude_sonnet, to_model: claude_opus, reason_context: complex_architecture]
  [analytics: agent_skill_enabled, session_id: hashed, skill_id: hashed, skill_name_hashed: hashed]
  [analytics: agent_session_started, mode: single, model: claude_opus, has_budget: true, skill_count: 1, project_id: hashed]

Step 5: The session enters an Idle state between turns; Bruno checks in
  -> 25 minutes later Bruno opens the Agents dashboard
  -> The session card shows: "Designing Projects module — Idle — 25m elapsed — 41% context — $3.20 used"
  -> He clicks into the session detail to read the activity stream
  -> The agent has drafted the bounded context diagram, entity definitions, and is pausing before writing the repository interfaces
  [analytics: agent_dashboard_viewed, active_sessions: 1, view: grid]
  [analytics: agent_session_detail_viewed, session_id: hashed, context_pct: 41, elapsed_minutes: 25]

Step 6: Bruno realizes the session needs the "Database Migration" skill
  -> Reading the agent's notes in the stream, Bruno sees it plans to write migration scaffolding next
  -> He knows the "Database Migration" skill has specialized tools for schema validation and migration naming conventions
  -> He did not enable it at session start — but the session is currently Idle
  -> He opens the session's "Skills" panel (a gear icon in the session detail toolbar)
  -> The Skills panel shows: enabled skills (Code Review — on), available skills (Database Migration — off, Git Operations — off)
  [analytics: agent_session_skills_panel_opened, session_id: hashed, session_status: idle]
  [analytics: agent_skill_panel_available_skills_viewed, session_id: hashed, available_count: 2]

Step 7: Bruno hot-loads the "Database Migration" skill into the running session
  -> He clicks the toggle next to "Database Migration" to enable it
  -> A confirmation prompt appears: "Enabling a new skill requires a session reload. The agent will reinitialize with the new configuration. Current context and progress are preserved. Continue?"
  -> He clicks "Reload with New Skill"
  -> A "Reloading session configuration..." banner appears across the session detail panel
  [analytics: agent_skill_hot_load_initiated, session_id: hashed, skill_id: hashed, session_status: idle]
  [analytics: agent_session_reload_confirmed, session_id: hashed]
  [analytics: agent_session_reload_indicator_shown, session_id: hashed]

Step 8: The sidecar reinitializes with the merged configuration
  -> The reload takes approximately 3 seconds; the banner disappears
  -> The activity stream appends a new [system] item:
       [system]  Session reloaded — Skills: Code Review v2, Database Migration v1
       [system]  Tools available: Run Linter, Read File, Schema Validator, Migration Namer (4 tools total)
       [system]  Subagents available: test-runner, migration-checker
  -> The session status returns to "Idle — ready to continue"
  -> No prior context was lost; the agent can still see its previous design work
  [analytics: agent_session_reloaded, session_id: hashed, skill_count: 2, tool_count: 4, subagent_count: 2]
  [analytics: agent_skills_composed, session_id: hashed, skill_count: 2, tool_count: 4]

Step 9: Bruno sends a follow-up message referencing the newly available migration tools
  -> He types into the session input: "Go ahead and scaffold the EF Core migration for the Projects bounded context. Use the migration naming conventions from the Database Migration skill."
  -> He sends the message — the session transitions from Idle to Running
  -> The activity stream shows the agent picking up: [message] "Starting migration scaffolding — applying Database Migration naming conventions."
  -> A [tool_call] item appears: "Schema Validator — validating Projects entity graph before generating migration"
  -> The new tool is working seamlessly — the agent had no awareness gap from the reload
  [analytics: agent_follow_up_message_sent, session_id: hashed, triggered_skill: database_migration]
  [analytics: agent_session_status_changed, session_id: hashed, status: Running]
  [analytics: agent_tool_call_expanded, session_id: hashed, tool_type: schema_validator, had_preview: true]

Step 10: The session completes; Bruno reviews the output
  -> A notification arrives: "Agent finished: Design Projects module bounded context — Review ready"
  -> Review panel: files changed: 9, tests added: 22, migration files created: 2, all passing, cost: $8.40 of $10.00
  -> The review panel notes which tools were used: "Schema Validator (3 calls), Migration Namer (2 calls)" — visible evidence the hot-loaded skill was effective
  -> Bruno approves the merge
  [analytics: agent_completion_notification_clicked, session_id: hashed]
  [analytics: agent_review_panel_opened, session_id: hashed, files_changed: 9, tests_added: 22, cost: 8.40, hot_loaded_skills: 1]
  [analytics: agent_change_approved, session_id: hashed, files_changed: 9]
  [analytics: agent_worktree_merged, session_id: hashed, project_id: hashed]
  [analytics: task_completed, source: agent_merge, task_id: hashed]
  [cross-module: Projects — worktree merged and cleaned up (PM-004)]
```

**Expected Emotion**: Adaptive control. "I picked the right model for each job, saved money on the trivial task, and added the skill I forgot without losing a single minute of work on the complex session."

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
| AG-016 | Agent session templates (predefined objectives, context, constraints) | S-AG-02 (Step 7), S-AG-06 (Step 8) |
| AG-017 | Claude Agent SDK integration for custom agent behaviors | S-AG-02 (Step 2) — scheduled automation implies SDK-level agent |
| AG-018 | Session history and audit trail | S-AG-01 (Step 10), S-AG-02 (Step 2), S-AG-03 (Step 9) |
| AG-019 | Approve/reject agent-proposed changes before merge | S-AG-01 (Steps 8-9), S-AG-03 (Step 9), S-AG-04 (Step 8) |
| AG-020 | Agent notification on completion/failure | S-AG-01 (Step 8), S-AG-03 (Steps 6, 8), S-AG-04 (Step 7) |
| AG-021 | Real-time structured activity stream (skimmable tool calls, messages, subagent events) | S-AG-05 (Steps 2, 10) |
| AG-022 | Expandable tool call detail with per-tool-type UI customization | S-AG-05 (Steps 3, 4, 5) |
| AG-023 | Subagent compact view (status, elapsed, context window, last action, expandable) | S-AG-05 (Step 1) — session card; S-AG-07 (Step 5) — consolidation session card |
| AG-024 | Context window usage indicator for informed decision-making | S-AG-05 (Step 9) |
| AG-025 | Send immediate steering message to running agent (interrupt + inject) | S-AG-05 (Steps 6, 7) |
| AG-026 | Send queued follow-up message for after current turn | S-AG-05 (Steps 8, 9, 10) |
| AG-027 | Create and manage agent skills (name, instructions, tools, subagent definitions) | S-AG-06 (Steps 1, 2, 3, 4, 5) |
| AG-028 | Enable/disable skills per agent session | S-AG-06 (Step 6) |
| AG-029 | Skills compose into effective session config (instructions + tools + subagents merged) | S-AG-06 (Steps 6, 7) |
| AG-030 | Default skills on templates (auto-enabled for all sessions using that template) | S-AG-06 (Step 8) |
| AG-031 | Subagent definitions within skills (scoped, not global) | S-AG-06 (Step 4) |
| AG-032 | Agents record memory pills during skill usage (content, category) | S-AG-07 (Steps 2, 3) — pills recorded by previous sessions, reviewed by Bruno |
| AG-033 | View memory pills per skill (filterable by category, status) | S-AG-07 (Steps 2, 3) |
| AG-034 | Consolidate memory pills via dedicated agent session | S-AG-07 (Steps 4, 5, 6, 7, 8) |
| AG-035 | Skill versioning (incremented on consolidation) | S-AG-07 (Step 8) |
| AG-036 | Agent API endpoint for recording memory pills | S-AG-07 (Step 3) — pills created by agents via API during prior sessions |
| AG-037 | Auto-continue mode with deliverable validation | S-AG-08 (Steps 1, 2, 3, 4) |
| AG-038 | Configurable validation criteria (tests passing, coverage threshold, custom) | S-AG-08 (Step 1) |
| AG-039 | Agent-initiated voluntary handoff (not just context exhaustion) | S-AG-08 (Steps 7, 8) |
| AG-040 | Session chain view showing handoff documents between sessions | S-AG-08 (Steps 8, 9, 10) |
| AG-041 | AskUserQuestion tool call renders as structured interactive prompt in session UI | S-AG-09 (Step 9) |
| AG-042 | Session transitions to WaitingForInput when AskUserQuestion is called; resumes on user answer | S-AG-09 (Steps 9, 10) |
| AG-043 | TodoWrite creates a visible progress tracker sidebar in session UI | S-AG-09 (Steps 7, 8, 11) |
| AG-044 | EnterPlanMode creates a reviewable plan document visible in session UI | S-AG-09 (Steps 2, 3, 4) |
| AG-045 | ExitPlanMode optionally requires user approval before agent proceeds to implementation | S-AG-09 (Steps 4, 5, 6) |
| AG-046 | Select AI model per agent session at start time (override template default) | S-AG-10 (Steps 1, 2, 4) |
| AG-047 | Model selection shows cost/capability tradeoff info to inform choice | S-AG-10 (Step 2) |
| AG-048 | Hot-load skills into Idle or Interrupted sessions without restart | S-AG-10 (Steps 6, 7, 8) |
| AG-049 | Session reload indicator in UI during skill hot-loading | S-AG-10 (Step 7) |
| AG-050 | Sidecar reinitializes with merged config after skill hot-load | S-AG-10 (Step 8) |
