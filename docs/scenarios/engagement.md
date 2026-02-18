# Engagement & Re-engagement Scenarios

> **Source**: Extracted from docs/SCENARIOS.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Scenario S08: Churn Prevention - Inactive User Re-engagement

**Context**: Sarah hasn't opened LemonDo in 5 days.

```
Day 3: No activity detected
  -> System sends email: "Your tasks are waiting for you"
  -> Email shows top 3 incomplete tasks
  -> Single CTA: "Open LemonDo" (deep link to task board)
  [analytics: churn_email_sent, days_inactive: 3]

Day 7: Still no activity
  -> System sends email: "LemonDo misses you! Here's what's pending"
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
