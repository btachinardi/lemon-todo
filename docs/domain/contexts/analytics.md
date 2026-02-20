# Analytics Context

> **Source**: Extracted from docs/DOMAIN.md §7
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## 7.1 Entities

### AnalyticsEvent (Entity, write-only)

```
AnalyticsEvent
├── Id: Guid
├── EventName: string
├── Timestamp: DateTimeOffset
├── HashedUserId: string (SHA-256 of UserId)
├── SessionId: string
├── Properties: Dictionary<string, string>
├── Context: EventContext
```

## 7.2 Value Objects

```
EventContext
├── DeviceType: string (mobile, tablet, desktop)
├── Locale: string
├── Theme: string
├── AppVersion: string
```
