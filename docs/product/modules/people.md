# People & Companies Module

> **Source**: Extracted from docs/PRD.2.draft.md §6
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

> **Status**: Draft (v2)

## Overview

A lightweight CRM for tracking relationships with people and companies Bruno interacts with. Not a full CRM suite — focused on knowledge retention and relationship context.

---

## Core Concepts

- **Person** = an individual Bruno interacts with
- **Company** = an organization (may have associated people)
- **Note** = a timestamped piece of information about a person/company
- **Tag** = categorization (client, colleague, friend, family, vendor, etc.)
- **Relationship Type** = professional, personal, or both

---

## Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| PP-001 | Create and manage person records | P0 | Name, email(s), phone(s), photo |
| PP-002 | Create and manage company records | P0 | Name, website, industry, logo |
| PP-003 | Link people to companies (role, department) | P0 | Many-to-many |
| PP-004 | General information section (bio, role, how we met) | P0 | Free-form + structured |
| PP-005 | Notes/learnings section (timestamped entries) | P0 | "Mentioned daughter starts school in Sept" |
| PP-006 | Preferences section (communication style, timezone, tools they use) | P1 | Structured fields |
| PP-007 | Personal vs professional information separation | P1 | Toggle visibility context |
| PP-008 | Important dates tracking (birthdays, anniversaries, milestones) | P1 | With reminders |
| PP-009 | Link people to projects (role: collaborator, stakeholder, client) | P1 | Cross-module reference |
| PP-010 | Link people to communication messages/threads | P1 | Auto-link by email/handle match |
| PP-011 | Tag and categorize people/companies | P0 | Flexible tagging |
| PP-012 | Search across all people and companies | P0 | Full-text |
| PP-013 | Timeline view of all interactions with a person | P2 | Aggregate from comms + notes + tasks |
| PP-014 | Family/relationship tree for personal contacts | P3 | "Partner: Maria, Kids: Lucas, Sofia" |

---

## Key Scenarios

**S-PP-01: Meeting Preparation**
> Before a call with a client, Bruno opens their profile. He sees they prefer async communication, use Slack for quick questions, and mentioned last month that they're migrating to AWS. He also sees they have a birthday next week. He sends a quick "happy birthday" message after the meeting.

**S-PP-02: Knowledge Capture**
> During a conversation, a colleague mentions they're an expert in Kubernetes. Bruno opens their profile, adds a note: "Strong K8s experience, offered to help with our deployment". This knowledge is now searchable and visible next time Bruno needs K8s help.
