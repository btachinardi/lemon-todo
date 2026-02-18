# Backend Architecture

> **Source**: Extracted from GUIDELINES.md §2 (Architecture: Domain-Driven Design — backend sections), §3 (Code Quality — backend naming), §4 (Git Workflow), §5 (Dependency Injection — backend), §6 (Error Handling — backend), §7 (Security Guidelines), §8 (Performance Guidelines)
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Backend Project Structure

```
src/
├── LemonDo.AppHost/              # Aspire orchestrator
├── LemonDo.ServiceDefaults/      # Shared Aspire defaults
├── LemonDo.Api/                  # ASP.NET Core API
│   ├── Endpoints/                # Minimal API endpoint definitions
│   ├── Middleware/               # Auth, error handling, protected data redaction
│   └── Program.cs                # App configuration
├── LemonDo.Application/          # Use cases layer
│   ├── Identity/
│   │   ├── Commands/             # RegisterUser, Login, etc.
│   │   └── Queries/              # GetCurrentUser, ListUsers, etc.
│   ├── Tasks/
│   │   ├── Commands/             # CreateTask, MoveTask, etc.
│   │   └── Queries/              # ListTasks, GetBoard, etc.
│   ├── Admin/
│   └── Common/                   # Shared interfaces, behaviors
├── LemonDo.Domain/               # Pure domain layer
│   ├── Identity/
│   │   ├── Entities/             # User, Role
│   │   ├── ValueObjects/         # Email, DisplayName, UserId
│   │   ├── Events/               # UserRegistered, LoginSucceeded, etc.
│   │   └── Repositories/         # IUserRepository (interface only)
│   ├── Tasks/
│   │   ├── Entities/             # TaskItem, Board, Column
│   │   ├── ValueObjects/         # TaskTitle, Priority, Tag, etc.
│   │   ├── Events/               # TaskCreated, TaskCompleted, etc.
│   │   └── Repositories/         # ITaskItemRepository, IBoardRepository
│   ├── Admin/
│   └── Common/                   # Entity base, ValueObject base, Result<T>
├── LemonDo.Infrastructure/       # Data access & external services
│   ├── Persistence/
│   │   ├── DbContext.cs
│   │   ├── Configurations/       # EF Core entity configs
│   │   ├── Repositories/         # Repository implementations
│   │   └── Migrations/
│   ├── Services/                 # Email, protected data encryption, etc.
│   └── Extensions/               # DI registration
```

---

## Layer Import Rules

| Layer | Can Import | Cannot Import |
|-------|-----------|---------------|
| **Domain** | Common (base classes, shared VOs) | Application, Infrastructure, API |
| **Application** | Domain | Infrastructure, API |
| **Infrastructure** | Domain, Application interfaces | API |
| **API** | Application, Infrastructure (via DI) | Domain directly |

---

## Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Class | PascalCase | `TaskItem`, `CreateTaskCommand` |
| Interface | IPascalCase | `IUserRepository` |
| Method | PascalCase | `GetByIdAsync` |
| Property | PascalCase | `DisplayName` |
| Private field | _camelCase | `_userRepository` |
| Parameter | camelCase | `cancellationToken` |
| Constant | PascalCase | `MaxTitleLength` |
| Enum value | PascalCase | `Priority.High` |

---

## Dependency Injection

All dependencies registered in `Infrastructure/Extensions/`:

```csharp
// Registration
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<ITaskItemRepository, TaskItemRepository>();

// Use cases registered via MediatR or manual registration
services.AddScoped<CreateTaskHandler>();
```

---

## Error Handling

- Domain layer: Return `Result<T, DomainError>` (never throw for business logic)
- Application layer: Handle results, translate to appropriate HTTP codes
- API layer: Global exception handler for unexpected errors
- All errors return consistent JSON structure (see [domain/api-design.md](../domain/api-design.md) §11.6)

---

## Security Guidelines

- Never log passwords, tokens, or protected data unredacted
- Always validate input at API boundaries
- Use parameterized queries (EF Core handles this)
- Set CORS to explicit origins only
- Rate limit authentication endpoints
- Use HTTPS in all environments
- Store secrets in environment variables (Azure Key Vault in production)
- Rotate JWT signing keys on a schedule
- Hash analytics identifiers (SHA-256)

---

## Performance Guidelines

- Use async/await for all I/O operations
- Use pagination for all list endpoints (max 100 items per page)
- Use EF Core projections (Select) to avoid loading unnecessary data
- Cache static assets with service worker
- Use Aspire health checks for readiness probes

---

## Git Workflow: Gitflow

### Branches

| Branch | Purpose | Merges Into |
|--------|---------|-------------|
| `main` | Production-ready code | - |
| `develop` | Integration branch | `main` (via release) |
| `feature/*` | New features | `develop` |
| `bugfix/*` | Bug fixes | `develop` |
| `release/*` | Release preparation | `main` + `develop` |
| `hotfix/*` | Production fixes | `main` + `develop` |

### Branch Naming

```
feature/auth-login-endpoint
feature/task-kanban-board
bugfix/task-completion-race-condition
release/1.0.0
hotfix/auth-token-expiry-fix
```

### Commit Messages: Conventional Commits

```
<type>(<scope>): <description>

[optional body]
```

| Type | When |
|------|------|
| `feat` | New feature |
| `fix` | Bug fix |
| `docs` | Documentation only |
| `test` | Adding/modifying tests |
| `refactor` | Code change that neither fixes nor adds |
| `chore` | Build, tooling, config changes |
| `ci` | CI/CD changes |
| `perf` | Performance improvement |

Examples:
```
feat(tasks): add drag-and-drop task movement between columns
fix(auth): prevent duplicate role assignment on concurrent requests
test(tasks): add property tests for TaskTitle value object
docs: update API endpoint documentation
chore: upgrade Aspire to 13.1
```

### Commit Rules

- Commit often, commit atomic logical blocks
- Each commit should compile and pass tests
- Never commit commented-out code
- Squash WIP commits before merging to develop

---

## Code Principles

1. **SOLID**: Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
2. **DRY**: Don't Repeat Yourself, but don't over-abstract (rule of three)
3. **KISS**: Keep It Simple. No premature optimization or abstraction
4. **Explicit over implicit**: Named parameters, clear return types, no magic strings
5. **Fail fast**: Validate at boundaries, throw early, catch late
6. **Immutable by default**: Use `readonly`, `IReadOnlyList`, `record` types

---

## Documentation Rules

- Best documentation is well-named code: `completeTask(taskId)` not `doAction(id)`
- Add inline comments ONLY for: complex algorithms, non-obvious business rules, workarounds with links to issues
- No comments that restate what code does: `// increment counter` above `counter++` is banned
- All public API endpoints have XML doc comments (auto-generates OpenAPI docs)
- All React components have JSDoc with prop descriptions
