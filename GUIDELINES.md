# LemonDo - Development Guidelines

> These guidelines govern all code contributions to the LemonDo project.

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
- Each test should test one behavior, not one method
- Test names describe behavior: `should_create_task_with_valid_title`, not `test_create_task`
- Property tests cover domain invariants (value objects, entities)
- Integration tests cover API endpoints
- E2E tests cover user scenarios

### Testing Pyramid

```
         +-------------------------+
        /      E2E (Playwright)     \     <- Few, slow, full-stack
       /      Integration (API)      \    <- Moderate, test endpoints
      /   Unit (Domain + Components)  \   <- Many, fast, isolated
     /  Property (Domain invariants)   \  <- Many, fast, generated
    +-----------------------------------+
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
│   ├── Middleware/               # Auth, error handling, PII redaction
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
│   ├── Services/                 # Email, PII encryption, etc.
│   └── Extensions/               # DI registration
```

### Frontend Architecture: 4-Layer System

The frontend follows a strict layered architecture that separates concerns and enforces
unidirectional data flow. Each layer has a single responsibility and clear import rules.

```
┌─────────────────────────────────────────────────────────────┐
│  L1: Data Sourcing & Routing                                │
│  Route components, data orchestration, auth guards          │
│  Connects server state to domain UI via props               │
├─────────────────────────────────────────────────────────────┤
│  State Layer: Stores, Queries & Hooks                       │
│  Zustand stores, TanStack Query hooks, custom domain hooks  │
│  The bridge between raw data and UI consumption             │
├─────────────────────────────────────────────────────────────┤
│  L2: Domain UI Components                                   │
│  Domain-specific views that speak the business language      │
│  Composed entirely from L3 components                       │
├─────────────────────────────────────────────────────────────┤
│  L3: Design System (Dumb UI)                                │
│  Shadcn/ui, Radix primitives, pure presentational           │
│  Zero business knowledge                                    │
└─────────────────────────────────────────────────────────────┘
```

#### L1: Data Sourcing & Routing Components

**Responsibility**: L1 is the entry point for every page. Its ONLY job is to source
data and wire it into L2 components via props. It handles route parameters, guards,
layout composition, and loading/error boundaries.

**What belongs here**:
- Route components (one per route segment)
- Auth guards and redirect logic
- Calls to State Layer hooks to fetch data
- Passing fetched data as props to L2 components
- Composing multiple L2 components into a page (e.g., `<Sidebar />` + `<BoardContent />`)

**What does NOT belong here**:
- Business logic or data transformation
- Direct API calls (use State Layer hooks instead)
- Visual styling, CSS classes, or UI markup (delegate to L2)
- L3 components (`<Button>`, `<Card>`, `<Skeleton>` - those are composed by L2)
- Native HTML tags (`<div>`, `<span>`, `<section>`) - use L2 layout components instead
- Loading/error state rendering (L2 handles its own via props like `isLoading`)

```tsx
// GOOD: L1 route component - data sourcing only, delegates everything to L2
function TaskBoardPage() {
  const { boardId } = useParams();
  const { data: board, isLoading } = useBoardQuery(boardId);
  const { data: tasks } = useTasksByBoardQuery(boardId);

  return (
    <KanbanBoard board={board} tasks={tasks} isLoading={isLoading} />
  );
}

// BAD: L1 rendering L3 components directly
function TaskBoardPage() {
  const { data, isLoading } = useBoardQuery(boardId);
  if (isLoading) return <Skeleton className="h-64" />;  // L3 in L1!
  return <KanbanBoard board={data} />;
}

// BAD: L1 using native HTML and styling
function TaskBoardPage() {
  const [tasks, setTasks] = useState([]);
  useEffect(() => {
    fetch('/api/tasks').then(r => r.json()).then(setTasks); // Direct API call
  }, []);
  return <div className="grid grid-cols-3">...</div>;     // Native HTML in L1!
}
```

#### State Layer: Zustand Stores, TanStack Query & Custom Hooks

**Responsibility**: This layer is the single source of truth for all application state.
It cleanly separates **server state** (data from the API) from **client state** (UI
preferences, form drafts, optimistic updates, offline queue).

**Server State (TanStack Query)**:
- All API data fetching, caching, and synchronization
- Automatic background refetching and stale-while-revalidate
- Mutation hooks with optimistic updates and rollback
- Offline mutation queue (pairs with PWA service worker)
- Cache invalidation after mutations

```tsx
// Server state: TanStack Query hook
function useTasksQuery(filters: TaskFilters) {
  return useQuery({
    queryKey: ['tasks', filters],
    queryFn: () => tasksApi.list(filters),
  });
}

function useCompleteTaskMutation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: tasksApi.complete,
    onMutate: async (taskId) => {
      // Optimistic update
      await queryClient.cancelQueries({ queryKey: ['tasks'] });
      queryClient.setQueryData(['tasks'], (old) =>
        old.map(t => t.id === taskId ? { ...t, status: 'done' } : t)
      );
    },
    onSettled: () => queryClient.invalidateQueries({ queryKey: ['tasks'] }),
  });
}
```

**Client State (Zustand)**:
- UI state: sidebar open/closed, active view (kanban/list), selected filters
- Theme preference, locale selection
- Onboarding progress (local until synced)
- Form drafts and unsaved changes
- Offline operation queue
- Anything that does NOT come from the server

```tsx
// Client state: Zustand store
interface TaskViewStore {
  activeView: 'kanban' | 'list';
  selectedFilters: TaskFilters;
  setActiveView: (view: 'kanban' | 'list') => void;
  setFilters: (filters: TaskFilters) => void;
}

const useTaskViewStore = create<TaskViewStore>()(
  persist(
    (set) => ({
      activeView: 'kanban',
      selectedFilters: {},
      setActiveView: (view) => set({ activeView: view }),
      setFilters: (filters) => set({ selectedFilters: filters }),
    }),
    { name: 'task-view-preferences' }
  )
);
```

**Custom Domain Hooks**:
- Compose Zustand stores + TanStack Query into domain-specific hooks
- Encapsulate complex state interactions
- Provide a clean API for L1 and L2 components

```tsx
// Custom hook: composes server + client state
function useTaskBoard(boardId: string) {
  const { data: board, isLoading } = useBoardQuery(boardId);
  const { data: tasks } = useTasksByBoardQuery(boardId);
  const completeMutation = useCompleteTaskMutation();
  const { activeView, setActiveView } = useTaskViewStore();

  const completeTask = (taskId: string) => completeMutation.mutate(taskId);

  return { board, tasks, isLoading, activeView, setActiveView, completeTask };
}
```

**Rules for the State Layer**:
- TanStack Query owns ALL server data. Never `useState` + `useEffect` + `fetch`.
- Zustand owns ALL client-only state. Never React Context for frequently-changing state.
- React Context is reserved for low-frequency cross-cutting concerns (theme provider, i18n provider, auth provider that wraps the query client).
- Custom hooks compose stores + queries. Components never import both a store AND a query directly - they use the composed hook.
- Query keys follow convention: `[domain, resource, ...params]` (e.g., `['tasks', 'list', { status: 'todo' }]`).
- Zustand stores follow convention: `use{Domain}{Concept}Store` (e.g., `useTaskViewStore`, `useAuthSessionStore`).

#### L2: Domain UI Components

**Responsibility**: L2 components are the visual representation of domain concepts.
They speak the language of the business: tasks, boards, columns, priorities - not
buttons, inputs, or grids. They receive domain data as props and compose L3 components
to render it.

**What belongs here**:
- Components that represent domain concepts: `KanbanBoard`, `TaskCard`, `LoginForm`, `AuditLogTable`
- Domain-specific layout decisions (a task card shows title, priority badge, due date)
- Event handlers that call domain actions (onComplete, onMove, onDelete)
- Conditional rendering based on domain state (empty board, overdue task, locked account)
- Domain-specific validation feedback

**What does NOT belong here**:
- Native HTML tags (`<div>`, `<span>`, `<button>`, `<input>`) - use L3 components instead
- Direct API calls or store access (receive data via props or use State Layer hooks)
- Generic/reusable presentational logic (that belongs in L3)
- Route awareness (no `useParams`, `useNavigate` - that's L1)

```tsx
// GOOD: L2 domain component using L3 components only
function TaskCard({ task, onComplete, onDelete }: TaskCardProps) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardHeader>
        <CardTitle>{task.title}</CardTitle>
        <PriorityBadge priority={task.priority} />
      </CardHeader>
      <CardContent>
        {task.dueDate && <DueDateLabel date={task.dueDate} />}
        <TagList tags={task.tags} />
      </CardContent>
      <CardFooter>
        <Button variant="ghost" onClick={() => onComplete(task.id)}>
          {t('tasks.card.complete')}
        </Button>
      </CardFooter>
    </Card>
  );
}

// BAD: L2 using native HTML
function TaskCard({ task }: TaskCardProps) {
  return (
    <div className="border rounded p-4">          {/* native HTML! */}
      <h3>{task.title}</h3>                        {/* native HTML! */}
      <span className="text-red-500">High</span>   {/* native HTML! */}
    </div>
  );
}
```

**L2 can reference domain types** (enums, interfaces from the domain types folder).
This is what makes L2 "domain-aware" - it knows what a `Priority` is, what a `TaskStatus`
means, and how to render them with semantic meaning.

#### L3: Design System / Dumb UI Components

**Responsibility**: L3 is our design system. These components are completely generic,
reusable, and have ZERO knowledge of any business domain. They are the atoms and
molecules of our UI. Shadcn/ui components live here, along with any custom primitives
we build on top of Radix.

**What belongs here**:
- Shadcn/ui components (Button, Card, Input, Dialog, Toast, etc.)
- Custom design system primitives built on Radix (e.g., `DragHandle`, `Badge`, `Skeleton`)
- Layout primitives (Stack, Grid, Container)
- Typography components (Heading, Text, Label)
- Icon components
- Animation primitives

**What does NOT belong here**:
- Any business/domain terms (no `TaskCard`, no `LoginForm`, no `Priority`)
- Data fetching or state management
- Domain types in props (use generic types: `string`, `ReactNode`, `variant`)
- Business logic or conditional rendering based on domain state

**Accessibility is paramount in L3**: Since all UI ultimately renders through L3,
this is where we enforce WCAG 2.1 AA compliance. Radix primitives handle focus
management, keyboard navigation, and ARIA attributes. Every L3 component must:
- Be keyboard accessible
- Have proper ARIA roles and labels
- Meet 4.5:1 contrast ratio
- Support screen readers

```tsx
// GOOD: L3 generic component
interface BadgeProps {
  variant: 'default' | 'success' | 'warning' | 'danger' | 'info';
  children: ReactNode;
}

function Badge({ variant, children }: BadgeProps) {
  return (
    <span className={cn(badgeVariants({ variant }))}>
      {children}
    </span>
  );
}

// BAD: L3 with domain knowledge
function PriorityBadge({ priority }: { priority: Priority }) {
  // This knows about Priority domain type! Should be in L2.
}
```

### Frontend Project Structure

```
client/
├── src/
│   ├── app/                         # App shell, routing, providers
│   │   ├── routes/                  # L1: Route/page components
│   │   │   ├── auth/                #   Login, Register, ResetPassword pages
│   │   │   ├── tasks/               #   Board, List pages
│   │   │   ├── admin/               #   Admin panel pages
│   │   │   └── onboarding/          #   Onboarding flow pages
│   │   └── providers/               # Global providers (QueryClient, Auth, Theme, i18n)
│   │
│   ├── domains/                     # L2 + State Layer, organized by domain
│   │   ├── auth/
│   │   │   ├── components/          # L2: LoginForm, RegisterForm, MfaSetup
│   │   │   ├── hooks/               # State: useAuth, useCurrentUser, useLoginMutation
│   │   │   ├── stores/              # State: useAuthSessionStore (Zustand)
│   │   │   ├── api/                 # State: API client functions for auth endpoints
│   │   │   └── types/               # Domain types: User, Role, AuthState
│   │   ├── tasks/
│   │   │   ├── components/          # L2: KanbanBoard, TaskCard, TaskList, QuickAdd
│   │   │   ├── hooks/               # State: useTasks, useBoard, useCompleteTask
│   │   │   ├── stores/              # State: useTaskViewStore, useOfflineQueueStore
│   │   │   ├── api/                 # State: API client functions for task endpoints
│   │   │   └── types/               # Domain types: TaskItem, Board, Column, Priority
│   │   ├── admin/
│   │   │   ├── components/          # L2: AuditLogTable, UserManagement, SystemHealth
│   │   │   ├── hooks/               # State: useAuditLog, useAdminUsers
│   │   │   ├── stores/              # State: useAdminFilterStore
│   │   │   ├── api/                 # State: API client functions for admin endpoints
│   │   │   └── types/               # Domain types: AuditEntry, AdminUser
│   │   └── onboarding/
│   │       ├── components/          # L2: WelcomeScreen, OnboardingStep, Celebration
│   │       ├── hooks/               # State: useOnboarding, useOnboardingProgress
│   │       ├── stores/              # State: useOnboardingStore
│   │       ├── api/                 # State: API client functions for onboarding
│   │       └── types/               # Domain types: OnboardingStep, OnboardingProgress
│   │
│   ├── ui/                          # L3: Design system (domain-agnostic)
│   │   ├── primitives/              #   Radix-based primitives (Badge, Tooltip, etc.)
│   │   ├── layout/                  #   Stack, Grid, Container, Separator
│   │   ├── feedback/                #   Toast, Skeleton, Spinner, EmptyState
│   │   ├── forms/                   #   Input, Select, Checkbox, DatePicker, Field
│   │   ├── data-display/            #   Card, Table, List, Avatar
│   │   ├── navigation/              #   Tabs, Breadcrumb, Sidebar, NavLink
│   │   ├── overlay/                 #   Dialog, Sheet, Popover, DropdownMenu
│   │   └── typography/              #   Heading, Text, Label, Code
│   │
│   ├── lib/                         # Shared utilities
│   │   ├── api-client.ts            #   Configured fetch/axios instance
│   │   ├── analytics.ts             #   Analytics event tracking service
│   │   ├── i18n.ts                  #   i18next configuration
│   │   └── utils.ts                 #   cn(), formatDate(), etc.
│   ├── i18n/                        # Translation JSON files (en, pt-BR, es)
│   └── test/                        # Test utilities, factories, MSW handlers
│
├── e2e/                             # Playwright E2E tests
```

### Layer Import Rules

```
                    ┌──────────────┐
                    │  L1 (Routes) │  Can import: State Layer, L2
                    └──────┬───────┘  CANNOT import: L3, native HTML
                           │ passes data via props
                    ┌──────▼────────────────┐
                    │  State Layer           │  Can import: api/, types/, lib
                    │  (Stores + Queries     │  Zustand stores, TanStack hooks,
                    │   + Custom Hooks)      │  custom composed hooks
                    └──────┬────────────────┘
                           │ consumed by L1 and L2
                    ┌──────▼───────┐
                    │  L2 (Domain) │  Can import: L3, types/, State Layer hooks
                    └──────┬───────┘  CANNOT import: L1, native HTML
                           │ composes
                    ┌──────▼───────┐
                    │  L3 (Design) │  Can import: Radix, Tailwind, React
                    └──────────────┘  CANNOT import: L1, L2, domain types, State Layer
```

| Layer | Can Import | Cannot Import |
|-------|-----------|---------------|
| **L1** (Routes) | L2, State Layer hooks, lib, types | L3, native HTML tags, direct API calls (`fetch`/`axios`) |
| **State Layer** (Stores/Queries/Hooks) | api/ functions, domain types, lib | L1, L2, L3 components |
| **L2** (Domain UI) | L3 components, domain types, State Layer hooks, `useTranslation` | L1, native HTML tags (`<div>`, `<button>`, etc.), direct API calls |
| **L3** (Design System) | Radix, Tailwind, React, generic TypeScript types | L1, L2, State Layer, domain types, `useTranslation` |

### Backend Layer Rules

| Layer | Can Import | Cannot Import |
|-------|-----------|---------------|
| **Domain** | Common (base classes, shared VOs) | Application, Infrastructure, API |
| **Application** | Domain | Infrastructure, API |
| **Infrastructure** | Domain, Application interfaces | API |
| **API** | Application, Infrastructure (via DI) | Domain directly |

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

State management is split by state ownership:

```tsx
// Server state: TanStack Query (configured via QueryClientProvider)
// Client state: Zustand stores (no provider needed - stores are plain hooks)
// Cross-cutting: React Context ONLY for low-frequency globals:
//   - QueryClientProvider (TanStack Query)
//   - AuthProvider (wraps query client, manages JWT refresh)
//   - ThemeProvider (light/dark, reads from Zustand but provides CSS vars)
//   - I18nProvider (react-i18next)
//   - AnalyticsProvider (event tracking context)
```

**When to use what**:

| State Type | Tool | Example |
|------------|------|---------|
| Data from API | TanStack Query | Task list, user profile, board data |
| UI preferences | Zustand (with `persist`) | Active view, theme, locale, sidebar state |
| Form drafts | Zustand | Unsaved task edits, registration form progress |
| Offline queue | Zustand (with `persist`) | Operations pending sync |
| Auth tokens | Zustand (with `persist`) + TanStack Query | Token in store, user profile in query |
| Theme/i18n | React Context | Low-frequency, tree-wide, CSS variable driven |

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
