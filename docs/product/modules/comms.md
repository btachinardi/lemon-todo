# Communications Module

> **Source**: Extracted from docs/PRD.2.draft.md §5
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

> **Status**: Draft (v2)

## Overview

A unified inbox that aggregates messages from multiple communication channels into a single, filterable, priority-aware interface. The goal is: **never miss or forget an important message again.**

---

## Core Concepts

- **Channel** = a communication source (Gmail, WhatsApp, Discord, Slack, LinkedIn, GitHub)
- **Thread** = a conversation/email thread within a channel
- **Message** = an individual message within a thread
- **Priority** = user-assigned or AI-suggested importance level
- **Link** = association between a message/thread and a project, task, or person

---

## Functional Requirements

| ID | Requirement | Priority | Notes |
|----|-------------|----------|-------|
| CM-001 | Connect Gmail account(s) via OAuth2 | P0 | Read inbox, labels, threads |
| CM-002 | Connect WhatsApp via WhatsApp Business API or bridge | P1 | Read/send messages |
| CM-003 | Connect Discord via bot token | P1 | Monitor specific servers/channels |
| CM-004 | Connect Slack via bot/app token | P1 | Monitor specific workspaces/channels |
| CM-005 | Connect LinkedIn messaging (scraping or API if available) | P2 | Read messages, connection requests |
| CM-006 | Connect GitHub notifications | P1 | Issues, PRs, mentions, reviews |
| CM-007 | Unified inbox view with all messages across channels | P0 | Chronological or priority-sorted |
| CM-008 | Filter by channel, priority, read/unread, date range | P0 | Faceted filtering |
| CM-009 | View all priority messages across all channels | P0 | "Important" smart view |
| CM-010 | Reply to messages without leaving LemonDo | P1 | Per-channel send integration |
| CM-011 | Link messages/threads to projects | P1 | Cross-module reference |
| CM-012 | Link messages/threads to tasks (as attachments) | P1 | Context on tasks |
| CM-013 | Link messages to people/companies | P1 | Conversation history per person |
| CM-014 | AI-powered message categorization and priority suggestion | P2 | Based on sender, content, urgency keywords |
| CM-015 | Notification when new high-priority messages arrive | P1 | Desktop notification |
| CM-016 | Search across all channels simultaneously | P0 | Full-text search |

---

## Key Scenarios

**S-CM-01: Morning Inbox Review**
> Bruno opens LemonDo's Comms tab. He sees 12 unread messages: 5 emails, 3 Slack messages, 2 WhatsApp, 1 Discord, 1 GitHub review request. He switches to "Priority" view and sees the 3 most important items highlighted. He replies to the urgent email directly from LemonDo.

**S-CM-02: Linking Communication to Work**
> A client sends an email with a bug report. Bruno reads it in the unified inbox, clicks "Create Task", which pre-fills the task with the email subject and links the email thread as an attachment. The task is automatically assigned to the client's project.

**S-CM-03: Person-Centric Communication History**
> Bruno is about to meet with a client. He opens their People profile and sees a timeline of all communications: recent emails, Slack messages, and a WhatsApp thread from last week. He's fully prepared without searching 5 different apps.
