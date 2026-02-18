# Identity Context

> **Source**: Extracted from docs/DOMAIN.md §2
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## 2.1 Architecture Split

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

## 2.2 Entities

### User (Aggregate Root, Domain Layer)

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

### ApplicationUser (Infrastructure Layer, ASP.NET Identity)

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

## 2.3 Value Objects

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

## 2.4 Domain Events

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

## 2.5 Use Cases

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

## 2.6 Repository Interface

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

## 2.7 Anti-Corruption Layer (IAuthService)

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

## 2.8 Protected Data Access Service

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
