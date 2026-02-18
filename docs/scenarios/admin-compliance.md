# Admin & Compliance Scenarios

> **Source**: Extracted from docs/SCENARIOS.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Scenario S05: Admin Audit & Compliance (Diana)

**Context**: Diana needs to produce an audit report for the compliance team.

```
Step 1: Diana logs in with her SystemAdmin account
  -> She sees the regular dashboard plus an "Admin" icon in the sidebar
  -> The Admin section is clearly separated from personal task management
  [analytics: session_started, role: system_admin]

Step 2: Diana opens the Admin panel
  -> Tabs: Users | Audit Log | System Health
  -> User list shows names with PROTECTED DATA REDACTED by default (emails masked: s***@example.com)
  -> She can click "Reveal" on individual fields (logged as audit event)
  [analytics: admin_panel_opened]

Step 3: Diana searches the audit log
  -> Filters: Date range, User, Action type, Resource
  -> Searches for "all data access events in January 2026"
  -> Results show: who accessed what, when, from what IP
  -> Protected data in results is redacted (user names/emails masked)
  [analytics: audit_log_searched]

Step 4: Diana reveals specific protected data for the report
  -> She clicks "Reveal" next to a masked email
  -> A confirmation prompt: "Revealing protected data will be logged. Continue?"
  -> She confirms -> email is shown -> event is logged
  [analytics: protected_data_revealed, field: email]

Step 5: Diana reviews system health
  -> Dashboard: active users, API response times, error rates
  -> All metrics from OpenTelemetry/Aspire dashboard
  -> No protected data visible in health metrics
  [analytics: system_health_viewed]
```
