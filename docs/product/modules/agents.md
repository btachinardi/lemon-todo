# Agent Sessions Module

> **Source**: Extracted from docs/PRD.2.draft.md §7
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

> **Status**: Draft (v2)

## Overview

The most ambitious module. Enables Bruno to interact with Claude Code agent sessions directly from the LemonDo UI, automate task-to-agent workflows, and orchestrate parallel development across worktrees.

---

## Core Concepts

- **Agent Session** = a Claude Code CLI session (or Claude Agent SDK session)
- **Work Queue** = ordered list of tasks assigned to agent processing
- **Execution Mode** = parallel (multiple worktrees) or sequential (one after another)
- **Budget** = token/cost limit per session or per queue
- **Agent API** = REST endpoints agents can call to interact with LemonDo (create tasks, update projects, log learnings)

---

## Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| AG-001 | Start a new Claude Code agent session from UI | P0 | With project context |
| AG-002 | View active agent sessions and their status | P0 | Running, idle, completed, failed |
| AG-003 | View agent session output/logs in real-time | P0 | Streaming terminal output |
| AG-004 | Assign a task to an agent session | P0 | Task becomes the agent's objective |
| AG-005 | Auto-create worktree for agent session | P1 | Isolate agent work |
| AG-006 | Select multiple tasks and batch-assign to agent queue | P1 | "Start working on these 7 features" |
| AG-007 | Parallel execution mode: multiple agents on separate worktrees | P1 | For speed |
| AG-008 | Sequential execution mode: one agent finishes, next starts | P1 | For dependent work |
| AG-009 | Work queue with priority ordering | P1 | Higher priority tasks processed first |
| AG-010 | Budget management: set token/cost limits per session | P1 | Prevent runaway costs |
| AG-011 | Budget management: set limits per queue | P2 | Total budget for a batch |
| AG-012 | Agent API: expose REST endpoints for agents to create/manage tasks | P1 | Agents can create follow-up tasks |
| AG-013 | Agent API: expose endpoints for agents to update project state | P2 | Agents can log decisions, update docs |
| AG-014 | Agent API: expose endpoints for agents to add people/comms learnings | P2 | Auto-capture relationship data |
| AG-015 | Agent-driven email-to-task automation | P2 | Agent reads emails, creates categorized tasks |
| AG-016 | Agent session templates (predefined objectives, context, constraints) | P2 | Reusable agent configurations |
| AG-017 | Claude Agent SDK integration for custom agent behaviors | P2 | Beyond CLI sessions |
| AG-018 | Session history and audit trail | P1 | What did the agent do, when, cost |
| AG-019 | Approve/reject agent-proposed changes before merge | P1 | Human-in-the-loop |
| AG-020 | Agent notification on completion/failure | P0 | Desktop notification + in-app |

---

## Key Scenarios

**S-AG-01: Batch Feature Development**
> Bruno selects 7 tasks from the backlog for the next version. He clicks "Create worktrees and start working." LemonDo creates 7 worktrees, starts 7 Claude Code sessions (within budget limits), each assigned to one task. Bruno monitors progress in a dashboard, reviews completed work, and approves merges.

**S-AG-02: Email-to-Task Automation**
> An agent is configured to process Bruno's inbox every morning. It reads customer feedback emails, creates bug fix tasks (tagged "customer-feedback") in the appropriate project, and creates improvement tasks for feature requests. Bruno reviews the generated tasks over coffee.

**S-AG-03: Sequential Quality Pipeline**
> Bruno assigns 5 tasks to a sequential queue for a project that can't have parallel worktrees. Agent #1 completes task 1, commits, and signals done. Agent #2 starts on task 2 with the updated codebase. Each agent runs the verification gate before signaling completion.

**S-AG-04: Agent Creates Follow-Up Work**
> While working on a feature, an agent discovers a related bug. It calls the LemonDo API to create a new bug fix task, tagged with the project and linked to the current feature task. Bruno sees the new task in his board without interrupting the agent's work.
