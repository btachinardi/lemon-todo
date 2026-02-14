# LemonDo - Domain Design

> **Date**: 2026-02-13
> **Status**: Active
> **Architecture**: Domain-Driven Design (DDD)
> **Pattern**: CQRS-light with Use Cases
> **Informed By**: [PRD.md](./PRD.md), [SCENARIOS.md](./SCENARIOS.md)

---

## 1. Bounded Contexts

```
+-------------------+     +-------------------+     +-------------------+
|     Identity      |     |  Task Management  |     |   Administration  |
|   (Auth + RBAC)   |<--->|   (Core Domain)   |<--->|   (Audit + Admin) |
+-------------------+     +-------------------+     +-------------------+
         |                         |                         |
         v                         v                         v
+-------------------+     +-------------------+     +-------------------+
|    Onboarding     |     |    Analytics       |     |   Notification    |
|   (User Journey)  |     |   (Measurement)   |     |  (Communication)  |
+-------------------+     +-------------------+     +-------------------+
```

### Context Map

| Context | Type | Responsibility |
|---------|------|----------------|
| **Identity** | Core | User registration, authentication, authorization, roles |
| **Task Management** | Core | Task CRUD, Kanban boards, list views, task lifecycle |
| **Administration** | Supporting | Audit logs, user management, system health, PII handling |
| **Onboarding** | Supporting | User journey tracking, guided tours, progress tracking |
| **Analytics** | Generic | Event collection, funnel tracking, metrics aggregation |
| **Notification** | Generic | Email sending, in-app notifications, push notifications |

### Context Relationships

| Upstream | Downstream | Relationship |
|----------|------------|--------------|
| Identity | Task Management | Conformist (tasks reference user IDs) |
| Identity | Administration | Conformist (admin views user data) |
| Identity | Onboarding | Customer-Supplier (onboarding tracks identity events) |
| Task Management | Analytics | Published Language (domain events -> analytics events) |
| Task Management | Onboarding | Published Language (task events -> onboarding progress) |
| Identity | Notification | Customer-Supplier (user data for email) |
| Administration | Notification | Customer-Supplier (alerts, reports) |

---

## 2. Identity Context

### 2.1 Entities

#### User (Aggregate Root)

```
User
├── Id: UserId (value object)
├── Email: Email (value object)
├── DisplayName: DisplayName (value object)
├── PasswordHash: string (nullable, for email auth)
├── Roles: IReadOnlyList<Role>
├── MfaEnabled: bool
├── MfaSecret: string? (encrypted)
├── IsEmailVerified: bool
├── IsActive: bool
├── LockoutEnd: DateTimeOffset?
├── FailedLoginAttempts: int
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
├── LastLoginAt: DateTimeOffset?
│
├── Methods:
│   ├── Register(email, displayName, passwordHash?) -> UserRegisteredEvent
│   ├── VerifyEmail() -> EmailVerifiedEvent
│   ├── Login(passwordHash) -> Result<LoginSucceededEvent, LoginFailedEvent>
│   ├── AssignRole(role) -> RoleAssignedEvent
│   ├── RemoveRole(role) -> RoleRemovedEvent
│   ├── EnableMfa(secret) -> MfaEnabledEvent
│   ├── DisableMfa() -> MfaDisabledEvent
│   ├── Deactivate() -> UserDeactivatedEvent
│   ├── Reactivate() -> UserReactivatedEvent
│   ├── RecordFailedLogin() -> AccountLockedEvent?
│   └── ResetPassword(newPasswordHash) -> PasswordResetEvent
│
└── Invariants:
    ├── Email must be valid format
    ├── DisplayName must be 1-100 characters
    ├── Cannot assign duplicate roles
    ├── Cannot login if deactivated
    ├── Lockout after 5 failed attempts for 15 minutes
    └── MFA secret must be set before enabling MFA
```

#### Role (Entity)

```
Role
├── Id: RoleId
├── Name: RoleName (value object: User, Admin, SystemAdmin)
├── Permissions: IReadOnlySet<Permission>
│
└── Invariants:
    ├── Name must be one of predefined values
    └── SystemAdmin inherits all Admin permissions
```

### 2.2 Value Objects

```
UserId          -> Guid wrapper
Email           -> Validated email string (RFC 5322)
DisplayName     -> Non-empty string, 1-100 chars, trimmed
RoleName        -> Enum: User, Admin, SystemAdmin
Permission      -> Enum: ManageTasks, ViewAuditLog, ManageUsers, RevealPii, ViewSystemHealth
RefreshToken    -> Token string + expiration
```

### 2.3 Domain Events

```
UserRegisteredEvent         { UserId, Email, Method: Email|Google|GitHub }
EmailVerifiedEvent          { UserId }
LoginSucceededEvent         { UserId, Method, IpAddress }
LoginFailedEvent            { Email, IpAddress, Reason }
AccountLockedEvent          { UserId, LockoutEnd }
RoleAssignedEvent           { UserId, RoleName, AssignedBy }
RoleRemovedEvent            { UserId, RoleName, RemovedBy }
MfaEnabledEvent             { UserId }
MfaDisabledEvent            { UserId }
UserDeactivatedEvent        { UserId, DeactivatedBy }
PasswordResetEvent          { UserId }
```

### 2.4 Use Cases

```
Commands:
├── RegisterUserCommand          { Email, DisplayName, Password?, OAuthProvider? }
├── VerifyEmailCommand           { Token }
├── LoginCommand                 { Email, Password }
├── LoginWithOAuthCommand        { Provider, OAuthCode }
├── RefreshTokenCommand          { RefreshToken }
├── RequestPasswordResetCommand  { Email }
├── ResetPasswordCommand         { Token, NewPassword }
├── AssignRoleCommand            { UserId, RoleName }  [SystemAdmin only]
├── RemoveRoleCommand            { UserId, RoleName }  [SystemAdmin only]
├── EnableMfaCommand             { UserId, TotpCode }
├── DisableMfaCommand            { UserId, TotpCode }
├── DeactivateUserCommand        { UserId }            [SystemAdmin only]

Queries:
├── GetCurrentUserQuery          {} -> UserDto
├── GetUserByIdQuery             { UserId } -> UserDto  [Admin+]
├── ListUsersQuery               { Page, PageSize, Search? } -> PagedResult<UserDto>  [Admin+]
└── ValidateMfaCodeQuery         { UserId, TotpCode } -> bool
```

### 2.5 Repository Interface

```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct);
    Task<User?> GetByEmailAsync(Email email, CancellationToken ct);
    Task<IReadOnlyList<User>> ListAsync(int page, int pageSize, string? search, CancellationToken ct);
    Task<int> CountAsync(string? search, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task UpdateAsync(User user, CancellationToken ct);
}
```

---

## 3. Task Management Context

### 3.1 Design Principles

1. **Column determines status** — A column declares a `TargetStatus` (Todo, InProgress, Done). When a task is placed in a column, it inherits that status. One source of truth.
2. **Status changes require column transitions** — You cannot set a task's status directly. `MoveTo()` is the single method for all column/status transitions.
3. **Archive is a visibility flag, NOT a lifecycle status** — `IsArchived` is a boolean orthogonal to board position. A task stays `Done` when archived.
4. **Complete/Uncomplete are convenience routes** — The application layer resolves them to `MoveTo()` calls targeting the board's Done/Todo column.

### 3.2 Entities

#### BoardTask (Aggregate Root)

```
BoardTask
├── Id: BoardTaskId (value object)
├── OwnerId: UserId (value object, from Identity context)
├── Title: TaskTitle (value object)
├── Description: TaskDescription? (value object)
├── Priority: Priority (value object / enum)
├── Status: BoardTaskStatus (derived from column's TargetStatus)
├── DueDate: DateTimeOffset?
├── Tags: IReadOnlyList<Tag>
├── ColumnId: ColumnId (required, non-nullable)
├── Position: int (sort order within column)
├── IsArchived: bool (visibility flag, orthogonal to status)
├── IsDeleted: bool (soft delete flag)
├── CompletedAt: DateTimeOffset? (set when status becomes Done)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, columnId, position, initialStatus, title, description?, priority?, dueDate?, tags?)
│   │       -> TaskCreatedEvent
│   ├── UpdateTitle(title) -> TaskUpdatedEvent
│   ├── UpdateDescription(description) -> TaskUpdatedEvent
│   ├── SetPriority(priority) -> TaskPriorityChangedEvent
│   ├── SetDueDate(dueDate) -> TaskDueDateChangedEvent
│   ├── AddTag(tag) -> TaskTagAddedEvent
│   ├── RemoveTag(tag) -> TaskTagRemovedEvent
│   ├── MoveTo(columnId, position, targetStatus)
│   │       -> TaskMovedEvent + TaskStatusChangedEvent (if status changes)
│   ├── Archive() -> TaskArchivedEvent (requires Status == Done)
│   ├── Unarchive() -> TaskUnarchivedEvent
│   └── Delete() -> TaskDeletedEvent (soft delete)
│
└── Invariants:
    ├── Title must be 1-500 characters
    ├── Description must be 0-10000 characters
    ├── ColumnId is required (task always belongs to a column)
    ├── Status is always consistent with the column's TargetStatus
    ├── Cannot archive a non-completed task (Status must be Done)
    ├── Cannot edit a deleted task
    ├── Tags are unique per task (no duplicates)
    ├── Position must be >= 0
    ├── OwnerId cannot change after creation
    ├── CompletedAt is set when status transitions to Done
    ├── CompletedAt is cleared when status transitions away from Done
    └── IsArchived is cleared when moving away from Done
```

#### Board (Aggregate Root)

```
Board
├── Id: BoardId (value object)
├── OwnerId: UserId
├── Name: BoardName (value object)
├── Columns: IReadOnlyList<Column> (ordered by position)
├── CreatedAt: DateTimeOffset
│
├── Methods:
│   ├── CreateDefault(ownerId) -> BoardCreatedEvent (3 columns: To Do, In Progress, Done)
│   ├── Create(ownerId, name) -> BoardCreatedEvent (2 columns: To Do, Done)
│   ├── GetInitialColumn() -> Column (first Todo column by position)
│   ├── GetDoneColumn() -> Column (first Done column by position)
│   ├── FindColumnById(columnId) -> Column? (lookup for validation)
│   ├── AddColumn(name, targetStatus, position?) -> ColumnAddedEvent
│   ├── RemoveColumn(columnId) -> ColumnRemovedEvent
│   ├── ReorderColumn(columnId, newPosition) -> ColumnReorderedEvent
│   └── RenameColumn(columnId, name) -> ColumnRenamedEvent
│
└── Invariants:
    ├── Must have at least one column with TargetStatus = Todo
    ├── Must have at least one column with TargetStatus = Done
    ├── Default board has 3 columns: To Do (Todo), In Progress (InProgress), Done (Done)
    ├── Custom board starts with at least To Do + Done columns
    ├── Column names must be unique within a board (case-insensitive)
    └── Cannot remove the last column targeting a required status (Todo or Done)
```

#### Column (Entity, owned by Board)

```
Column
├── Id: ColumnId (value object)
├── Name: ColumnName (value object)
├── TargetStatus: BoardTaskStatus (the status tasks get when placed here)
├── Position: int
├── MaxTasks: int? (null = unlimited)
│
└── Invariants:
    ├── Name must be 1-50 characters
    ├── Position must be >= 0
    ├── MaxTasks must be > 0 if set
    └── TargetStatus is immutable after creation
```

### 3.3 Value Objects

```
BoardTaskId     -> Guid wrapper
BoardId         -> Guid wrapper
ColumnId        -> Guid wrapper
TaskTitle       -> Non-empty string, 1-500 chars, trimmed
TaskDescription -> String, 0-10000 chars
BoardName       -> Non-empty string, 1-100 chars
ColumnName      -> Non-empty string, 1-50 chars
Tag             -> Non-empty string, 1-50 chars, lowercase, trimmed
Priority        -> Enum: None, Low, Medium, High, Critical
BoardTaskStatus -> Enum: Todo, InProgress, Done (NO Archived — archive is a visibility flag)
```

### 3.4 Domain Events

```
TaskCreatedEvent            { BoardTaskId, OwnerId, Title, Priority, ColumnId, Position, InitialStatus }
TaskUpdatedEvent            { BoardTaskId, FieldName, OldValue, NewValue }
TaskPriorityChangedEvent    { BoardTaskId, OldPriority, NewPriority }
TaskDueDateChangedEvent     { BoardTaskId, OldDueDate?, NewDueDate? }
TaskTagAddedEvent           { BoardTaskId, Tag }
TaskTagRemovedEvent         { BoardTaskId, Tag }
TaskMovedEvent              { BoardTaskId, FromColumnId, ToColumnId, NewPosition }
TaskStatusChangedEvent      { BoardTaskId, OldStatus, NewStatus }
TaskArchivedEvent           { BoardTaskId }
TaskUnarchivedEvent         { BoardTaskId }
TaskDeletedEvent            { BoardTaskId, DeletedAt }
BoardCreatedEvent           { BoardId, OwnerId }
ColumnAddedEvent            { BoardId, ColumnId, ColumnName }
ColumnRemovedEvent          { BoardId, ColumnId }
ColumnReorderedEvent        { BoardId, ColumnId, OldPosition, NewPosition }
ColumnRenamedEvent          { BoardId, ColumnId, Name }
```

### 3.5 Use Cases

```
Commands:
├── CreateTaskCommand            { Title, Description?, Priority?, DueDate?, Tags? }
│       → Resolves board's initial (Todo) column, creates task at end of column
├── UpdateTaskCommand            { BoardTaskId, Title?, Description?, Priority?, DueDate? }
├── AddTagToTaskCommand          { BoardTaskId, Tag }
├── RemoveTagFromTaskCommand     { BoardTaskId, Tag }
├── MoveTaskCommand              { BoardTaskId, ColumnId, Position }
│       → Validates column exists on board, derives status from column's TargetStatus
├── CompleteTaskCommand          { BoardTaskId }
│       → Resolves board's Done column, calls MoveTo with Done status
├── UncompleteTaskCommand        { BoardTaskId }
│       → Resolves board's Todo column, calls MoveTo with Todo status
├── ArchiveTaskCommand           { BoardTaskId }
├── DeleteTaskCommand            { BoardTaskId }
├── AddColumnCommand             { BoardId, Name, TargetStatus, Position? }
├── RemoveColumnCommand          { BoardId, ColumnId }
├── ReorderColumnCommand         { BoardId, ColumnId, NewPosition }
├── RenameColumnCommand          { BoardId, ColumnId, Name }
└── BulkCompleteTasksCommand     { BoardTaskIds }

Queries:
├── GetTaskByIdQuery             { BoardTaskId } -> BoardTaskDto
├── ListTasksQuery               { Status?, Priority?, Search?, Page, PageSize } -> PagedResult<BoardTaskDto>
├── GetBoardQuery                { BoardId } -> BoardDto
├── GetDefaultBoardQuery         {} -> BoardDto (auto-creates if missing)
└── GetTasksByColumnQuery        { ColumnId, Page, PageSize } -> PagedResult<BoardTaskDto>
```

### 3.6 Repository Interfaces

```csharp
public interface IBoardTaskRepository
{
    Task<BoardTask?> GetByIdAsync(BoardTaskId id, CancellationToken ct);
    Task<IReadOnlyList<BoardTask>> GetByColumnAsync(ColumnId columnId, CancellationToken ct);
    Task<PagedResult<BoardTask>> ListAsync(
        UserId ownerId, ColumnId? columnId, Priority? priority,
        BoardTaskStatus? status, string? searchTerm,
        int page, int pageSize, CancellationToken ct);
    Task AddAsync(BoardTask task, CancellationToken ct);
    Task UpdateAsync(BoardTask task, CancellationToken ct);
}

public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct);
    Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct);
    Task AddAsync(Board board, CancellationToken ct);
    Task UpdateAsync(Board board, CancellationToken ct);
}
```

---

## 4. Administration Context

### 4.1 Entities

#### AuditEntry (Entity)

```
AuditEntry
├── Id: AuditEntryId (value object)
├── Timestamp: DateTimeOffset
├── ActorId: UserId
├── Action: AuditAction (value object / enum)
├── ResourceType: string (e.g., "User", "TaskItem")
├── ResourceId: string
├── Details: string (JSON, PII-redacted)
├── IpAddress: string
├── UserAgent: string
│
└── Invariants:
    ├── Timestamp cannot be in the future
    ├── ActorId must be a valid user
    └── Details must not contain unredacted PII
```

### 4.2 Value Objects

```
AuditEntryId    -> Guid wrapper
AuditAction     -> Enum: UserRegistered, UserLoggedIn, UserDeactivated,
                         RoleAssigned, RoleRemoved, PiiRevealed,
                         TaskCreated, TaskCompleted, TaskDeleted,
                         DataExported, SettingsChanged
RedactedString  -> Wrapper that stores original (encrypted) + masked version
```

### 4.3 Use Cases

```
Commands:
├── RecordAuditEntryCommand      { ActorId, Action, ResourceType, ResourceId, Details, IpAddress }
├── RevealPiiCommand             { FieldType, ResourceId }  [SystemAdmin only, creates audit entry]

Queries:
├── SearchAuditLogQuery          { DateFrom?, DateTo?, ActorId?, Action?, ResourceType?, Page, PageSize }
├── ListUsersAdminQuery          { Page, PageSize, Search?, Role? } -> PagedResult<AdminUserDto> (PII redacted)
├── GetSystemHealthQuery         {} -> SystemHealthDto
└── GetUserActivityReportQuery   { UserId, DateFrom, DateTo } -> UserActivityDto
```

### 4.4 PII Redaction Service

```
IPiiRedactionService
├── RedactEmail(email) -> "s***@example.com"
├── RedactName(name) -> "S*** L***"
├── RedactForLog(object) -> object with all PII fields redacted
├── RevealField(encryptedValue, adminId) -> string + AuditEntry
```

---

## 5. Onboarding Context

### 5.1 Entities

#### OnboardingProgress (Aggregate Root)

```
OnboardingProgress
├── Id: OnboardingProgressId
├── UserId: UserId
├── Steps: IReadOnlyList<OnboardingStep>
├── IsCompleted: bool
├── IsSkipped: bool
├── StartedAt: DateTimeOffset
├── CompletedAt: DateTimeOffset?
│
├── Methods:
│   ├── Start() -> OnboardingStartedEvent
│   ├── CompleteStep(stepType) -> OnboardingStepCompletedEvent
│   ├── Skip() -> OnboardingSkippedEvent
│   └── Complete() -> OnboardingCompletedEvent
│
└── Invariants:
    ├── Steps must be completed in order
    ├── Cannot complete if already completed or skipped
    └── Cannot skip if already completed
```

### 5.2 Value Objects

```
OnboardingStep
├── Type: OnboardingStepType (WelcomeViewed, FirstTaskCreated, FirstTaskCompleted, KanbanExplored)
├── CompletedAt: DateTimeOffset?
├── IsCompleted: bool
```

---

## 6. Analytics Context

### 6.1 Entities

#### AnalyticsEvent (Entity, write-only)

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

### 6.2 Value Objects

```
EventContext
├── DeviceType: string (mobile, tablet, desktop)
├── Locale: string
├── Theme: string
├── AppVersion: string
```

---

## 7. Notification Context

### 7.1 Entities

#### NotificationTemplate (Entity)

```
NotificationTemplate
├── Id: NotificationTemplateId
├── Type: NotificationType (Welcome, ChurnDay3, ChurnDay7, ChurnDay14, WeeklySummary)
├── Subject: LocalizedString
├── Body: LocalizedString (with template variables)
├── Channel: NotificationChannel (Email, InApp, Push)
```

#### UserNotification (Entity)

```
UserNotification
├── Id: UserNotificationId
├── UserId: UserId
├── TemplateType: NotificationType
├── Channel: NotificationChannel
├── SentAt: DateTimeOffset
├── ReadAt: DateTimeOffset?
├── Data: Dictionary<string, string> (template variables, PII-redacted)
```

### 7.2 Use Cases

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

---

## 8. Shared Kernel

Types shared across all bounded contexts:

```
UserId              -> Guid wrapper (shared identity)
DateTimeOffset      -> All timestamps in UTC
Result<T, E>        -> Discriminated union for operation results
PagedResult<T>      -> { Items, TotalCount, Page, PageSize }
DomainEvent         -> Base class for all domain events
Entity<TId>         -> Base class with Id, CreatedAt, UpdatedAt
ValueObject         -> Base class with structural equality
```

---

## 9. Entity Relationship Diagram

```
+----------+       +------------+       +-----------+
|   User   |1---N  | BoardTask  |N---1  |  Column   |
+----------+       +------------+       +-----------+
| Id       |       | Id         |       | Id        |
| Email    |       | OwnerId   |----+  | Name      |
| Name     |       | Title      |    |  | TargetSt. |
| Roles[]  |       | Priority   |    |  | Pos       |
+----------+       | Status     |    |  | MaxTasks  |
     |             | ColumnId  |----+  +-----------+
     |             | IsArchived |           |
     |             | Tags[]    |       +--------+
     |             +------------+       | Board  |
     |                                  +--------+
     |1---N  +------------------+       | Id     |
     +------>| AuditEntry       |       | Name   |
     |       +------------------+       | Cols[] |
     |       | Id               |       +--------+
     |       | ActorId          |
     |       | Action           |
     |       | ResourceType     |
     |       | Details (redacted)|
     |       +------------------+
     |
     |1---1  +--------------------+
     +------>| OnboardingProgress |
     |       +--------------------+
     |       | UserId             |
     |       | Steps[]            |
     |       | IsCompleted        |
     |       +--------------------+
     |
     |1---N  +------------------+
     +------>| UserNotification |
             +------------------+
             | UserId           |
             | TemplateType     |
             | SentAt           |
             +------------------+
```

---

## 10. API Endpoint Design

### Identity Endpoints

```
POST   /api/auth/register              Register new user
POST   /api/auth/login                 Login with email/password
POST   /api/auth/login/oauth/{provider} OAuth login
POST   /api/auth/refresh               Refresh JWT token
POST   /api/auth/verify-email          Verify email with token
POST   /api/auth/forgot-password       Request password reset
POST   /api/auth/reset-password        Reset password with token
POST   /api/auth/mfa/enable            Enable MFA
POST   /api/auth/mfa/disable           Disable MFA
POST   /api/auth/mfa/verify            Verify MFA code during login
GET    /api/auth/me                    Get current user profile
PUT    /api/auth/me                    Update current user profile
```

### Task Management Endpoints

```
GET    /api/tasks                      List tasks (with filters/pagination)
POST   /api/tasks                      Create task
GET    /api/tasks/{id}                 Get task by ID
PUT    /api/tasks/{id}                 Update task
DELETE /api/tasks/{id}                 Soft-delete task
POST   /api/tasks/{id}/complete        Complete task
POST   /api/tasks/{id}/uncomplete      Uncomplete task
POST   /api/tasks/{id}/archive         Archive task
POST   /api/tasks/{id}/move            Move task to column/position
POST   /api/tasks/{id}/tags            Add tag
DELETE /api/tasks/{id}/tags/{tag}      Remove tag
POST   /api/tasks/bulk/complete        Bulk complete tasks

GET    /api/boards                     List user's boards
POST   /api/boards                     Create board
GET    /api/boards/{id}                Get board with columns and tasks
GET    /api/boards/default             Get default board
POST   /api/boards/{id}/columns        Add column
PUT    /api/boards/{id}/columns/{colId} Rename column
DELETE /api/boards/{id}/columns/{colId} Remove column
POST   /api/boards/{id}/columns/reorder Reorder columns
```

### Administration Endpoints

```
GET    /api/admin/users                List users (PII redacted)    [Admin+]
GET    /api/admin/users/{id}           Get user detail (redacted)    [Admin+]
POST   /api/admin/users/{id}/reveal    Reveal PII field             [SystemAdmin]
POST   /api/admin/users/{id}/roles     Assign role                  [SystemAdmin]
DELETE /api/admin/users/{id}/roles/{r}  Remove role                  [SystemAdmin]
POST   /api/admin/users/{id}/deactivate Deactivate user             [SystemAdmin]

GET    /api/admin/audit                Search audit log             [Admin+]
GET    /api/admin/health               System health                [Admin+]
GET    /api/admin/reports/activity     User activity report         [Admin+]
```

### Onboarding Endpoints

```
GET    /api/onboarding/progress        Get onboarding progress
POST   /api/onboarding/step/{type}     Complete onboarding step
POST   /api/onboarding/skip            Skip onboarding
```

### Notification Endpoints

```
GET    /api/notifications              List user notifications
POST   /api/notifications/{id}/read    Mark notification as read
```

### System Endpoints

```
GET    /health                         Health check
GET    /ready                          Readiness check
GET    /scalar/v1                      API documentation
```

---

## 11. Cross-Cutting Concerns

### 11.1 Authentication Middleware

All endpoints except `/api/auth/register`, `/api/auth/login`, `/api/auth/login/oauth/*`, `/health`, `/ready`, and `/scalar/*` require a valid JWT.

### 11.2 Authorization

Role-based via `[Authorize(Roles = "...")]` attributes. Permission checks at use-case level.

### 11.3 Validation

FluentValidation for all commands. Validation errors return 400 with structured error response.

### 11.4 Audit Trail

All mutations publish domain events. An event handler persists audit entries asynchronously.

### 11.5 PII Handling

- All DTOs returned to admin endpoints use `RedactedString` fields by default
- PII reveal requires explicit API call which creates audit entry
- Logs use Serilog destructuring policy to redact PII
- Analytics events hash user identifiers

### 11.6 Error Handling

Consistent error response format:

```json
{
  "type": "validation_error",
  "title": "One or more validation errors occurred",
  "status": 400,
  "errors": {
    "title": ["Task title is required"],
    "priority": ["Invalid priority value"]
  },
  "traceId": "00-abc123..."
}
```
