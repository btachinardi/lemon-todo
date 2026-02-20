# Administration Context

> **Source**: Extracted from docs/DOMAIN.md §5
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## 5.1 Entities

### AuditEntry (Entity)

```
AuditEntry
├── Id: AuditEntryId (value object)
├── Timestamp: DateTimeOffset
├── ActorId: UserId
├── Action: AuditAction (value object / enum)
├── ResourceType: string (e.g., "User", "Task")
├── ResourceId: string
├── Details: string (JSON, protected-data-redacted)
├── IpAddress: string
├── UserAgent: string
│
└── Invariants:
    ├── Timestamp cannot be in the future
    ├── ActorId must be a valid user
    └── Details must not contain unredacted protected data
```

## 5.2 Value Objects

```
AuditEntryId    -> Guid wrapper
AuditAction     -> Enum: UserRegistered, UserLoggedIn, UserDeactivated, UserReactivated,
                         RoleAssigned, RoleRemoved, ProtectedDataRevealed, ProtectedDataAccessed,
                         TaskCreated, TaskCompleted, TaskDeleted,
                         DataExported, SettingsChanged
ProtectedDataRevealReason -> Enum: SupportTicket, LegalRequest, AccountRecovery,
                                   SecurityInvestigation, DataSubjectRequest,
                                   ComplianceAudit, Other
SystemProtectedDataAccessReason -> Enum: TransactionalEmail, PasswordResetEmail,
                                         AccountVerificationEmail, DataExport, SystemMigration
IProtectedData  -> Marker interface with Redacted property (implemented by Email, DisplayName)
```

## 5.3 Use Cases

```
Commands:
├── RecordAuditEntryCommand      { ActorId, Action, ResourceType, ResourceId, Details, IpAddress }
├── RevealProtectedDataCommand    { UserId, Reason, ReasonDetails?, Comments?, AdminPassword }
│                                  [SystemAdmin only, break-the-glass]
│                                  Flow: validate reason → re-authenticate via password →
│                                        reveal protected data → record structured audit (JSON details)
│                                  If Reason=Other, ReasonDetails is required.

Queries:
├── SearchAuditLogQuery          { DateFrom?, DateTo?, ActorId?, Action?, ResourceType?, Page, PageSize }
├── ListUsersAdminQuery          { Page, PageSize, Search?, Role? } -> PagedResult<AdminUserDto> (protected data redacted)
├── GetSystemHealthQuery         {} -> SystemHealthDto
└── GetUserActivityReportQuery   { UserId, DateFrom, DateTo } -> UserActivityDto
```

## 5.4 Protected Data Handling Services

```
IProtectedDataAccessService
├── AccessForSystemAsync(userId, reason, details?) -> DecryptedProtectedData
│       → Decrypts encrypted columns from Users table
│       → Records AuditAction.ProtectedDataAccessed with null actor (system operation)
│       → Use cases: transactional emails, password resets, data export
├── RevealForAdminAsync(userId) -> DecryptedProtectedData
        → Decrypts encrypted columns from Users table
        → Caller (RevealProtectedDataCommandHandler) records admin-specific audit
        → Requires justification reason + admin password re-auth

ProtectedDataHasher (static utility)
└── HashEmail(email) -> SHA-256 hex string (64 chars uppercase)
        → Normalizes: trim + ToUpperInvariant
        → Used for Identity.UserName (login lookups) and Users.EmailHash (admin searches)

IProtectedData (marker interface)
└── Redacted { get; } property
        → Implemented by Email VO: "j***@example.com" (first char + domain)
        → Implemented by DisplayName VO: "J***n" (first + last char, or "***" if ≤2 chars)
```
