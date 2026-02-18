# Projects Module

> **Source**: Extracted from docs/PRD.2.draft.md §4
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

> **Status**: Draft (v2)

## Overview

A project is a **git repository** with associated metadata, configuration, and operational controls. Projects aggregate tasks, link to people (collaborators), and serve as the organizational unit for development workflows.

---

## Core Concepts

- **Project** = a git repository (local or remote)
- **Worktree** = a git worktree within a project (for parallel feature development)
- **Environment** = a deployment target (local dev, staging, production)
- **Instructions** = development guidelines, deployment docs, architecture notes (markdown files already in repo)

---

## Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| PM-001 | Register a project by pointing to a local git repository path | P0 | Scan for README, CLAUDE.md, GUIDELINES.md, etc. |
| PM-002 | View project metadata: name, description, tech stack, branch info | P0 | Auto-detect from repo |
| PM-003 | View and navigate project documentation (markdown files) | P0 | Render markdown in-app |
| PM-004 | Create and manage git worktrees from the UI | P1 | `git worktree add/remove/list` |
| PM-005 | View worktree status (current branch, dirty/clean, ahead/behind) | P1 | Real-time or polling |
| PM-006 | Install project dependencies from UI (npm install, dotnet restore, etc.) | P1 | Detect package manager |
| PM-007 | Start/stop/restart project dev servers remotely | P1 | Process management |
| PM-008 | Expose local dev servers via ngrok integration | P2 | For external testing |
| PM-009 | Aggregate tasks by project (link tasks to a project) | P0 | Filter boards/lists by project |
| PM-010 | Link people/companies to projects (collaborators, stakeholders) | P1 | Cross-module reference |
| PM-011 | Project-level settings (default branch, CI/CD status, environment vars) | P2 | Configuration management |
| PM-012 | View git log and branch history within the UI | P1 | Visual git graph |

---

## Key Scenarios

**S-PM-01: Register Existing Project**
> Bruno opens LemonDo, clicks "Add Project", selects a local folder. LemonDo scans the repo, detects it's a pnpm monorepo with .NET backend, reads the README and CLAUDE.md, and creates a project card with tech stack tags and documentation links.

**S-PM-02: Parallel Feature Development**
> Bruno has 3 features to build for a project. He selects the project, clicks "Create Worktrees", names them after the feature branches. Each worktree appears as a tab within the project. He can see the status of each, start dev servers, and assign agent sessions to each.

**S-PM-03: Quick Demo Setup**
> Bruno needs to demo a feature to a client. He clicks "Expose" on a running dev server, LemonDo creates an ngrok tunnel and provides a shareable URL. When the demo ends, he clicks "Stop" and the tunnel closes.
