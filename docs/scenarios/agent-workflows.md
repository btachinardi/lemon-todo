# Agent Workflow Scenarios

> **Source**: Extracted from docs/PRD.2.draft.md §7.4
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

> **Status**: Draft (v2)

## Scenario S-AG-01: Batch Feature Development

> Bruno selects 7 tasks from the backlog for the next version. He clicks "Create worktrees and start working." LemonDo creates 7 worktrees, starts 7 Claude Code sessions (within budget limits), each assigned to one task. Bruno monitors progress in a dashboard, reviews completed work, and approves merges.

---

## Scenario S-AG-02: Email-to-Task Automation

> An agent is configured to process Bruno's inbox every morning. It reads customer feedback emails, creates bug fix tasks (tagged "customer-feedback") in the appropriate project, and creates improvement tasks for feature requests. Bruno reviews the generated tasks over coffee.

---

## Scenario S-AG-03: Sequential Quality Pipeline

> Bruno assigns 5 tasks to a sequential queue for a project that can't have parallel worktrees. Agent #1 completes task 1, commits, and signals done. Agent #2 starts on task 2 with the updated codebase. Each agent runs the verification gate before signaling completion.

---

## Scenario S-AG-04: Agent Creates Follow-Up Work

> While working on a feature, an agent discovers a related bug. It calls the LemonDo API to create a new bug fix task, tagged with the project and linked to the current feature task. Bruno sees the new task in his board without interrupting the agent's work.
