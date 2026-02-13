# LemonDo - User Scenarios & Storyboards

> **Date**: 2026-02-13
> **Purpose**: Define how real users will interact with LemonDo, from their perspective.
> Every scenario maps user intent to expected behavior and analytics measurement.

---

## 1. Personas

### Persona A: "Sarah" - The Overwhelmed Freelancer

- **Age**: 29
- **Role**: Freelance graphic designer
- **Tech Comfort**: High (uses Figma, Notion, Slack daily)
- **Pain Points**:
  - Tasks scattered across sticky notes, emails, and mental notes
  - Loses track of deadlines for multiple clients
  - Existing tools (Trello, Asana) feel too heavy for solo use
- **Goals**: One simple place to dump tasks and see what needs doing today
- **Device**: Primarily iPhone (on the go), MacBook (at desk)
- **Job to Be Done**: "When I have a new task, I want to capture it instantly so I don't forget it."

### Persona B: "Marcus" - The Team Lead

- **Age**: 35
- **Role**: Engineering team lead at a healthcare startup
- **Tech Comfort**: Very high
- **Pain Points**:
  - Needs HIPAA-compliant tools for team task tracking
  - Current tools don't provide audit trails
  - Wants Kanban visualization for sprint planning
- **Goals**: Track team work with compliance guarantees
- **Device**: Desktop primary, tablet for meetings
- **Job to Be Done**: "When I need to track my team's work, I want a compliant board so I can see progress without compliance risk."

### Persona C: "Diana" - The System Administrator

- **Age**: 42
- **Role**: IT administrator at a mid-size company
- **Tech Comfort**: High (infrastructure focus)
- **Pain Points**:
  - Needs complete visibility into who accessed what
  - Must produce compliance reports on demand
  - Worries about data leaks in admin interfaces
- **Goals**: Full platform control with audit capabilities
- **Device**: Desktop only
- **Job to Be Done**: "When compliance asks for an audit report, I want to pull it in seconds so I don't block their review."

---

## 2. Value Proposition

### The Problem Space

Task management tools exist on a spectrum:
- **Too simple** (Apple Notes, Google Keep): No structure, no tracking
- **Too complex** (Jira, Monday.com): Overwhelming for individuals, expensive
- **Not compliant** (Trello, Todoist): No HIPAA, no audit trail

### Our Position

LemonDo sits at the intersection of **simplicity** and **compliance**. We are:
- As simple as Todoist for personal use
- As visual as Trello with Kanban boards
- As compliant as enterprise tools for regulated industries

### Value Proposition Statement

> **For** knowledge workers and regulated organizations
> **Who** need to manage tasks without complexity
> **LemonDo is** a task management platform
> **That** combines consumer-grade UX with enterprise-grade compliance
> **Unlike** Trello, Todoist, or Jira
> **Our product** provides HIPAA-compliant task management with a delightful, mobile-first experience

---

## 3. North Star Metric

### Primary: **Weekly Active Task Completers (WATC)**

A user who completes at least one task in a given week. This metric:
- Measures actual value delivery (tasks getting done)
- Correlates with retention (completing = finding value)
- Is actionable (improve onboarding, reduce friction to complete)
- Is not gameable (requires genuine task management behavior)

### Supporting Metrics

| Metric | Definition | Why It Matters |
|--------|-----------|----------------|
| Activation Rate | % of signups who complete onboarding | Measures first-value delivery |
| Task Velocity | Tasks completed per user per week | Measures engagement depth |
| D7 Retention | % returning after 7 days | Measures product-market fit |
| Kanban Adoption | % of active users using Kanban view | Measures feature discovery |
| Mobile Usage | % of sessions from mobile | Validates mobile-first thesis |

---

## 4. Analytics Measurement Points

### 4.1 Registration Funnel

```
Landing Page Visit
  -> Click "Sign Up"                    [event: signup_cta_clicked]
  -> Fill Registration Form             [event: registration_form_started]
  -> Submit Registration                [event: registration_submitted]
  -> Email Verified                     [event: email_verified]
  -> Onboarding Started                 [event: onboarding_started]
```

### 4.2 Onboarding Funnel

```
Onboarding Started
  -> Welcome Screen Viewed              [event: onboarding_welcome_viewed]
  -> First Task Created                 [event: onboarding_first_task_created]
  -> First Task Completed               [event: onboarding_first_task_completed]
  -> Kanban Explored                    [event: onboarding_kanban_explored]
  -> Onboarding Completed              [event: onboarding_completed]
  -> Onboarding Skipped                [event: onboarding_skipped]
```

### 4.3 Core Usage Events

```
Task Management:
  [event: task_created]                 {priority, has_due_date, has_tags, source: kanban|list}
  [event: task_edited]                  {fields_changed}
  [event: task_completed]               {time_to_complete, view: kanban|list}
  [event: task_deleted]                 {was_completed}
  [event: task_moved]                   {from_column, to_column}

View Switching:
  [event: view_switched]                {from: kanban|list, to: kanban|list}

Search/Filter:
  [event: search_performed]             {query_length, results_count}
  [event: filter_applied]               {filter_type, filter_value}
```

### 4.4 Engagement Events

```
Session:
  [event: session_started]              {device_type, referrer}
  [event: session_ended]                {duration, tasks_completed, tasks_created}

Feature Discovery:
  [event: feature_discovered]           {feature_name, discovery_method}
  [event: theme_toggled]                {to: light|dark}
  [event: language_changed]             {from, to}
  [event: pwa_installed]                {device_type}
```

---

## 5. User Storyboards

### Scenario S01: First-Time Registration (Sarah)

**Context**: Sarah heard about LemonDo from a friend. She opens the app on her iPhone.

```
Step 1: Sarah visits lemondo.app on her phone
  -> She sees a clean landing page with "Get started free" button
  -> She notices the app looks modern and not cluttered
  [analytics: landing_page_viewed, device: mobile]

Step 2: Sarah taps "Get started free"
  -> She sees registration options: Email, Google, GitHub
  -> She chooses Google (fastest for her)
  [analytics: signup_cta_clicked, method: google]

Step 3: Google OAuth flow completes
  -> She's redirected back to LemonDo
  -> Her name and email are pre-filled from Google
  -> She sees a "Welcome, Sarah!" screen
  [analytics: registration_completed, method: google]

Step 4: Onboarding begins
  -> A friendly, minimal overlay says "Let's get you started. Create your first task!"
  -> There's a large, inviting input field with placeholder "What do you need to do?"
  -> There's a subtle "Skip tour" link at the bottom
  [analytics: onboarding_started]

Step 5: Sarah types "Design logo for Acme Corp" and hits Enter
  -> The task appears with a satisfying animation
  -> Confetti or subtle celebration effect
  -> Message: "Your first task! Now let's complete it."
  [analytics: onboarding_first_task_created]

Step 6: Sarah taps the checkmark to complete the task
  -> Strikethrough animation, task moves to "Done"
  -> Celebration: "You did it! You're all set."
  -> CTA: "Explore your board" or "Add more tasks"
  [analytics: onboarding_first_task_completed, onboarding_completed]
```

**Expected Emotion**: Delighted, not overwhelmed. "That was easy."

---

### Scenario S02: Daily Task Management (Sarah)

**Context**: It's Monday morning. Sarah opens LemonDo to plan her day.

```
Step 1: Sarah opens LemonDo (PWA on her phone)
  -> App loads instantly (cached via service worker)
  -> She sees her Kanban board with tasks from last week
  -> "To Do" has 3 tasks, "In Progress" has 1, "Done" has 5
  [analytics: session_started, device: mobile, view: kanban]

Step 2: Sarah adds new tasks quickly
  -> She taps the "+" button at the top
  -> Quick-add: types "Client call at 2pm" -> Enter
  -> Quick-add: types "Invoice Acme Corp" -> Enter
  -> Quick-add: types "Review brand guidelines" -> Enter
  -> Each appears instantly in "To Do"
  [analytics: task_created x3, source: quick_add]

Step 3: Sarah prioritizes by dragging
  -> She long-presses "Client call at 2pm" and drags it to "In Progress"
  -> She sets "Invoice Acme Corp" to High priority via a quick-tap on priority icon
  [analytics: task_moved, task_edited]

Step 4: During the day, Sarah completes tasks
  -> After each client call, she swipes the task to complete
  -> Satisfying completion animation each time
  -> End of day: 4 tasks completed
  [analytics: task_completed x4]

Step 5: Sarah switches to list view for a quick overview
  -> She taps the list icon in the top bar
  -> Sees all tasks sorted by priority
  -> Filters to show only "To Do" tasks
  [analytics: view_switched, filter_applied]
```

**Expected Emotion**: Productive, in control. "I can see everything I need."

---

### Scenario S03: Email Registration & Password Flow (New User)

**Context**: A user without Google/GitHub accounts registers with email.

```
Step 1: User clicks "Sign up with email"
  -> Form: Name, Email, Password (with strength indicator)
  -> Password requirements shown inline (not as error)
  [analytics: registration_form_started, method: email]

Step 2: User submits the form
  -> Loading spinner on button (no page reload)
  -> Success: "Check your email for a verification link"
  -> Email sent within 5 seconds
  [analytics: registration_submitted, method: email]

Step 3: User clicks verification link in email
  -> Opens LemonDo in browser
  -> "Email verified! Let's get started."
  -> Redirected to onboarding
  [analytics: email_verified]

Step 4: User forgets password later
  -> Clicks "Forgot password?" on login
  -> Enters email -> receives reset link
  -> Sets new password -> logged in automatically
  [analytics: password_reset_requested, password_reset_completed]
```

---

### Scenario S04: Kanban Power User (Marcus)

**Context**: Marcus manages his team's sprint using the Kanban board on his desktop.

```
Step 1: Marcus logs in on desktop
  -> Full-width Kanban board fills the screen
  -> Columns: Backlog | To Do | In Progress | In Review | Done
  -> Tasks show priority badges, due dates, and tags
  [analytics: session_started, device: desktop, view: kanban]

Step 2: Marcus creates tasks with full details
  -> Clicks "+" on "Backlog" column
  -> Full task form: title, description (markdown), priority, due date, tags
  -> Adds "Implement patient data API" with High priority, tags: [backend, sprint-12]
  [analytics: task_created, has_description: true, has_due_date: true, has_tags: true]

Step 3: Marcus drags tasks into sprint columns
  -> Moves 5 tasks from Backlog to To Do
  -> Drag-and-drop is smooth, with drop zone indicators
  -> Column card count updates in real-time
  [analytics: task_moved x5]

Step 4: Marcus uses search and filters
  -> Types "patient" in search bar
  -> Results highlight matching tasks across all columns
  -> Filters by tag "backend" to see only backend tasks
  [analytics: search_performed, filter_applied]

Step 5: Marcus switches to list view for reporting
  -> List view shows all tasks with their columns as a "Status" field
  -> Sorts by due date to see what's coming up
  -> Exports view (future feature) for standup notes
  [analytics: view_switched, to: list]
```

---

### Scenario S05: Admin Audit & Compliance (Diana)

**Context**: Diana needs to produce an audit report for the compliance team.

```
Step 1: Diana logs in with her SystemAdmin account
  -> She sees the regular dashboard plus an "Admin" icon in the sidebar
  -> The Admin section is clearly separated from personal task management
  [analytics: session_started, role: system_admin]

Step 2: Diana opens the Admin panel
  -> Tabs: Users | Audit Log | System Health
  -> User list shows names with PII REDACTED by default (emails masked: s***@example.com)
  -> She can click "Reveal" on individual fields (logged as audit event)
  [analytics: admin_panel_opened]

Step 3: Diana searches the audit log
  -> Filters: Date range, User, Action type, Resource
  -> Searches for "all data access events in January 2026"
  -> Results show: who accessed what, when, from what IP
  -> PII in results is redacted (user names/emails masked)
  [analytics: audit_log_searched]

Step 4: Diana reveals specific PII for the report
  -> She clicks "Reveal" next to a masked email
  -> A confirmation prompt: "Revealing PII will be logged. Continue?"
  -> She confirms -> email is shown -> event is logged
  [analytics: pii_revealed, field: email]

Step 5: Diana reviews system health
  -> Dashboard: active users, API response times, error rates
  -> All metrics from OpenTelemetry/Aspire dashboard
  -> No PII visible in health metrics
  [analytics: system_health_viewed]
```

---

### Scenario S06: Mobile Offline Usage (Sarah)

**Context**: Sarah is on a flight with no internet. She needs to review and add tasks.

```
Step 1: Sarah opens LemonDo (PWA) on her phone
  -> App loads from service worker cache
  -> A subtle banner: "You're offline. Changes will sync when you're back online."
  -> All previously loaded tasks are visible
  [analytics: session_started, connectivity: offline]

Step 2: Sarah adds tasks while offline
  -> Quick-add works normally
  -> Tasks appear in the UI with a subtle "pending sync" indicator
  -> She adds 3 tasks for the week ahead

Step 3: Sarah completes a task offline
  -> Taps checkmark -> task animates to done
  -> Subtle sync indicator remains

Step 4: Sarah's flight lands, phone reconnects
  -> LemonDo background syncs automatically
  -> "Pending sync" indicators disappear
  -> All changes are now on the server
  [analytics: offline_sync_completed, items_synced: 4]
```

---

### Scenario S07: Theme and Language Switching

**Context**: A user in Brazil wants the app in Portuguese with dark mode.

```
Step 1: User opens Settings
  -> Settings page with sections: Profile, Appearance, Language, Notifications
  [analytics: settings_opened]

Step 2: User switches to Dark mode
  -> Toggle switch: Light | Dark | System
  -> Instant theme change with smooth transition
  -> All components properly themed (no white flashes)
  [analytics: theme_toggled, to: dark]

Step 3: User changes language to Portuguese
  -> Language dropdown with: English, Portugues, Espanol
  -> On selection, ALL UI text updates immediately (no page reload)
  -> Dates, numbers format to pt-BR locale
  [analytics: language_changed, from: en, to: pt-BR]
```

---

### Scenario S08: Churn Prevention - Inactive User Re-engagement

**Context**: Sarah hasn't opened LemonDo in 5 days.

```
Day 3: No activity detected
  -> System sends email: "Your tasks are waiting for you"
  -> Email shows top 3 incomplete tasks
  -> Single CTA: "Open LemonDo" (deep link to task board)
  [analytics: churn_email_sent, days_inactive: 3]

Day 7: Still no activity
  -> System sends email: "We miss you! Here's what's pending"
  -> Shows task count, next due date
  -> Highlights a new feature they haven't tried
  [analytics: churn_email_sent, days_inactive: 7]

Day 14: Still no activity
  -> Final gentle email: "Your tasks are still here"
  -> No pressure, friendly tone
  -> Option to adjust notification preferences
  [analytics: churn_email_sent, days_inactive: 14]

Re-engagement: Sarah opens from email link
  -> Deep link takes her directly to her task board
  -> A warm "Welcome back!" toast notification
  -> Her tasks are exactly as she left them
  [analytics: re_engagement_success, inactive_days: 7, source: email]
```

---

### Scenario S09: Multi-Factor Authentication Setup

**Context**: Marcus's company requires MFA for all accounts.

```
Step 1: Marcus goes to Settings > Security
  -> "Two-Factor Authentication" section with "Enable" button
  [analytics: settings_security_opened]

Step 2: Marcus enables MFA
  -> QR code displayed for authenticator app
  -> Manual key also shown for copy-paste
  -> Input field to verify TOTP code
  [analytics: mfa_setup_started]

Step 3: Marcus scans QR code and enters verification code
  -> Code verified -> MFA enabled
  -> Backup codes generated and shown ONCE
  -> Prompt to save/print backup codes
  [analytics: mfa_enabled]

Step 4: Next login requires MFA
  -> Email + password entered
  -> Second screen: "Enter your 2FA code"
  -> Code from authenticator app accepted
  -> Login complete
  [analytics: login_completed, mfa: true]
```

---

### Scenario S10: First-Time Setup to Productive User (Full Journey)

**Context**: Complete journey from discovery to being a weekly active user.

```
Week 0 - Day 1: Discovery & Signup
  -> Finds LemonDo via recommendation
  -> Signs up with Google OAuth (30 seconds)
  -> Completes onboarding (2 minutes)
  -> Creates 5 tasks
  -> Completes 2 tasks
  -> Feels satisfied: "This is what I needed"
  [north_star: first task completed]

Week 0 - Day 2-3: Habit Formation
  -> Opens app in morning (via PWA on phone)
  -> Adds daily tasks
  -> Discovers priority system
  -> Starts using High/Medium/Low consistently
  [analytics: feature_discovered: priorities]

Week 1: Feature Exploration
  -> Tries Kanban view, finds it useful for project work
  -> Uses list view for quick daily planning
  -> Installs PWA on phone
  -> Switches to dark mode
  [analytics: pwa_installed, theme_toggled, view_switched]

Week 2: Sustained Usage
  -> Opens LemonDo daily
  -> Completing 3-5 tasks per day
  -> Creates tasks from email reminders
  -> Tells a friend about LemonDo
  [north_star: weekly_active_task_completer = true]

Month 2: Power User
  -> Has 50+ completed tasks
  -> Uses tags for different projects
  -> Kanban board is primary view
  -> Recommends to 2 colleagues
  [analytics: referral_sent x2]
```

---

## 6. Analytics Collection Points Summary

| Lifecycle Stage | Key Events | Purpose |
|----------------|------------|---------|
| Acquisition | `landing_page_viewed`, `signup_cta_clicked` | Measure top of funnel |
| Registration | `registration_submitted`, `email_verified` | Measure conversion |
| Activation | `onboarding_completed`, `first_task_completed` | Measure first value |
| Engagement | `task_created`, `task_completed`, `session_started` | Measure daily usage |
| Retention | `session_started` (D1, D7, D30) | Measure stickiness |
| Feature Adoption | `view_switched`, `pwa_installed`, `theme_toggled` | Measure discovery |
| Churn Prevention | `churn_email_sent`, `re_engagement_success` | Measure recovery |
| Compliance | `admin_panel_opened`, `pii_revealed`, `audit_log_searched` | Measure admin usage |

---

## 7. Product Analytics Architecture

```
Frontend Events                    Backend Events
     |                                  |
     v                                  v
  [Analytics Service]            [Domain Events]
     |                                  |
     +---->  [Event Bus / Queue] <------+
                    |
                    v
          [Analytics Storage]
                    |
                    v
          [Dashboard / Reports]
```

Events follow a consistent schema:
```json
{
  "event": "task_completed",
  "timestamp": "2026-02-13T10:30:00Z",
  "userId": "hashed_user_id",
  "sessionId": "session_uuid",
  "properties": {
    "taskId": "hashed_task_id",
    "view": "kanban",
    "timeToComplete": 86400
  },
  "context": {
    "device": "mobile",
    "locale": "en-US",
    "theme": "dark"
  }
}
```

Note: All PII is hashed or excluded from analytics events per HIPAA requirements.
