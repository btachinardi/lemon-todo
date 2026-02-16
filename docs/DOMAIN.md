# LemonDo - Domain Design

> **Date**: 2026-02-14
> **Status**: Active
> **Architecture**: Domain-Driven Design (DDD)
> **Pattern**: CQRS-light with Use Cases
> **Informed By**: [PRD.md](./PRD.md), [SCENARIOS.md](./SCENARIOS.md)

---

## 1. Bounded Contexts

```
+-------------------+     +-------------------+     +-------------------+
|     Identity      |     |   Task Context    |     |   Administration  |
|   (Auth + RBAC)   |<--->|  (Task Lifecycle) |<--->|   (Audit + Admin) |
+-------------------+     +-------------------+     +-------------------+
         |                     |          ^                    |
         |                     |          |                    |
         v                     v          |                    v
+-------------------+     +-------------------+     +-------------------+
|    Onboarding     |     |  Board Context    |     |   Notification    |
|   (User Journey)  |     | (Spatial Layout)  |     |  (Communication)  |
+-------------------+     +-------------------+     +-------------------+
         |                                                    |
         v                                                    |
+-------------------+                                         |
|    Analytics      |<----------------------------------------+
|   (Measurement)   |
+-------------------+
```

### Context Map

| Context | Type | Responsibility |
|---------|------|----------------|
| **Identity** | Core | User registration, authentication, authorization, roles |
| **Task** | Core | Task lifecycle, status management, metadata (title, description, priority, tags, due date) |
| **Board** | Core | Kanban boards, columns, card placement, spatial ordering of tasks |
| **Administration** | Supporting | Audit logs, user management, system health, protected data handling |
| **Onboarding** | Supporting | User journey tracking, guided tours, progress tracking |
| **Analytics** | Generic | Event collection, funnel tracking, metrics aggregation |
| **Notification** | Generic | Email sending, in-app notifications, push notifications |

### Context Relationships

| Upstream | Downstream | Relationship |
|----------|------------|--------------|
| Identity | Task | Conformist (tasks reference user IDs) |
| Identity | Board | Conformist (boards reference user IDs) |
| Identity | Administration | Conformist (admin views user data) |
| Identity | Onboarding | Customer-Supplier (onboarding tracks identity events) |
| Task | Board | Conformist (board imports TaskId and TaskStatus from Task context) |
| Task | Analytics | Published Language (domain events -> analytics events) |
| Task | Onboarding | Published Language (task events -> onboarding progress) |
| Board | Analytics | Published Language (card events -> analytics events) |
| Identity | Notification | Customer-Supplier (user data for email) |
| Administration | Notification | Customer-Supplier (alerts, reports) |

---

## 2. Identity Context

### 2.1 Architecture Split

**As of CP4 (v0.4.0)**, Identity is split into two tables with shared IDs:

**AspNetUsers** (ASP.NET Identity — credentials + lockout + roles):
- Managed by `UserManager<ApplicationUser>`, `SignInManager`, `RoleManager`
- Handles: password hashing, account lockout, failed login tracking, role membership
- `UserName` stores SHA-256 email hash for login lookups
- No custom user profile data

**Users** (Domain — profile + protected data + business state):
- Managed by `IUserRepository` in domain layer
- Stores: redacted display values, encrypted protected data shadow properties, deactivation state
- `EmailHash` unique index for exact-email admin searches
- All business logic (deactivate/reactivate, profile updates)

### 2.2 Entities

#### User (Aggregate Root, Domain Layer)

```
User
├── Id: UserId (value object, shared with AspNetUsers.Id)
├── RedactedEmail: string (display value, e.g., "j***@example.com")
├── RedactedDisplayName: string (display value, e.g., "J***n")
├── IsDeactivated: bool
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Shadow Properties (via UserRepository):
│   ├── EmailHash: string (SHA-256 of normalized email)
│   ├── EncryptedEmail: string (AES-256-GCM ciphertext)
│   └── EncryptedDisplayName: string (AES-256-GCM ciphertext)
│
├── Methods:
│   ├── Create(email: Email, displayName: DisplayName) -> UserRegisteredEvent
│   │       (validates VOs, stores redacted forms, raises event)
│   ├── Reconstitute(id, redactedEmail, redactedDisplayName, isDeactivated)
│   │       (for persistence reload, bypasses validation)
│   ├── Deactivate() -> Result<DomainError>
│   │       (business rule: cannot deactivate already-deactivated user)
│   └── Reactivate() -> Result<DomainError>
│       (business rule: cannot reactivate active user)
│
└── Invariants:
    ├── Email must be valid format (validated during Create via Email VO)
    ├── DisplayName must be 2-100 characters (validated during Create via DisplayName VO)
    ├── RedactedEmail/RedactedDisplayName are always derived from validated VOs
    ├── Cannot deactivate twice or reactivate twice
    └── Encrypted shadow properties managed by repository (domain unaware)
```

#### ApplicationUser (Infrastructure Layer, ASP.NET Identity)

```
ApplicationUser : IdentityUser<Guid>
├── Id: Guid (shared with domain User.Id)
├── UserName: string (stores SHA-256 email hash)
├── PasswordHash: string (Identity-managed)
├── SecurityStamp: string (Identity-managed)
├── LockoutEnd: DateTimeOffset? (Identity-managed)
├── AccessFailedCount: int (Identity-managed)
└── [All other Identity built-in fields]

No custom properties. Credentials and lockout ONLY.
```

### 2.3 Value Objects

```
UserId          -> Guid wrapper
Email           -> Validated email string (RFC 5322), implements IProtectedData
                   Redacted property: "j***@example.com" (first char + domain)
DisplayName     -> Non-empty string, 2-100 chars, trimmed, implements IProtectedData
                   Redacted property: "J***n" (first + last char)
ProtectedDataRevealReason -> Enum: SupportTicket, LegalRequest, AccountRecovery,
                                   SecurityInvestigation, DataSubjectRequest,
                                   ComplianceAudit, Other
SystemProtectedDataAccessReason -> Enum: TransactionalEmail, PasswordResetEmail,
                                         AccountVerificationEmail, DataExport, SystemMigration
```

### 2.4 Domain Events

```
UserRegisteredEvent         { UserId }
                            (No protected data — event handlers load user data via repository if needed)
EmailVerifiedEvent          { UserId }
LoginSucceededEvent         { UserId, Method, IpAddress }
LoginFailedEvent            { Email, IpAddress, Reason }
AccountLockedEvent          { UserId, LockoutEnd }
RoleAssignedEvent           { UserId, RoleName, AssignedBy }
RoleRemovedEvent            { UserId, RoleName, RemovedBy }
MfaEnabledEvent             { UserId }
MfaDisabledEvent            { UserId }
UserDeactivatedEvent        { UserId, DeactivatedBy }
UserReactivatedEvent        { UserId, ReactivatedBy }
PasswordResetEvent          { UserId }
```

### 2.5 Use Cases

```
Commands:
├── RegisterUserCommand          { Email, DisplayName, Password }
│       → Validates Email/DisplayName VOs, creates domain User,
│         creates Identity credentials (emailHash + password),
│         stores User with encrypted protected data, creates default board,
│         returns AuthResult with redacted values
├── LoginUserCommand             { Email, Password }
│       → Authenticates via Identity (hash-based lookup),
│         loads domain User for profile, generates tokens,
│         returns AuthResult with redacted values
├── RefreshTokenCommand          { RefreshToken }
│       → Validates refresh token via Identity,
│         loads domain User for profile, generates new tokens,
│         returns AuthResult with redacted values
├── LogoutCommand                { RefreshToken }
│       → Revokes refresh token in Identity
├── VerifyEmailCommand           { Token }
├── RequestPasswordResetCommand  { Email }
├── ResetPasswordCommand         { Token, NewPassword }
├── AssignRoleCommand            { UserId, RoleName }  [SystemAdmin only]
├── RemoveRoleCommand            { UserId, RoleName }  [SystemAdmin only]
├── EnableMfaCommand             { UserId, TotpCode }
├── DisableMfaCommand            { UserId, TotpCode }
├── DeactivateUserCommand        { UserId }            [SystemAdmin only]
│       → Domain User.Deactivate() + Identity lockout (DateTimeOffset.MaxValue)
└── ReactivateUserCommand        { UserId }            [SystemAdmin only]
        → Domain User.Reactivate() + Identity lockout cleared

Queries:
├── GetCurrentUserQuery          {} -> UserDto (loads from IUserRepository)
├── GetUserByIdQuery             { UserId } -> UserDto  [Admin+]
├── ListUsersAdminQuery          { Page, PageSize, Search?, Role? } -> PagedResult<AdminUserDto>  [Admin+]
│       (Search: exact email via hash match, or partial redacted display name)
└── ValidateMfaCodeQuery         { UserId, TotpCode } -> bool
```

### 2.6 Repository Interface

```csharp
/// <summary>
/// Repository for domain User entity. Handles transparent protected data encryption via EF shadow properties.
/// </summary>
public interface IUserRepository
{
    /// <summary>Loads user by ID. Returns null if not found.</summary>
    Task<User?> GetByIdAsync(UserId id, CancellationToken ct);

    /// <summary>
    /// Persists a new domain User with encrypted protected data.
    /// Repository computes EmailHash, encrypts plaintext VOs, stores shadow properties.
    /// </summary>
    Task AddAsync(User user, Email email, DisplayName displayName, CancellationToken ct);

    /// <summary>Updates an existing user (for deactivation/reactivation).</summary>
    Task UpdateAsync(User user, CancellationToken ct);
}
```

### 2.7 Anti-Corruption Layer (IAuthService)

```csharp
/// <summary>
/// ACL port for credential operations. Handles ONLY authentication and authorization.
/// User profile data is managed by IUserRepository.
/// </summary>
public interface IAuthService
{
    /// <summary>Creates Identity credentials (password hash + lockout config). No user data.</summary>
    Task<Result<DomainError>> CreateCredentialsAsync(
        UserId userId, string emailHash, string password, CancellationToken ct);

    /// <summary>Authenticates by email (hashed internally) and password. Returns UserId on success.</summary>
    Task<Result<UserId, DomainError>> AuthenticateAsync(
        string email, string password, CancellationToken ct);

    /// <summary>Generates JWT access + refresh token for a user. Loads roles from Identity.</summary>
    Task<AuthTokens> GenerateTokensAsync(UserId userId, CancellationToken ct);

    /// <summary>Validates refresh token, returns UserId + new token pair.</summary>
    Task<Result<(UserId, AuthTokens), DomainError>> RefreshTokenAsync(
        string refreshToken, CancellationToken ct);

    /// <summary>Revokes a refresh token (idempotent).</summary>
    Task<Result<DomainError>> RevokeRefreshTokenAsync(
        string refreshToken, CancellationToken ct);

    /// <summary>Verifies a user's password (for break-the-glass re-authentication).</summary>
    Task<Result<DomainError>> VerifyPasswordAsync(
        Guid userId, string password, CancellationToken ct);
}
```

### 2.8 Protected Data Access Service

```csharp
/// <summary>
/// Port for audited protected data decryption. Every call records an audit entry.
/// This is the ONLY authorized path for decrypting encrypted protected data fields.
/// </summary>
public interface IProtectedDataAccessService
{
    /// <summary>
    /// Decrypts protected data for system operations (transactional emails, data export).
    /// Records audit with AuditAction.ProtectedDataAccessed and null actor (system).
    /// </summary>
    Task<Result<DecryptedProtectedData, DomainError>> AccessForSystemAsync(
        Guid userId, SystemProtectedDataAccessReason reason, string? details, CancellationToken ct);

    /// <summary>
    /// Decrypts protected data for admin break-the-glass reveal.
    /// Caller records admin-specific audit with justification + re-auth proof.
    /// </summary>
    Task<Result<DecryptedProtectedData, DomainError>> RevealForAdminAsync(
        Guid userId, CancellationToken ct);
}
```

---

## 3. Task Context

### 3.1 Design Principles

1. **Task owns its lifecycle and status** — A Task manages its own status through `SetStatus()`, `Complete()`, and `Uncomplete()`. Status transitions are self-contained within the Task aggregate.
2. **Task knows nothing about boards** — The Task entity has no concept of columns, positions, or spatial placement. It is a pure domain entity focused on task metadata and lifecycle.
3. **Archive is a visibility flag, NOT a lifecycle status** — `IsArchived` is a boolean orthogonal to status. Any task can be archived regardless of its current status (Todo, InProgress, Done).
4. **Status and CompletedAt are always consistent** — `SetStatus()` is the single method that manages CompletedAt transitions. `Complete()` and `Uncomplete()` are convenience wrappers around `SetStatus()`.

### 3.2 Entities

#### Task (Aggregate Root)

```
Task
├── Id: TaskId (value object)
├── OwnerId: UserId (value object, from Identity context)
├── Title: TaskTitle (value object)
├── Description: TaskDescription? (value object)
├── Priority: Priority (value object / enum)
├── Status: TaskStatus (Todo, InProgress, Done)
├── DueDate: DateTimeOffset?
├── Tags: IReadOnlyList<Tag>
├── IsArchived: bool (visibility flag, orthogonal to status)
├── IsDeleted: bool (soft delete flag)
├── CompletedAt: DateTimeOffset? (set when status becomes Done)
├── CreatedAt: DateTimeOffset
├── UpdatedAt: DateTimeOffset
│
├── Methods:
│   ├── Create(ownerId, title, description?, priority?, dueDate?, tags?)
│   │       -> TaskCreatedEvent (defaults to TaskStatus.Todo)
│   ├── SetStatus(status) -> TaskStatusChangedEvent
│   │       (manages CompletedAt: set when transitioning to Done, cleared when leaving Done)
│   ├── Complete() -> TaskStatusChangedEvent
│   │       (convenience: calls SetStatus(Done))
│   ├── Uncomplete() -> TaskStatusChangedEvent
│   │       (convenience: calls SetStatus(Todo))
│   ├── UpdateTitle(title) -> TaskUpdatedEvent
│   ├── UpdateDescription(description) -> TaskUpdatedEvent
│   ├── SetPriority(priority) -> TaskPriorityChangedEvent
│   ├── SetDueDate(dueDate) -> TaskDueDateChangedEvent
│   ├── AddTag(tag) -> TaskTagAddedEvent
│   ├── RemoveTag(tag) -> TaskTagRemovedEvent
│   ├── Archive() -> TaskArchivedEvent (any status)
│   ├── Unarchive() -> TaskUnarchivedEvent
│   └── Delete() -> TaskDeletedEvent (soft delete)
│
└── Invariants:
    ├── Title must be 1-500 characters
    ├── Description must be 0-10000 characters
    ├── Cannot archive a deleted task
    ├── Cannot edit a deleted task
    ├── Tags are unique per task (no duplicates)
    ├── OwnerId cannot change after creation
    ├── CompletedAt is set when status transitions to Done
    └── CompletedAt is cleared when status transitions away from Done
```

### 3.3 Value Objects

```
TaskId          -> Guid wrapper
TaskTitle       -> Non-empty string, 1-500 chars, trimmed
TaskDescription -> String, 0-10000 chars
Tag             -> Non-empty string, 1-50 chars, lowercase, trimmed
Priority        -> Enum: None, Low, Medium, High, Critical
TaskStatus      -> Enum: Todo, InProgress, Done (NO Archived — archive is a visibility flag)
```

### 3.4 Domain Events

```
TaskCreatedEvent            { TaskId, OwnerId, Title, Priority }
TaskUpdatedEvent            { TaskId, FieldName, OldValue, NewValue }
TaskStatusChangedEvent      { TaskId, OldStatus, NewStatus }
TaskPriorityChangedEvent    { TaskId, OldPriority, NewPriority }
TaskDueDateChangedEvent     { TaskId, OldDueDate?, NewDueDate? }
TaskTagAddedEvent           { TaskId, Tag }
TaskTagRemovedEvent         { TaskId, Tag }
TaskArchivedEvent           { TaskId }
TaskUnarchivedEvent         { TaskId }
TaskDeletedEvent            { TaskId, DeletedAt }
```

### 3.5 Use Cases

```
Commands:
├── CreateTaskCommand            { Title, Description?, Priority?, DueDate?, Tags? }
│       → Creates Task (defaults to Todo), then coordinates with Board context
│         to place card on default board's initial column
├── UpdateTaskCommand            { TaskId, Title?, Description?, Priority?, DueDate? }
├── AddTagToTaskCommand          { TaskId, Tag }
├── RemoveTagFromTaskCommand     { TaskId, Tag }
├── CompleteTaskCommand          { TaskId }
│       → Calls task.Complete(), then coordinates with Board context
│         to move card to Done column
├── UncompleteTaskCommand        { TaskId }
│       → Calls task.Uncomplete(), then coordinates with Board context
│         to move card to Todo column
├── ArchiveTaskCommand           { TaskId }
├── DeleteTaskCommand            { TaskId }
└── BulkCompleteTasksCommand     { TaskIds }
        → Same as CompleteTaskCommand in a loop

Queries:
├── GetTaskByIdQuery             { TaskId } -> TaskDto (no columnId/position)
└── ListTasksQuery               { Status?, Priority?, Search?, Page, PageSize }
                                     -> PagedResult<TaskDto> (no columnId filter)
```

### 3.6 Repository Interface

```csharp
public interface ITaskRepository
{
    Task<Task?> GetByIdAsync(TaskId id, CancellationToken ct);
    Task<PagedResult<Task>> ListAsync(
        UserId ownerId, Priority? priority, TaskStatus? status,
        string? searchTerm, int page, int pageSize, CancellationToken ct);
    Task AddAsync(Task task, CancellationToken ct);
    Task UpdateAsync(Task task, CancellationToken ct);
}
```

---

## 4. Board Context

### 4.1 Design Principles

1. **Board owns spatial placement, not task lifecycle** — Boards manage where tasks appear on the kanban (which column, what position). Task status is owned by the Task context.
2. **Board is conformist to Task** — Board imports `TaskId` and `TaskStatus` from the Task context. The Task context knows nothing about boards.
3. **Status changes coordinated at application layer** — When a card moves to a new column, the application handler syncs the task's status to the column's `TargetStatus`. The Board's `MoveCard()` and `PlaceTask()` methods return the target status so the handler can call `task.SetStatus()`.
4. **TaskCard is a value object on Board** — The `TaskCard` represents a task's placement on the board (column + position). It references `TaskId` but does not contain task data.

### 4.2 Entities

#### Board (Aggregate Root)

```
Board
├── Id: BoardId (value object)
├── OwnerId: UserId
├── Name: BoardName (value object)
├── Columns: IReadOnlyList<Column> (ordered by position)
├── Cards: IReadOnlyList<TaskCard> (task placements on this board)
├── CreatedAt: DateTimeOffset
│
├── Methods:
│   ├── CreateDefault(ownerId) -> BoardCreatedEvent (3 columns: To Do, In Progress, Done)
│   ├── Create(ownerId, name) -> BoardCreatedEvent (2 columns: To Do, Done)
│   ├── GetInitialColumn() -> Column (first Todo column by position)
│   ├── GetDoneColumn() -> Column (first Done column by position)
│   ├── FindColumnById(columnId) -> Column? (lookup for validation)
│   ├── PlaceTask(taskId, columnId) -> TaskStatus (returns column's TargetStatus)
│   │       + CardPlacedEvent (rank auto-assigned from Column.NextRank)
│   ├── MoveCard(taskId, toColumnId, previousTaskId?, nextTaskId?) -> TaskStatus
│   │       + CardMovedEvent (rank computed from neighbor ranks)
│   ├── RemoveCard(taskId) -> CardRemovedEvent
│   ├── FindCardByTaskId(taskId) -> TaskCard?
│   ├── GetCardCountInColumn(columnId) -> int
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
    ├── Cannot remove the last column targeting a required status (Todo or Done)
    ├── A task can only have one card per board (no duplicate TaskIds)
    └── Cannot move a card to a column that doesn't exist on this board
```

#### Column (Entity, owned by Board)

```
Column
├── Id: ColumnId (value object)
├── Name: ColumnName (value object)
├── TargetStatus: TaskStatus (the status tasks get when placed here)
├── Position: int
├── MaxTasks: int? (null = unlimited)
├── NextRank: decimal (monotonic counter, starts at 1000, incremented by 1000 per placement)
│
└── Invariants:
    ├── Name must be 1-50 characters
    ├── Position must be >= 0
    ├── MaxTasks must be > 0 if set
    └── TargetStatus is immutable after creation
```

#### TaskCard (Value Object, owned by Board)

```
TaskCard
├── TaskId: TaskId (references Task context)
├── ColumnId: ColumnId (references Column on this board)
├── Rank: decimal (sparse sort key within column, e.g. 1000, 2000, 1500)
│
└── Invariants:
    ├── TaskId must be non-empty
    ├── ColumnId must reference a column on the owning board
    └── Rank must be > 0
```

### 4.3 Value Objects

```
BoardId         -> Guid wrapper
ColumnId        -> Guid wrapper
BoardName       -> Non-empty string, 1-100 chars
ColumnName      -> Non-empty string, 1-50 chars
TaskCard        -> TaskId + ColumnId + Rank (placement of a task on the board)
```

### 4.4 Domain Events

```
BoardCreatedEvent           { BoardId, OwnerId }
ColumnAddedEvent            { BoardId, ColumnId, ColumnName }
ColumnRemovedEvent          { BoardId, ColumnId }
ColumnReorderedEvent        { BoardId, ColumnId, OldPosition, NewPosition }
ColumnRenamedEvent          { BoardId, ColumnId, Name }
CardPlacedEvent             { BoardId, TaskId, ColumnId, Rank }
CardMovedEvent              { BoardId, TaskId, FromColumnId, ToColumnId, NewRank }
CardRemovedEvent            { BoardId, TaskId }
```

### 4.5 Use Cases

```
Commands:
├── MoveCardCommand              { TaskId, ColumnId, PreviousTaskId?, NextTaskId? }
│       → Moves card on board (rank from neighbors), gets target status, calls task.SetStatus(targetStatus)
├── AddColumnCommand             { BoardId, Name, TargetStatus, Position? }
├── RemoveColumnCommand          { BoardId, ColumnId }
├── ReorderColumnCommand         { BoardId, ColumnId, NewPosition }
└── RenameColumnCommand          { BoardId, ColumnId, Name }

Queries:
├── GetDefaultBoardQuery         {} -> BoardDto (auto-creates if missing, includes Cards)
└── GetBoardByIdQuery            { BoardId } -> BoardDto (includes Cards)
```

### 4.6 Repository Interface

```csharp
public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct);
    Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct);
    Task AddAsync(Board board, CancellationToken ct);
    Task UpdateAsync(Board board, CancellationToken ct);
}
```

### 4.7 Application Layer Coordination

The Task and Board contexts are coordinated at the application layer (command handlers). The following cross-context workflows exist:

| Operation | Task Context | Board Context |
|-----------|-------------|---------------|
| **Create Task** | `Task.Create()` (status = Todo) | `board.PlaceTask(taskId, initialColumn)` (rank auto-assigned) |
| **Move Card** | `task.SetStatus(targetStatus)` | `board.MoveCard(taskId, toColumnId, previousTaskId?, nextTaskId?)` returns `targetStatus` |
| **Complete Task** | `task.Complete()` | `board.MoveCard(taskId, doneColumn, null, null)` |
| **Uncomplete Task** | `task.Uncomplete()` | `board.MoveCard(taskId, todoColumn, null, null)` |
| **Delete Task** | `task.Delete()` | `board.RemoveCard(taskId)` |

---

## 5. Administration Context

### 5.1 Entities

#### AuditEntry (Entity)

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

### 5.2 Value Objects

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

### 5.3 Use Cases

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

### 5.4 Protected Data Handling Services

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

---

## 6. Onboarding Context

### 6.1 Entities

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

### 6.2 Value Objects

```
OnboardingStep
├── Type: OnboardingStepType (WelcomeViewed, FirstTaskCreated, FirstTaskCompleted, KanbanExplored)
├── CompletedAt: DateTimeOffset?
├── IsCompleted: bool
```

---

## 7. Analytics Context

### 7.1 Entities

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

### 7.2 Value Objects

```
EventContext
├── DeviceType: string (mobile, tablet, desktop)
├── Locale: string
├── Theme: string
├── AppVersion: string
```

---

## 8. Notification Context

### 8.1 Entities

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
├── Data: Dictionary<string, string> (template variables, protected-data-redacted)
```

### 8.2 Use Cases

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

## 9. Shared Kernel

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

## 10. Entity Relationship Diagram

```
+----------+       +-----------+
|   User   |1---N  |   Task    |
+----------+       +-----------+
| Id       |       | Id        |
| Email    |       | OwnerId  |----+
| Name     |       | Title     |    |
| Roles[]  |       | Priority  |    |
+----------+       | Status    |    |
     |             | DueDate   |    |
     |             | IsArchived|    |
     |             | Tags[]    |    |
     |             +-----------+    |
     |                  |           |
     |                  | TaskId    |
     |                  v           |
     |             +-----------+    |
     |             | TaskCard  |    |    +-----------+
     |             +-----------+    |    |  Column   |
     |             | TaskId   |----+    +-----------+
     |             | ColumnId |-------->| Id        |
     |             | Rank     |         | Name      |
     |             +-----------+         | TargetSt. |
     |                  |                | Pos       |
     |                  |                | MaxTasks  |
     |                  |                | NextRank  |
     |             +--------+            +-----------+
     |             | Board  |                 |
     |             +--------+                 |
     |1---N        | Id     |1---N  Columns---+
     +------------>| Name   |
     |             | Cards[]|1---N  TaskCards
     |             +--------+
     |
     |1---N  +------------------+
     +------>| AuditEntry       |
     |       +------------------+
     |       | Id               |
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

## 11. API Endpoint Design

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

### Task Endpoints

```
GET    /api/tasks                      List tasks (with filters/pagination)
POST   /api/tasks                      Create task (also places card on default board)
GET    /api/tasks/{id}                 Get task by ID
PUT    /api/tasks/{id}                 Update task
DELETE /api/tasks/{id}                 Soft-delete task (also removes card from board)
POST   /api/tasks/{id}/complete        Complete task (also moves card to Done column)
POST   /api/tasks/{id}/uncomplete      Uncomplete task (also moves card to Todo column)
POST   /api/tasks/{id}/archive         Archive task
POST   /api/tasks/{id}/tags            Add tag
DELETE /api/tasks/{id}/tags/{tag}      Remove tag
POST   /api/tasks/bulk/complete        Bulk complete tasks
```

### Board Endpoints

```
GET    /api/boards                     List user's boards
POST   /api/boards                     Create board
GET    /api/boards/{id}                Get board with columns and cards
GET    /api/boards/default             Get default board (includes cards)
POST   /api/boards/{id}/cards/move     Move card between neighbors (also syncs task status)
POST   /api/boards/{id}/columns        Add column
PUT    /api/boards/{id}/columns/{colId} Rename column
DELETE /api/boards/{id}/columns/{colId} Remove column
POST   /api/boards/{id}/columns/reorder Reorder columns
```

### Administration Endpoints

```
GET    /api/admin/users                List users (protected data redacted)    [Admin+]
GET    /api/admin/users/{id}           Get user detail (redacted)              [Admin+]
POST   /api/admin/users/{id}/reveal    Reveal protected data field             [SystemAdmin]
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

## 12. Cross-Cutting Concerns

### 12.1 Authentication Middleware

All endpoints except `/api/auth/register`, `/api/auth/login`, `/api/auth/login/oauth/*`, `/health`, `/ready`, and `/scalar/*` require a valid JWT.

### 12.2 Authorization

Role-based via `[Authorize(Roles = "...")]` attributes. Permission checks at use-case level.

### 12.3 Validation

FluentValidation for all commands. Validation errors return 400 with structured error response.

### 12.4 Audit Trail

All mutations publish domain events. An event handler persists audit entries asynchronously.

### 12.5 Protected Data Handling

- All DTOs returned to admin endpoints use `RedactedString` fields by default
- **Break-the-glass protected data reveal** (HIPAA-modeled):
  - Mandatory justification (`ProtectedDataRevealReason` enum + optional comments)
  - Password re-authentication before reveal (MFA step-up planned for future)
  - Structured audit trail with JSON details (reason, details, comments)
  - Time-limited reveal (30 seconds) with client-side countdown UX
- Task titles and descriptions treated as potential PHI — stripped from audit log entries
- Tags are categorical labels, not treated as protected data
- Logs use Serilog destructuring policy to redact protected data
- Analytics events hash user identifiers

### 12.6 Error Handling

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
