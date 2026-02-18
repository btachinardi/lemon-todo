# API Design

> **Source**: Extracted from docs/DOMAIN.md §11 and §12
> **Status**: Active
> **Last Updated**: 2026-02-18

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
