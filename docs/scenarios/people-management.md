# People Management Scenarios

> **Source**: Extracted from docs/PRD.2.draft.md §6.4 and docs/product/modules/people.md
> **Status**: Draft (v2)
> **Last Updated**: 2026-02-18

---

## Scenario S-PP-01: Adding a New Contact (Bruno)

**Context**: Bruno just finished a video call with a new freelance client — Natalia — whom he met through a referral. He wants to capture her contact details and first impressions while they're fresh, so she doesn't become yet another name in a forgotten email thread.

```
Step 1: Bruno opens LemonDo and navigates to the People tab
  -> The People module loads showing an empty contacts list (first use) with a
     prominent "Add Person" button and a brief prompt: "Track the people you
     work with. Their history lives here."
  -> Bruno clicks "Add Person"
  [analytics: people_module_opened, contact_count: 0]
  [analytics: add_person_initiated, trigger: manual]

Step 2: Bruno fills in Natalia's basic details
  -> A side panel opens with a structured form:
     Name, Email(s), Phone(s), Photo URL, Tags
  -> He types: "Natalia Reyes", adds her email natalia@example.com,
     her WhatsApp number, and uploads a photo from his Downloads folder
  -> He assigns the tags "client" and "design"
  [analytics: person_record_created, has_email: true, has_phone: true,
     has_photo: true, tag_count: 2]

Step 3: Bruno fills in the "How We Met" section
  -> Below the contact fields is a "General Info" card with labeled sections:
     Role, Company, How We Met, Bio
  -> He types: "Referred by Marco Rossi. UI/UX designer specialising in
     mobile apps. Met during a project scoping call — very clear communicator."
  -> He sets Relationship Type to "Professional"
  [analytics: person_general_info_saved, has_how_we_met: true,
     relationship_type: professional]

Step 4: Bruno links Natalia to the active project
  -> He clicks "Link to Project" within the profile and a searchable dropdown
     appears listing his registered projects
  -> He selects "lemon-todo-v2" and sets her role as "Client"
  -> A confirmation chip appears: "lemon-todo-v2 — Client"
  [analytics: person_linked_to_project, role: client]
  [cross-module: projects — see S-PM-01 for project registration]

Step 5: Bruno saves and reviews the new record
  -> He clicks "Save". The side panel closes and Natalia's card appears in the
     contacts list with her photo, tags, and the linked project visible at a glance
  -> Bruno scans the card: all the details are exactly as entered
  -> He types "Natalia" in the global search bar to confirm discoverability
  -> Her card surfaces immediately as the top result
  [analytics: person_record_saved, tag_count: 2, linked_projects: 1]
  [analytics: people_search_performed, query_length: 7, results_count: 1]
```

**Expected Emotion**: Satisfied and in control. "That took two minutes and now she's searchable. I'll never lose her details again."

---

## Scenario S-PP-02: Viewing a Person's Full Context (Bruno)

**Context**: Bruno is about to reply to an email from Marco Rossi — the colleague who referred Natalia last week. Before he writes back, he wants a complete picture: what they've discussed, what Marco is working on, and any useful personal details he should acknowledge.

```
Step 1: Bruno opens Marco's profile from the People tab
  -> He types "Marco" in the search bar at the top of the People module
  -> Marco's card appears immediately; Bruno clicks it
  -> The profile page loads with a left-column summary (photo, name, tags, linked
     company) and a right-column tabbed view: Overview | Notes | Timeline | Comms
  [analytics: people_search_performed, query_length: 5, results_count: 1]
  [analytics: person_profile_opened, person_id: hashed]

Step 2: Bruno reviews the Overview tab
  -> The Overview tab shows:
     - General Info: "Senior backend engineer at Acme Ltd. Met at a conference
       two years ago. Highly opinionated about architecture — in a good way."
     - Preferences: timezone Europe/Lisbon, prefers async communication,
       uses Slack and GitHub daily
     - Important Dates: birthday March 12 (3 weeks away — a yellow reminder badge)
  -> Bruno notices the birthday reminder and makes a mental note
  [analytics: person_overview_viewed, has_preferences: true,
     has_important_dates: true, upcoming_date_count: 1]

Step 3: Bruno switches to the Notes tab
  -> The Notes tab shows a reverse-chronological list of timestamped entries:
     - 2026-02-10: "Referred Natalia Reyes as a UI/UX designer for the v2 frontend"
     - 2025-11-22: "Mentioned he's leading a migration to event-driven architecture
       at Acme — keen to compare notes sometime"
     - 2025-09-04: "Recommended the 'Designing Data-Intensive Applications' book"
  -> Bruno reads the migration note — it's relevant to today's reply
  [analytics: person_notes_viewed, note_count: 3]

Step 4: Bruno switches to the Comms tab
  -> The Comms tab aggregates all messages linked to Marco across channels:
     - 3 Gmail threads (most recent: 4 days ago, subject "v2 planning thoughts")
     - 1 Slack thread (2 weeks ago, #architecture-talk channel)
     - 2 GitHub mentions (last month, on a PR review)
  -> Messages are sorted newest-first with channel icons for quick scanning
  -> Bruno opens the most recent Gmail thread directly within the panel and
     rereads the last exchange without leaving LemonDo
  [analytics: person_comms_tab_viewed, linked_message_count: 6,
     channel_count: 3]
  [analytics: person_linked_message_opened, channel: gmail]
  [cross-module: comms — see S-CM-03 for person-centric communication history]

Step 5: Bruno switches to the Timeline tab
  -> The Timeline tab shows a unified chronological feed of all interactions:
     each Gmail reply, Slack message, note entry, and project event appears
     as a single row with date, type icon, and a one-line summary
  -> Bruno can see the full arc of his relationship with Marco at a glance:
     first contact two years ago through to this week's email
  -> He scrolls to November 2025 to confirm the AWS migration note is accurate
     before referencing it in his reply
  [analytics: person_timeline_viewed, event_count: 11]
  [analytics: person_timeline_scrolled, scroll_depth: historic]
  [cross-module: comms — timeline aggregates from Comms module (CM-013)]
  [cross-module: projects — timeline includes project link events (PP-009)]

Step 6: Bruno toggles the "Professional Only" context filter
  -> A toggle in the top-right of the profile reads "Show: All | Professional |
     Personal"
  -> He switches to "Professional" — personal notes (e.g., book recommendation)
     are hidden; only work-related entries remain visible
  -> He confirms the view is clean for anything he'd share on screen during a call
  [analytics: person_context_filter_toggled, to: professional]
  [cross-module: people — PP-007 personal vs professional separation]
```

**Expected Emotion**: Prepared and confident. "I know exactly what we've talked about and where things stand. I can write back without guessing."

---

## Scenario S-PP-03: Preparing for a Meeting (Bruno)

**Context**: Bruno has a call in fifteen minutes with Sofia Andrade, a client he's been working with for three months. He wants a quick but complete briefing — what was agreed last time, what she cares about, and anything personal worth acknowledging before the call starts.

```
Step 1: Bruno searches for Sofia from anywhere in LemonDo
  -> He uses the global search shortcut (Cmd+K) while on the Tasks board
  -> He types "Sofia" — the results show People, Projects, and Tasks matches
  -> He clicks Sofia Andrade's contact from the People section of results
  -> Her profile opens immediately in a focused "Meeting Prep" layout
  [analytics: global_search_performed, query: hashed, result_type: person]
  [analytics: person_profile_opened, person_id: hashed, trigger: global_search]

Step 2: Bruno sees the meeting prep summary at the top of her profile
  -> A "Quick Briefing" card auto-generates at the top of the profile:
     - Last contact: 6 days ago (Gmail — project status check)
     - Most recent note: "Concerned about the March launch timeline — prefers
       weekly status updates over long monthly reviews"
     - Upcoming date: Sofia's work anniversary at her company is tomorrow
       (marked with an orange badge — "Consider acknowledging")
     - Linked project: "client-portal-v3" with status "In Progress"
  -> Bruno reads the timeline concern note — he'll address it proactively today
  [analytics: person_meeting_prep_viewed, has_upcoming_dates: true,
     linked_project_count: 1, recent_note_count: 1]

Step 3: Bruno reviews the Notes tab for the full history
  -> He clicks into Notes and skims the last four entries:
     - 2026-02-12: "Approved the new dashboard mockups — wants the export
       feature prioritized"
     - 2026-01-28: "Prefers written summaries after calls rather than live
       note-taking — she says it's less distracting"
     - 2026-01-14: "Mentioned her team is small (3 people) — avoid suggesting
       integrations that need IT approval"
     - 2025-12-20: "Uses Notion for internal docs, Figma for design review"
  -> Bruno picks out two actionable points: written summary after the call,
     and the export feature priority
  [analytics: person_notes_viewed, note_count: 4, pre_meeting: true]

Step 4: Bruno checks the Preferences section
  -> Back on the Overview tab he confirms:
     - Communication style: Async-first, prefers structured agendas
     - Timezone: Europe/Madrid (UTC+1) — same timezone as Bruno
     - Tools: Notion, Figma, Slack (work), WhatsApp (quick questions)
  -> He notices WhatsApp is listed for quick questions — he'll send the meeting
     summary there rather than email
  [analytics: person_preferences_viewed]

Step 5: Bruno adds a pre-call note with his discussion agenda
  -> He clicks "Add Note" and types:
     "Pre-call agenda: (1) March timeline concerns — show updated milestone plan,
     (2) export feature — confirm priority & scope, (3) quick check on team
     adoption. Send written summary to WhatsApp after."
  -> The note saves with a timestamp and appears at the top of the Notes list
  [analytics: person_note_created, note_type: pre_meeting, length: medium]

Step 6: Bruno joins the call and, during it, adds a live note
  -> After the call starts, Bruno keeps Sofia's profile open in a side panel
  -> Midway through the call she mentions a new stakeholder — her CTO — who
     will now need to approve the launch
  -> Bruno types a new note inline: "CTO (name unknown) now has launch approval
     authority — check with Sofia for intro before March"
  -> The note saves instantly without interrupting his conversation
  [analytics: person_note_created, note_type: live_during_meeting]

Step 7: After the call, Bruno sends the agreed written summary
  -> He opens Sofia's Comms tab, selects the WhatsApp thread, and types a
     structured summary directly from LemonDo
  -> The message sends; LemonDo records it as a linked communication on
     Sofia's timeline
  -> He also updates the linked task "Export feature — Q1" to "In Progress"
     on the project board
  [analytics: person_comms_reply_sent, channel: whatsapp]
  [cross-module: comms — reply via Comms module (CM-010)]
  [cross-module: tasks — task status updated from People context (PP-009)]
```

**Expected Emotion**: Professional and thorough. "She felt heard. I had every detail I needed without scrambling through five apps before the call."

---

## Scenario S-PP-04: Tracking a Company Relationship (Bruno)

**Context**: Bruno has worked with three people from Stellar Labs — a design agency — across different projects over the past year. The contacts exist as individual records but there's no company-level view. He wants to create a Stellar Labs company record, link his contacts to it, and see the company's combined project involvement and communication history.

```
Step 1: Bruno creates a new company record
  -> From the People module, he clicks the "Companies" tab (alongside "People")
  -> The view is empty except for a prompt: "Companies you work with. Link
     people and projects here."
  -> He clicks "Add Company" and fills in:
     Name: "Stellar Labs"
     Website: stellarlabs.io
     Industry: "Design Agency"
     Tags: "vendor", "design-partner"
  -> He pastes the company logo URL from their website
  [analytics: companies_tab_opened, company_count: 0]
  [analytics: company_record_created, has_logo: true, has_website: true,
     tag_count: 2]

Step 2: Bruno links existing contacts to Stellar Labs
  -> On the Stellar Labs company page, he clicks "Add People"
  -> A search overlay appears; he types each name and selects:
     - Natalia Reyes — Role: "Lead Designer", Department: "Product Design"
     - Tomas Becker — Role: "Account Manager", Department: "Sales"
     - Priya Kapoor — Role: "Art Director", Department: "Creative"
  -> Each person appears as a linked contact card on the company profile
  -> LemonDo confirms: "3 people linked"
  [analytics: company_people_linked, count: 3]

Step 3: Bruno links Stellar Labs to a project
  -> He clicks "Link to Project" on the company profile and selects "client-portal-v3"
  -> He sets Stellar Labs' role as "Design Vendor"
  -> A project chip appears on the company page alongside the linked contacts
  [analytics: company_linked_to_project, role: design_vendor]
  [cross-module: projects — see project-management.md for project context]

Step 4: Bruno reviews the company's aggregated communication history
  -> He clicks the "Comms" tab on the Stellar Labs company page
  -> The view aggregates all messages where sender/recipient email matches
     any of the three linked contacts:
     - 7 Gmail threads across Natalia, Tomas, and Priya
     - 2 Slack threads from Priya's shared workspace
  -> Messages are grouped by person with a toggle to show chronologically
  -> Bruno switches to chronological view and immediately sees a message from
     Tomas six weeks ago that he had forgotten — a pricing proposal still
     waiting for a response
  [analytics: company_comms_viewed, linked_message_count: 9, channel_count: 2]
  [analytics: company_comms_view_mode_changed, to: chronological]
  [cross-module: comms — auto-link by email/handle match (CM-013, PP-010)]

Step 5: Bruno adds a company-level note about the relationship
  -> He clicks the "Notes" tab on the company page and adds:
     "Long-term design partner since 2024. Main contact is Natalia for
     execution, Tomas for commercial. Priya's sign-off required for brand work.
     Rate negotiated at flat monthly retainer — renews in June 2026."
  -> He tags the note "commercial" so it's filterable
  [analytics: company_note_created, tag: commercial]

Step 6: Bruno searches across all people and companies to verify discoverability
  -> He uses the People module search bar and types "Stellar"
  -> Results show the Stellar Labs company record and all three linked contacts
     with their roles displayed beneath each name
  -> He also searches "design agency" — Stellar Labs appears via its industry tag
  [analytics: people_search_performed, query_length: 7, result_type: company,
     results_count: 1]
  [analytics: people_search_performed, query_length: 13, result_type: company,
     results_count: 1, match_type: tag]

Step 7: Bruno views an individual contact's profile to confirm company linkage
  -> He clicks Natalia Reyes from the search results
  -> Her profile now shows the Stellar Labs company chip at the top:
     "Stellar Labs — Lead Designer, Product Design"
  -> The timeline on her profile includes the company creation event:
     "Linked to Stellar Labs (2026-02-18)"
  -> Navigating back to Stellar Labs from Natalia's profile takes one click
  [analytics: person_profile_opened, has_company_link: true]
  [analytics: person_to_company_navigated]
```

**Expected Emotion**: Organised and retrospectively satisfied. "I had the pieces scattered for a year. Now there's one place for everything Stellar Labs, and I can see I owe Tomas a reply."

---

## Scenario Coverage Matrix

| FR ID | Requirement | Scenarios |
|-------|-------------|-----------|
| PP-001 | Create and manage person records | S-PP-01 |
| PP-002 | Create and manage company records | S-PP-04 |
| PP-003 | Link people to companies (role, department) | S-PP-04 |
| PP-004 | General information section (bio, role, how we met) | S-PP-01, S-PP-03 |
| PP-005 | Notes/learnings section (timestamped entries) | S-PP-02, S-PP-03 |
| PP-006 | Preferences section (communication style, timezone, tools) | S-PP-02, S-PP-03 |
| PP-007 | Personal vs professional information separation | S-PP-02 |
| PP-008 | Important dates tracking (birthdays, anniversaries, milestones) | S-PP-02, S-PP-03 |
| PP-009 | Link people to projects | S-PP-01, S-PP-03, S-PP-04 |
| PP-010 | Link people to communication messages/threads | S-PP-02, S-PP-04 |
| PP-011 | Tag and categorize people/companies | S-PP-01, S-PP-04 |
| PP-012 | Search across all people and companies | S-PP-01, S-PP-04 |
| PP-013 | Timeline view of all interactions with a person | S-PP-02, S-PP-03 |
| PP-014 | Family/relationship tree for personal contacts | — (P3, deferred — not yet in scope for v2 implementation) |
