# Communications Scenarios

> **Source**: Expanded from docs/PRD.2.draft.md §5.4 and task brief (2026-02-18)
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## Scenario S-CM-01: Morning Inbox Review (Bruno)

**Context**: It is 8:45 AM on a Tuesday. Bruno sits down at his desktop with coffee. He opens LemonDo to triage overnight and early-morning messages before starting his development work. He has not yet opened Gmail, WhatsApp, or Slack — LemonDo is his first stop.

```
Step 1: Bruno clicks the "Comms" tab in LemonDo's main navigation
  -> The unified inbox loads, aggregating messages from all connected channels
  -> He sees a badge: 12 unread — 5 Gmail, 3 Slack, 2 WhatsApp, 1 Discord, 1 GitHub
  -> Messages appear in a single chronological list with channel icons on the left
  -> Each row shows: channel icon, sender name, subject/preview, timestamp, read/unread state
  [analytics: comms_inbox_opened, unread_count: 12, channels_active: 5]

Step 2: Bruno switches from "All" to the "Priority" view
  -> He clicks the "Priority" button in the view toggle bar
  -> The list re-renders showing 3 messages flagged as high-priority
  -> Two are AI-suggested (orange dot) and one Bruno manually flagged yesterday (red dot)
  -> The first is an email from a client: "URGENT: Production bug on the dashboard"
  [analytics: comms_view_switched, from: chronological, to: priority]

Step 3: Bruno opens the urgent client email
  -> He clicks the email row — it expands inline (no page navigation)
  -> The full email body renders with the client's name linked to their People profile
  -> He sees a yellow banner: "Lucas Ferreira — Client, Project: LemonDo Dashboard"
  -> [cross-module: People] The sender was automatically matched to a People record by email address
  -> [cross-module: Projects] The project link was inferred from the email domain and prior message history
  [analytics: comms_message_opened, channel: gmail, has_people_link: true, has_project_link: true]

Step 4: Bruno reads the email and decides it needs a task
  -> He clicks "Create Task" in the message action bar
  -> A task creation panel slides in on the right side — no full navigation away
  -> The task title is pre-filled: "Production bug on the dashboard" (from email subject)
  -> Project is pre-set to "LemonDo Dashboard"; priority is pre-set to "High"
  -> The email thread is attached to the task as a context link
  -> Bruno adjusts the due date to today, then clicks "Create"
  -> [cross-module: Tasks] The task appears in his board immediately
  [analytics: comms_task_created_from_message, channel: gmail, prefill_used: true, project_autoset: true]

Step 5: Bruno replies to the client email directly from LemonDo
  -> He clicks "Reply" in the message action bar
  -> An inline reply composer opens below the email, matching the reply-in-place pattern
  -> He types: "On it — I've created a task and will push a fix within the hour."
  -> He clicks "Send" — the reply is dispatched via the Gmail adapter
  -> A subtle "Sent" confirmation appears; the thread is marked as replied
  [analytics: comms_message_replied, channel: gmail, reply_length_chars: 68]

Step 6: Bruno continues triaging the remaining 11 messages
  -> He uses keyboard shortcut J/K to move between messages, E to mark as read
  -> He marks 3 Slack messages as read (daily standup noise, no action needed)
  -> He snoozes 1 WhatsApp message: "Remind me tonight" — it disappears from inbox
  -> He opens the GitHub review request, reads the diff summary, and marks it for later
  -> After 4 minutes, 11 of 12 messages are processed; 1 snoozed
  [analytics: comms_message_marked_read x3, channel: slack]
  [analytics: comms_message_snoozed, channel: whatsapp, snooze_duration: until_tonight]
  [analytics: comms_inbox_triaged, total: 12, acted_on: 4, read_only: 7, snoozed: 1, duration_seconds: 238]
```

**Expected Emotion**: Calm, in control, ahead of the day. "I know exactly what needs attention and what can wait. And I haven't opened a single other app."

---

## Scenario S-CM-02: Replying to a WhatsApp Message Without Leaving LemonDo (Bruno)

**Context**: It is mid-afternoon. Bruno is deep in a coding session. A desktop notification pops up: a WhatsApp message from a contact, Mariana Silva, asking about the status of a freelance project. Bruno wants to respond immediately without breaking his flow by switching to WhatsApp on his phone or a separate web tab.

```
Step 1: Desktop notification appears in the top-right corner
  -> LemonDo surfaces a toast notification: "WhatsApp · Mariana Silva: 'Hey, any update on the...'"
  -> Two quick-action buttons appear on the notification: "View" and "Dismiss"
  -> Bruno clicks "View"
  [analytics: comms_notification_received, channel: whatsapp, action: view]

Step 2: The Comms tab opens directly to Mariana's WhatsApp thread
  -> LemonDo navigates to the Comms tab and scrolls to the unread WhatsApp message
  -> Bruno sees the full conversation thread — the last 8 messages in context
  -> Mariana's name in the thread header is a link: "Mariana Silva — Freelance Client"
  -> [cross-module: People] Her People record shows role: Freelance Client, preferred contact: WhatsApp
  [analytics: comms_thread_opened, channel: whatsapp, from_notification: true, has_people_link: true]

Step 3: Bruno reads the message and checks the linked project context
  -> He reads: "Hey, any update on the landing page? Client is asking me."
  -> He clicks her name to open the People panel in a side drawer (without leaving the thread)
  -> He sees: project "Mariana — Landing Page" is 80% complete, last activity 2 days ago
  -> [cross-module: Projects] The project status is pulled from the Projects module
  -> He closes the side drawer and returns to the thread
  [analytics: comms_people_panel_opened, from: thread_context]

Step 4: Bruno types and sends a WhatsApp reply
  -> He clicks the reply box at the bottom of the thread
  -> He types: "Yes! Almost done — targeting Friday delivery. I'll send you a preview link tomorrow."
  -> He clicks "Send" — the message is dispatched via the WhatsApp adapter
  -> The message appears in the thread with a "Sent" checkmark
  -> The thread is marked as replied; the unread badge clears
  [analytics: comms_message_replied, channel: whatsapp, reply_length_chars: 85]

Step 5: Bruno returns to his previous task with zero tab switching
  -> He clicks the back arrow or uses keyboard shortcut Alt+Left to return to the Projects tab
  -> His coding session context is exactly where he left it
  -> Total time in Comms: 47 seconds
  [analytics: comms_reply_session_duration_seconds: 47, returned_to: projects]
```

**Expected Emotion**: Uninterrupted and responsive. "I replied in under a minute without losing my flow. I didn't have to unlock my phone or open a single other app."

---

## Scenario S-CM-03: Searching Across All Channels Simultaneously (Bruno)

**Context**: It is Thursday morning. Bruno needs to find a conversation where someone shared AWS cost-saving recommendations. He vaguely remembers it was either an email or a Slack message, sometime in the last two months. He has no idea which channel or who sent it — he just remembers the topic.

```
Step 1: Bruno opens the Comms search bar
  -> He clicks the search icon in the Comms tab header, or presses Ctrl+F from within Comms
  -> A prominent search bar appears with placeholder: "Search across all channels..."
  -> A row of channel filter chips appears below: All | Gmail | WhatsApp | Discord | Slack | LinkedIn | GitHub
  -> All channels are selected by default
  [analytics: comms_search_opened, channels_selected: all]

Step 2: Bruno types his search query
  -> He types: "AWS cost"
  -> Results begin appearing after a short debounce (300 ms), searching across all channels
  -> Results stream in: 2 Gmail threads, 1 Slack message, 1 Discord message
  -> Each result shows: channel icon, sender, snippet with search terms highlighted, date
  [analytics: comms_search_performed, query_length_chars: 8, results_count: 4, channels_hit: 3]

Step 3: Bruno narrows the results by channel
  -> He clicks the "Slack" chip to filter to Slack only
  -> Results collapse to 1 item: a Slack message from "dev-general" channel, 6 weeks ago
  -> The snippet shows: "...here's the full AWS cost breakdown — reserved instances vs on-demand..."
  -> This looks promising; Bruno clicks it
  [analytics: comms_search_filter_applied, filter: channel, value: slack, results_after: 1]

Step 4: Bruno opens the search result
  -> The Slack thread opens inline, scrolled to the matching message
  -> The search term "AWS cost" is highlighted in yellow within the message body
  -> The full thread is visible above and below — Bruno reads 3 replies for context
  -> He sees the sender: "Carlos Mendes" — he remembers now, a colleague from a previous project
  [analytics: comms_search_result_opened, channel: slack, scroll_context: true]

Step 5: Bruno links Carlos's message to a project for future reference
  -> He clicks "Link to Project" in the message action bar
  -> A project picker dropdown appears — he selects "Infrastructure Spike"
  -> The message is saved as a reference on that project
  -> [cross-module: Projects] The project now has a "Communications" section with this thread attached
  [analytics: comms_message_linked_to_project, channel: slack, project_id: hashed]

Step 6: Bruno also links the message to Carlos's People record
  -> He clicks "Link to Person" in the message action bar
  -> He searches "Carlos" — Carlos Mendes appears with a tag: "Former Colleague"
  -> He clicks Carlos's name; the message is attached to his People record
  -> [cross-module: People] Carlos's communication timeline now includes this Slack thread
  [analytics: comms_message_linked_to_person, channel: slack, person_id: hashed]
```

**Expected Emotion**: Relieved and confident. "I found it in 30 seconds. I would have spent 10 minutes checking three different apps and still might have missed it."

---

## Scenario S-CM-04: Configuring a New Channel — Discord (Bruno)

**Context**: Bruno has just set up a Discord bot for a new client project server. He wants to connect LemonDo to that Discord server so he can monitor specific channels without having to context-switch to Discord throughout the day. This is the first time he is adding Discord as a channel; his Gmail and Slack are already connected.

```
Step 1: Bruno opens channel settings
  -> He navigates to Comms → Settings (gear icon in the Comms tab header)
  -> He sees the "Connected Channels" panel listing: Gmail (connected), Slack (connected)
  -> He sees a list of available channels to add: WhatsApp, Discord, GitHub, LinkedIn
  -> He clicks "+ Connect" next to Discord
  [analytics: comms_settings_opened]
  [analytics: comms_channel_connect_started, channel: discord]

Step 2: Bruno enters his Discord bot token
  -> A configuration panel slides open on the right
  -> Step 1 of 3: "Bot Token" — a password-style input field
  -> Helper text: "Create a bot at discord.com/developers and paste the token here."
  -> A link icon opens Discord's developer portal in a new browser tab
  -> Bruno pastes his bot token and clicks "Next"
  [analytics: comms_channel_config_step_completed, channel: discord, step: bot_token]

Step 3: Bruno selects which servers and channels to monitor
  -> Step 2 of 3: "Select Servers & Channels"
  -> LemonDo uses the token to fetch his bot's server list — it shows 2 servers
  -> He selects "ClientProject Alpha" server and expands it
  -> He sees a list of channels: #general, #dev-updates, #bugs, #releases
  -> He checks #dev-updates and #bugs; leaves #general and #releases unchecked
  -> He clicks "Next"
  [analytics: comms_channel_config_step_completed, channel: discord, step: server_selection, servers_selected: 1, channels_selected: 2]

Step 4: Bruno configures notification preferences for Discord
  -> Step 3 of 3: "Notifications"
  -> Options: "All messages", "Only @mentions", "Only messages matching keywords"
  -> He selects "Only messages matching keywords"
  -> A keyword input appears — he types: "bug, error, urgent, deploy"
  -> Toggle: "Desktop notification for high-priority matches" — he turns it on
  -> He clicks "Save & Connect"
  [analytics: comms_channel_config_step_completed, channel: discord, step: notifications, mode: keywords, desktop_notify: true]

Step 5: LemonDo sends a test message and confirms the connection
  -> A progress indicator shows: "Connecting bot... Testing access..."
  -> After 2 seconds: a green checkmark — "Discord connected successfully"
  -> A test message appears in his unified inbox: "[Discord — ClientProject Alpha / #bugs] Connection test from LemonDo bot. Reply to confirm."
  -> The Discord channel is now listed in "Connected Channels" with a green status dot
  [analytics: comms_channel_connected, channel: discord, test_message_received: true]

Step 6: Bruno sees Discord messages appear in his unified inbox
  -> He clicks back to the main Comms inbox
  -> The channel filter bar now includes a "Discord" chip alongside Gmail and Slack
  -> A message from #bugs from 12 minutes ago has already surfaced: "deploy pipeline failing on staging"
  -> The message has an AI-suggested priority badge: "High" (keyword match: "deploy", "failing")
  -> [cross-module: Projects] LemonDo has auto-matched "staging" to a project context
  [analytics: comms_message_auto_prioritized, channel: discord, reason: keyword_match, priority: high]
```

**Expected Emotion**: Satisfied and in command. "That took three minutes and I'm now monitoring a client server without ever opening Discord. Everything comes to me now."

---

## Scenario Coverage Matrix

| FR ID | Requirement | Scenarios |
|-------|-------------|-----------|
| CM-001 | Connect Gmail account(s) via OAuth2 | S-CM-01, S-CM-04 (implied, Gmail already connected) |
| CM-002 | Connect WhatsApp via WhatsApp Business API or bridge | S-CM-02 |
| CM-003 | Connect Discord via bot token | S-CM-04 |
| CM-004 | Connect Slack via bot/app token | S-CM-03 (Slack already connected, used in search) |
| CM-005 | Connect LinkedIn messaging | Not yet covered — deferred (P2, open API question) |
| CM-006 | Connect GitHub notifications | S-CM-01 (GitHub review request in morning triage) |
| CM-007 | Unified inbox view with all messages across channels | S-CM-01, S-CM-02 |
| CM-008 | Filter by channel, priority, read/unread, date range | S-CM-01 (priority view), S-CM-03 (channel filter) |
| CM-009 | View all priority messages across all channels ("Important" view) | S-CM-01 (Step 2) |
| CM-010 | Reply to messages without leaving LemonDo | S-CM-01 (Step 5, Gmail), S-CM-02 (Steps 4-5, WhatsApp) |
| CM-011 | Link messages/threads to projects | S-CM-01 (Step 4, task creation links thread), S-CM-03 (Step 5) |
| CM-012 | Link messages/threads to tasks (as attachments) | S-CM-01 (Step 4 — email thread attached to new task) |
| CM-013 | Link messages to people/companies | S-CM-01 (Step 3, auto-link by email), S-CM-02 (Step 2), S-CM-03 (Step 6) |
| CM-014 | AI-powered message categorization and priority suggestion | S-CM-04 (Step 6 — keyword-based AI priority badge) |
| CM-015 | Notification when new high-priority messages arrive | S-CM-02 (Steps 1-2, desktop toast), S-CM-04 (Step 4, config) |
| CM-016 | Search across all channels simultaneously | S-CM-03 (full scenario) |
