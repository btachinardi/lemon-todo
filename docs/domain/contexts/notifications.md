# Notification Context

> **Source**: Extracted from docs/DOMAIN.md §8
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## 8.1 Entities

### NotificationTemplate (Entity)

```
NotificationTemplate
├── Id: NotificationTemplateId
├── Type: NotificationType (Welcome, ChurnDay3, ChurnDay7, ChurnDay14, WeeklySummary)
├── Subject: LocalizedString
├── Body: LocalizedString (with template variables)
├── Channel: NotificationChannel (Email, InApp, Push)
```

### UserNotification (Entity)

```
UserNotification
├── Id: UserNotificationId
├── UserId: UserId
├── TemplateType: NotificationType
├── Channel: NotificationChannel
├── SentAt: DateTimeOffset
├── ReadAt: DateTimeOffset?
├── Data: Dictionary<string, string> (template variables, protected-data-redacted)
```

## 8.2 Use Cases

```
Commands:
├── SendWelcomeNotificationCommand       { UserId }
├── SendChurnPreventionCommand           { UserId, DaysInactive }
├── SendWeeklySummaryCommand             { UserId }
├── MarkNotificationReadCommand          { NotificationId }

Queries:
├── GetUnreadNotificationsQuery          { UserId } -> List<NotificationDto>
└── GetNotificationHistoryQuery          { UserId, Page, PageSize } -> PagedResult<NotificationDto>
```
