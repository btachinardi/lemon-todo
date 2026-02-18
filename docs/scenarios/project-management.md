# Project Management Scenarios

> **Source**: Extracted from docs/PRD.2.draft.md §4.4
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

> **Status**: Draft (v2)

## Scenario S-PM-01: Register Existing Project

> Bruno opens LemonDo, clicks "Add Project", selects a local folder. LemonDo scans the repo, detects it's a pnpm monorepo with .NET backend, reads the README and CLAUDE.md, and creates a project card with tech stack tags and documentation links.

---

## Scenario S-PM-02: Parallel Feature Development

> Bruno has 3 features to build for a project. He selects the project, clicks "Create Worktrees", names them after the feature branches. Each worktree appears as a tab within the project. He can see the status of each, start dev servers, and assign agent sessions to each.

---

## Scenario S-PM-03: Quick Demo Setup

> Bruno needs to demo a feature to a client. He clicks "Expose" on a running dev server, LemonDo creates an ngrok tunnel and provides a shareable URL. When the demo ends, he clicks "Stop" and the tunnel closes.
