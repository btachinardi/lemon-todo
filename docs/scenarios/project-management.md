# Project Management Scenarios

> **Source**: Extracted from docs/PRD.2.draft.md §4.4
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## Scenario S-PM-01: Adding a New Repository (Bruno)

**Context**: Bruno has just started a new client project. The repo exists on his machine at `~/dev/acme-platform`. He opens LemonDo to register it so he can manage tasks, worktrees, and deployments from one place rather than jumping between the terminal and GitHub.

```
Step 1: Bruno opens the Projects section of LemonDo
  -> The Projects view loads, showing two existing registered projects
  -> A prominent "Add Project" button is visible in the top-right
  -> Bruno clicks "Add Project"
  [analytics: projects_view_opened, existing_project_count: 2]

Step 2: Bruno selects the local folder path
  -> A folder-picker dialog opens (native OS file picker)
  -> He navigates to ~/dev/acme-platform and confirms
  -> LemonDo shows a "Scanning repository..." progress indicator
  [analytics: project_registration_started, method: folder_picker]

Step 3: LemonDo scans the repository
  -> The scan completes in ~2 seconds
  -> LemonDo detects: Git repo, pnpm monorepo, .NET 10 backend, React frontend
  -> It finds README.md, CLAUDE.md, and docs/ folder with 8 markdown files
  -> It reads the default branch (main), last commit message, and total branch count (7)
  [analytics: project_scan_completed, detected_stack: pnpm_dotnet_react, doc_files_found: 8, branch_count: 7]

Step 4: Bruno reviews the auto-detected project card preview
  -> A preview card appears: name "acme-platform", description pulled from README first paragraph
  -> Tech stack tags shown: ".NET 10", "React 19", "pnpm", "TypeScript"
  -> Documentation links listed: README.md, CLAUDE.md, docs/architecture.md, docs/deployment.md
  -> Branch info: default branch "main", 7 branches, last commit "fix(auth): token refresh on expiry" 3 hours ago
  -> Bruno sees the field is editable — he renames it "Acme Platform" for readability
  [analytics: project_preview_reviewed, name_edited: true]

Step 5: Bruno confirms and saves the project
  -> He clicks "Add Project"
  -> The new project card appears in the Projects grid with a subtle entrance animation
  -> The card shows: name, tech stack tags, branch count, and last commit time
  -> A "Get started" prompt suggests: "Link tasks to this project" and "Create your first worktree"
  [analytics: project_registered, tech_stack_tags: 4, has_docs: true, has_claude_md: true]

Step 6: Bruno opens the project to explore its documentation
  -> He clicks the project card
  -> The project detail view opens, with tabs: Overview | Worktrees | Dev Servers | Tasks | Docs | Settings
  -> The Docs tab lists all detected markdown files, rendered in-app
  -> Bruno reads CLAUDE.md directly inside LemonDo without opening a separate editor
  [analytics: project_detail_opened, initial_tab: overview]
  [analytics: project_doc_viewed, file: CLAUDE.md]
```

**Expected Emotion**: Impressed by the scan, immediately productive. "It found everything — I didn't have to type a single thing."

---

## Scenario S-PM-02: Creating a Worktree for a Feature Branch (Bruno)

**Context**: Bruno is about to start work on a new payments feature for the acme-platform project. He wants to keep this work isolated from main while he works on two other features in parallel. He opens the project in LemonDo to create the worktree from the UI.

```
Step 1: Bruno opens the acme-platform project and navigates to the Worktrees tab
  -> The Worktrees tab shows the current state: 1 worktree (main, clean, up-to-date)
  -> A "+ New Worktree" button is prominent
  -> Bruno clicks it
  [analytics: worktrees_tab_opened, project_id: hashed, existing_worktree_count: 1]

Step 2: Bruno fills in the worktree details
  -> A compact form slides in: Branch Name, Base Branch (dropdown), Worktree Path (auto-suggested)
  -> He types "feature/payments-v2" as the branch name
  -> Base branch defaults to "main" — he leaves it
  -> Worktree path auto-suggested as ~/dev/acme-platform-payments-v2 — he accepts it
  [analytics: worktree_form_started]

Step 3: LemonDo creates the worktree
  -> He clicks "Create Worktree"
  -> A progress bar shows: "Running git worktree add..." → "Checking out branch..." → "Done"
  -> The entire operation takes about 3 seconds
  [analytics: worktree_created, base_branch: main, new_branch: feature_branch]

Step 4: The new worktree appears as a row in the Worktrees tab
  -> Row shows: branch name "feature/payments-v2", path ~/dev/acme-platform-payments-v2, status "Clean", ahead/behind "0/0"
  -> A "Start Dev Server" button sits inline on the row
  -> A "Open in Terminal" shortcut is also visible
  -> Bruno also sees his existing worktrees: "feature/auth-refresh" (1 file modified) and "main" (clean)
  [analytics: worktree_status_viewed, worktree_count: 3]

Step 5: Bruno installs dependencies in the new worktree
  -> He clicks the "..." menu on the worktree row and selects "Install Dependencies"
  -> LemonDo detects pnpm and runs `pnpm install` in the worktree directory
  -> A collapsible log panel shows the install output in real-time
  -> After ~45 seconds: "Dependencies installed. 0 packages added." (workspace node_modules reused)
  [analytics: deps_install_triggered, package_manager: pnpm, project_id: hashed]
  [analytics: deps_install_completed, duration_seconds: 45, packages_added: 0]

Step 6: Bruno opens the project's git log to confirm the branch was created correctly
  -> He clicks the "Git Log" icon in the project toolbar
  -> A visual branch graph renders inline: main → feature/payments-v2 branch point visible
  -> He confirms the branch is at the correct commit
  -> He also sees "feature/auth-refresh" as a parallel branch 4 commits ahead of main
  [analytics: git_log_viewed, project_id: hashed, branch_count_visible: 3]

Step 7: Bruno notes a collaborator who will need access to this branch
  -> He sees a "People" section on the worktree detail — currently empty
  -> He types the collaborator's name, links them to the project as "Contributor"
  -> Cross-reference: the collaborator's People profile will now show acme-platform as a linked project
  [analytics: person_linked_to_project, role: contributor]
  [cross-module: people — PP-009 (link people to projects)]
```

**Expected Emotion**: Confident and organized. "Three parallel features, all isolated, all visible in one screen."

---

## Scenario S-PM-03: Starting a Dev Server and Creating an ngrok Tunnel (Bruno)

**Context**: Bruno has finished building the payments feature and needs to show it to a client over a video call in 10 minutes. The client can't access his local network. He needs to start the dev server and share a public URL — fast.

```
Step 1: Bruno opens the acme-platform project and goes to the Dev Servers tab
  -> The Dev Servers tab shows no active servers for this project
  -> A "+ Start Dev Server" button is visible
  -> Bruno clicks it
  [analytics: dev_servers_tab_opened, project_id: hashed, active_server_count: 0]

Step 2: Bruno configures the dev server
  -> A form appears: Command (auto-detected "pnpm dev"), Port (auto-detected 5173), Working Directory (defaults to worktree root)
  -> The working directory dropdown lets him select which worktree: he picks "feature/payments-v2"
  -> He leaves all other settings at their defaults
  [analytics: dev_server_form_started, command_auto_detected: true, port_auto_detected: true]

Step 3: LemonDo starts the dev server process
  -> He clicks "Start"
  -> A terminal output panel streams the startup logs in real-time
  -> After ~8 seconds the Vite banner appears: "VITE v7.0 ready in 812ms"
  -> The server row updates: status "Running", port 5173, URL http://localhost:5173
  -> A green pulsing dot confirms the server is alive
  [analytics: dev_server_started, port: 5173, worktree: feature_branch, startup_seconds: 8]

Step 4: Bruno creates an ngrok tunnel to expose the server publicly
  -> He clicks the "Expose" button on the running server row
  -> A confirmation dialog: "This will create a public URL accessible from the internet. Continue?"
  -> He confirms
  -> LemonDo calls the ngrok API and establishes a tunnel in ~3 seconds
  -> A shareable URL appears: https://a1b2c3d4.ngrok.io — with a "Copy" button beside it
  [analytics: ngrok_tunnel_created, port: 5173, project_id: hashed]

Step 5: Bruno copies the URL and shares it with the client
  -> He clicks "Copy" — the URL is on his clipboard
  -> He pastes it into the video call chat
  -> The ngrok URL row shows: status "Active", tunnel duration "0:00:35", requests served "3"
  -> The client confirms they can see the payments flow in their browser
  [analytics: ngrok_url_copied, project_id: hashed]
  [cross-module: comms — if Bruno sent this URL via a message from LemonDo's Comms tab, CM-010 (reply without leaving app) would apply]

Step 6: Bruno stops the tunnel after the demo ends
  -> He clicks "Stop" on the ngrok row
  -> The tunnel closes immediately; the public URL becomes inaccessible
  -> The server itself remains running (only the tunnel stopped)
  -> The row reverts: status "Running (local only)"
  [analytics: ngrok_tunnel_stopped, tunnel_duration_seconds: 847, requests_served: 12]

Step 7: Bruno stops the dev server once he's done for the day
  -> He clicks "Stop" on the server row
  -> The process is terminated cleanly
  -> Status updates to "Stopped", the green dot disappears
  -> LemonDo shows the last exit code: 0 (clean shutdown)
  [analytics: dev_server_stopped, worktree: feature_branch, uptime_seconds: 912]
```

**Expected Emotion**: Calm and professional under time pressure. "Client on the call in 10 minutes — done in 3."

---

## Scenario S-PM-04: Morning Project Dashboard Review (Bruno)

**Context**: It's 8:45am on a Tuesday. Bruno has four active projects across two clients and his personal tools. Before opening any editor or terminal, he opens LemonDo's Projects view to get a fast read on the state of everything — what changed overnight, what needs attention, what's running.

```
Step 1: Bruno opens LemonDo to the Projects view
  -> The Projects grid loads, showing 4 project cards
  -> Each card has a status ring: green (clean), amber (uncommitted changes), red (requires attention)
  -> At a glance: acme-platform (amber), lemon-todo-v2 (amber), personal-scripts (green), old-client-archive (green)
  [analytics: session_started, device: desktop, initial_view: projects]
  [analytics: projects_view_opened, project_count: 4]

Step 2: Bruno checks acme-platform first (amber — uncommitted changes)
  -> He clicks the acme-platform card
  -> The Overview tab shows a summary panel: "3 worktrees — 1 has uncommitted changes"
  -> Worktrees list: main (clean), feature/payments-v2 (2 files modified), feature/auth-refresh (clean, 3 commits ahead of main)
  -> The modified worktree is highlighted with a count badge "2 unstaged"
  -> No dev servers are currently running
  [analytics: project_detail_opened, initial_tab: overview, dirty_worktree_count: 1]

Step 3: Bruno checks the uncommitted changes in feature/payments-v2
  -> He clicks the worktree row to expand it
  -> A diff summary shows: "2 files changed — src/payments/PaymentForm.tsx (+12, -3), src/payments/api.ts (+5, -1)"
  -> He recalls: yesterday he started a small tweak but didn't commit before shutting down
  -> He makes a mental note to commit before starting today's agent session
  [analytics: worktree_diff_viewed, files_changed: 2, project_id: hashed]

Step 4: Bruno checks lemon-todo-v2 (amber)
  -> He navigates back to the Projects grid and opens lemon-todo-v2
  -> Overview: "feature/v2-planning — 5 files modified (documentation changes)"
  -> He sees the last commit: "docs(planning): complete Phase 0 + Phase 1" — 2 days ago
  -> No running servers, no active agent sessions
  -> He adds a quick task: "Commit Phase 2 scenario docs" — it auto-links to this project
  [analytics: project_detail_opened, project_id: hashed, dirty_worktree_count: 1]
  [analytics: task_created, source: project_dashboard, project_linked: true]
  [cross-module: tasks — PM-009 (aggregate tasks by project)]

Step 5: Bruno scans the GitHub notifications panel (cross-module)
  -> A sidebar widget on the project detail shows recent GitHub activity for acme-platform
  -> He sees: 1 pull request comment from a collaborator on feature/auth-refresh, 2 CI check results
  -> The PR comment is marked as needing his review
  -> He clicks "View in Comms" — this takes him to the Comms unified inbox, filtered to this project's GitHub notifications
  [analytics: github_notifications_viewed, project_id: hashed, notification_count: 3]
  [analytics: comms_deeplink_followed, source: project_dashboard, filter: project_github]
  [cross-module: comms — CM-006 (GitHub notifications), CM-008 (filter by channel)]

Step 6: Bruno reviews his open tasks across all projects
  -> He clicks the "Tasks" tab on the Projects overview (not a specific project — the global tasks panel)
  -> The task list filters to show only tasks linked to any project, sorted by project then priority
  -> He sees: 4 tasks for acme-platform (2 high, 1 medium, 1 low), 2 for lemon-todo-v2 (1 high, 1 medium)
  -> He identifies the one high-priority acme-platform task to tackle first: "Implement payment webhook handler"
  [analytics: tasks_view_opened, filter: project_linked, project_count: 2, task_count: 6]

Step 7: Bruno plans his day — starts a dev server and assigns a task to an agent
  -> He navigates back to acme-platform → Worktrees, commits the 2 uncommitted files via the UI (commit message: "wip: payment form tweaks")
  -> He starts the dev server on feature/payments-v2
  -> He right-clicks the "Implement payment webhook handler" task and selects "Start Agent Session"
  -> LemonDo opens the Agents tab, pre-filled with the task title and the feature/payments-v2 worktree context
  -> He confirms: "Start session"
  -> The agent session begins and Bruno switches back to his coffee
  [analytics: worktree_committed, files_committed: 2, project_id: hashed]
  [analytics: dev_server_started, port: 5173, worktree: feature_branch]
  [analytics: agent_session_started, source: task_context_menu, project_id: hashed, task_id: hashed]
  [cross-module: agents — AG-001 (start agent session), AG-004 (assign task to session), AG-005 (auto-create worktree context)]
```

**Expected Emotion**: Grounded and in command before the day has started. "I know exactly what every project is doing. No surprises."

---

## Scenario Coverage Matrix

| FR ID | Requirement | Scenarios |
|-------|-------------|-----------|
| PM-001 | Register a project by pointing to a local git repository path | S-PM-01 (Steps 2–5) |
| PM-002 | View project metadata: name, description, tech stack, branch info | S-PM-01 (Steps 4–5), S-PM-04 (Steps 2–4) |
| PM-003 | View and navigate project documentation (markdown files) | S-PM-01 (Step 6) |
| PM-004 | Create and manage git worktrees from the UI | S-PM-02 (Steps 2–4) |
| PM-005 | View worktree status (current branch, dirty/clean, ahead/behind) | S-PM-02 (Step 4), S-PM-04 (Steps 2–3) |
| PM-006 | Install project dependencies from UI | S-PM-02 (Step 5) |
| PM-007 | Start/stop/restart project dev servers remotely | S-PM-03 (Steps 2–3, 7), S-PM-04 (Step 7) |
| PM-008 | Expose local dev servers via ngrok integration | S-PM-03 (Steps 4–6) |
| PM-009 | Aggregate tasks by project (link tasks to a project) | S-PM-04 (Steps 4, 6) |
| PM-010 | Link people/companies to projects (collaborators, stakeholders) | S-PM-02 (Step 7) |
| PM-011 | Project-level settings (default branch, CI/CD status, environment vars) | Not covered — deferred (P2, no scenario at this detail level yet) |
| PM-012 | View git log and branch history within the UI | S-PM-02 (Step 6) |
