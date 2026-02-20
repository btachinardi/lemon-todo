# Analytics

> **Source**: Extracted from docs/SCENARIOS.md §3, §4, §6, §7
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## North Star Metric

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

## Analytics Measurement Points

### Registration Funnel

```
Landing Page Visit
  -> Click "Sign Up"                    [event: signup_cta_clicked]
  -> Fill Registration Form             [event: registration_form_started]
  -> Submit Registration                [event: registration_submitted]
  -> Email Verified                     [event: email_verified]
  -> Onboarding Started                 [event: onboarding_started]
```

### Onboarding Funnel

```
Onboarding Started
  -> Welcome Screen Viewed              [event: onboarding_welcome_viewed]
  -> First Task Created                 [event: onboarding_first_task_created]
  -> First Task Completed               [event: onboarding_first_task_completed]
  -> Kanban Explored                    [event: onboarding_kanban_explored]
  -> Onboarding Completed              [event: onboarding_completed]
  -> Onboarding Skipped                [event: onboarding_skipped]
```

### Core Usage Events

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

### Engagement Events

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

## Analytics Collection Points Summary

| Lifecycle Stage | Key Events | Purpose |
|----------------|------------|---------|
| Acquisition | `landing_page_viewed`, `signup_cta_clicked` | Measure top of funnel |
| Registration | `registration_submitted`, `email_verified` | Measure conversion |
| Activation | `onboarding_completed`, `first_task_completed` | Measure first value |
| Engagement | `task_created`, `task_completed`, `session_started` | Measure daily usage |
| Retention | `session_started` (D1, D7, D30) | Measure stickiness |
| Feature Adoption | `view_switched`, `pwa_installed`, `theme_toggled` | Measure discovery |
| Churn Prevention | `churn_email_sent`, `re_engagement_success` | Measure recovery |
| Compliance | `admin_panel_opened`, `protected_data_revealed`, `audit_log_searched` | Measure admin usage |

---

## Product Analytics Architecture

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

Note: All protected data is hashed or excluded from analytics events per HIPAA requirements.

---

## Privacy-First Analytics (HIPAA)

From PRD.md §3.2 Analytics Architecture:

- Frontend: Custom analytics service wrapping events
- Backend: Domain events published to analytics processor
- Storage: Separate analytics database (no protected data)
- All user identifiers are hashed (SHA-256)
- All task identifiers are hashed
- No task content (titles, descriptions) in analytics

### Related Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-010.8 | All events follow consistent schema (see above) | P0 |
| FR-010.9 | Events include device context (type, locale, theme) | P1 |
| FR-010.10 | Offline events queued and synced | P1 |
| FR-007.9 | Analytics events must hash all protected data | P0 |
