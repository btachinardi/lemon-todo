# LemonDo - Development Guidelines

> These guidelines govern all code contributions to the LemonDo project.
> Every developer (human or AI) must follow these rules without exception.

---

## 1. Development Methodology: TDD

We follow strict Test-Driven Development with RED-GREEN-VALIDATE phases.

### The TDD Cycle

```
1. RED:      Write a failing test that describes the desired behavior
2. GREEN:    Write the minimum code to make the test pass
3. VALIDATE: Refactor while keeping tests green, then verify full suite
```

### Rules

- Never write production code without a failing test first
- Tests are the specification. If there's no test for it, it doesn't exist
- Each test should test ONE behavior, not one method
- Test names describe behavior: `should_create_task_with_valid_title`, not `test_create_task`
- Property tests cover domain invariants (value objects, entities)
- Integration tests cover API endpoints
- E2E tests cover user scenarios

### Testing Pyramid

```
        /  E2E (Playwright)  \          <- Few, slow, full-stack
       / Integration (API)    \         <- Moderate, test endpoints
      / Unit (Domain + Components) \    <- Many, fast, isolated
     /  Property (Domain invariants) \  <- Many, fast, generated
    +----------------------------------+
```

### Coverage Targets

| Layer | Target |
|-------|--------|
| Domain entities & value objects | 90% |
| Use cases / handlers | 80% |
| API endpoints (integration) | 100% of happy + error paths |
| Frontend components | 80% |
| E2E scenarios | 100% of SCENARIOS.md storyboards |

---

## 2. Architecture: Domain-Driven Design

### Backend Project Structure

```
src/
├── LemonDo.AppHost/              # Aspire orchestrator
├── LemonDo.ServiceDefaults/      # Shared Aspire defaults
├── LemonDo.Api/                  # ASP.NET Core API
│   ├── Endpoints/                # Minimal API endpoint definitions
│   ├── Middleware/                # Auth, error handling, PII redaction
│   └── Program.cs               # App configuration
├── LemonDo.Application/          # Use cases layer
│   ├── Identity/
│   │   ├── Commands/            # RegisterUser, Login, etc.
│   │   └── Queries/             # GetCurrentUser, ListUsers, etc.
│   ├── Tasks/
│   │   ├── Commands/            # CreateTask, MoveTask, etc.
│   │   └── Queries/             # ListTasks, GetBoard, etc.
│   ├── Admin/
│   └── Common/                  # Shared interfaces, behaviors
├── LemonDo.Domain/               # Pure domain layer
│   ├── Identity/
│   │   ├── Entities/            # User, Role
│   │   ├── ValueObjects/        # Email, DisplayName, UserId
│   │   ├── Events/              # UserRegistered, LoginSucceeded, etc.
│   │   └── Repositories/        # IUserRepository (interface only)
│   ├── Tasks/
│   │   ├── Entities/            # TaskItem, Board, Column
│   │   ├── ValueObjects/        # TaskTitle, Priority, Tag, etc.
│   │   ├── Events/              # TaskCreated, TaskCompleted, etc.
│   │   └── Repositories/        # ITaskItemRepository, IBoardRepository
│   ├── Admin/
│   └── Common/                  # Entity base, ValueObject base, Result<T>
├── LemonDo.Infrastructure/       # Data access & external services
│   ├── Persistence/
│   │   ├── DbContext.cs
│   │   ├── Configurations/      # EF Core entity configs
│   │   ├── Repositories/        # Repository implementations
│   │   └── Migrations/
│   ├── Services/                # Email, PII encryption, etc.
│   └── Extensions/              # DI registration
```

### Frontend Project Structure (3-Layer Components)

```
client/
├── src/
│   ├── app/                     # App shell, routing, providers
│   │   ├── routes/              # L1: Route components (data sourcing)
│   │   └── providers/           # Global providers (auth, theme, i18n)
│   ├── domains/                 # L2: Domain UI components
│   │   ├── auth/
│   │   │   ├── components/      # LoginForm, RegisterForm, MfaSetup
│   │   │   ├── hooks/           # useAuth, useCurrentUser
│   │   │   └── types/           # Auth domain types
│   │   ├── tasks/
│   │   │   ├── components/      # KanbanBoard, TaskCard, TaskList
│   │   │   ├── hooks/           # useTasks, useBoard
│   │   │   └── types/           # Task domain types
│   │   ├── admin/
│   │   └── onboarding/
│   ├── ui/                      # L3: Design system (dumb components)
│   │   ├── button/
│   │   ├── input/
│   │   ├── card/
│   │   ├── dialog/
│   │   ├── toast/
│   │   └── ...                  # Shadcn/ui + custom components
│   ├── lib/                     # Utilities, API client, analytics
│   ├── i18n/                    # Translation files
│   └── test/                    # Test utilities, factories
├── e2e/                         # Playwright E2E tests
```

### Layer Rules

| Layer | Can Import From | Cannot Import From |
|-------|----------------|--------------------|
| L1 (Routes) | L2, L3, hooks, lib | Nothing restricted |
| L2 (Domain UI) | L3, domain types, hooks | L1, native HTML tags |
| L3 (Design System) | Radix, Tailwind, React | L1, L2, domain types |
| Domain (backend) | Common only | Application, Infrastructure, API |
| Application | Domain | Infrastructure, API |
| Infrastructure | Domain, Application interfaces | API |
| API | Application, Infrastructure (via DI) | Domain directly |

---

## 3. Code Quality

### Naming Conventions

#### Backend (.NET)

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

#### Frontend (TypeScript)

| Element | Convention | Example |
|---------|-----------|---------|
| Component | PascalCase | `TaskCard`, `LoginForm` |
| Hook | useCamelCase | `useAuth`, `useTasks` |
| Function | camelCase | `createTask`, `formatDate` |
| Variable | camelCase | `taskList`, `isLoading` |
| Constant | SCREAMING_SNAKE | `MAX_TITLE_LENGTH` |
| Type/Interface | PascalCase | `TaskItem`, `AuthState` |
| Enum | PascalCase | `Priority.High` |
| File (component) | PascalCase.tsx | `TaskCard.tsx` |
| File (hook) | use-*.ts | `use-auth.ts` |
| File (utility) | camelCase.ts | `apiClient.ts` |
| File (test) | *.test.ts(x) | `TaskCard.test.tsx` |

### Code Principles

1. **SOLID**: Single Responsibility, Open/Closed, Liskov, Interface Segregation, Dependency Inversion
2. **DRY**: Don't Repeat Yourself, but don't over-abstract (rule of three)
3. **KISS**: Keep It Simple. No premature optimization or abstraction
4. **Explicit over implicit**: Named parameters, clear return types, no magic strings
5. **Fail fast**: Validate at boundaries, throw early, catch late
6. **Immutable by default**: Use `readonly`, `IReadOnlyList`, `record` types

### Documentation Rules

- Best documentation is well-named code: `completeTask(taskId)` not `doAction(id)`
- Add inline comments ONLY for: complex algorithms, non-obvious business rules, workarounds with links to issues
- No comments that restate what code does: `// increment counter` above `counter++` is banned
- All public API endpoints have XML doc comments (auto-generates OpenAPI docs)
- All React components have JSDoc with prop descriptions

---

## 4. Git Workflow: Gitflow

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
- Never attribute commits (no "Co-authored-by" unless pair programming)
- Squash WIP commits before merging to develop

---

## 5. Dependency Injection

### Backend

All dependencies registered in `Infrastructure/Extensions/`:

```csharp
// Registration
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<ITaskItemRepository, TaskItemRepository>();

// Use cases registered via MediatR or manual registration
services.AddScoped<CreateTaskHandler>();
```

### Frontend

React Context for cross-cutting concerns:

```tsx
// Auth context, Theme context, i18n provider
// API client configured via provider
// Analytics service injected via context
```

---

## 6. Error Handling

### Backend

- Domain layer: Return `Result<T, DomainError>` (never throw for business logic)
- Application layer: Handle results, translate to appropriate HTTP codes
- API layer: Global exception handler for unexpected errors
- All errors return consistent JSON structure (see DOMAIN.md section 11.6)

### Frontend

- API calls: try/catch with error boundary fallbacks
- Component errors: React Error Boundaries
- User-facing errors: Toast notifications with actionable messages
- Never show stack traces or technical details to users

---

## 7. Security Guidelines

- Never log passwords, tokens, or PII unredacted
- Always validate input at API boundaries
- Use parameterized queries (EF Core handles this)
- Set CORS to explicit origins only
- Rate limit authentication endpoints
- Use HTTPS in all environments
- Store secrets in environment variables (Azure Key Vault in production)
- Rotate JWT signing keys on a schedule
- Hash analytics identifiers (SHA-256)

---

## 8. Performance Guidelines

- Use async/await for all I/O operations
- Use pagination for all list endpoints (max 100 items per page)
- Use EF Core projections (Select) to avoid loading unnecessary data
- Frontend: React.memo for expensive components, useMemo/useCallback where measured
- Frontend: Code-split routes with React.lazy
- Frontend: Optimize images, use WebP where possible
- Cache static assets with service worker
- Use Aspire health checks for readiness probes

---

## 9. Accessibility Guidelines

- All interactive elements must be keyboard-navigable
- All images must have alt text
- Color contrast must meet WCAG 2.1 AA (4.5:1 for text)
- Form inputs must have associated labels
- Error messages must be associated with their fields (aria-describedby)
- Focus management: return focus after modal close
- Screen reader testing for all user flows
- Use Radix UI primitives (they handle most accessibility)

---

## 10. i18n Guidelines

- All user-facing strings must use translation keys
- No hardcoded strings in components
- Translation keys follow: `{domain}.{component}.{element}`
  - Example: `tasks.kanban.columnHeader`, `auth.login.submitButton`
- Date/number formatting via locale-aware formatters
- Backend validation messages also use translation keys
- Default language: English (en)
- MVP languages: English (en), Portuguese (pt-BR), Spanish (es)
