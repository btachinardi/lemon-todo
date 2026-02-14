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

### Frontend Architecture

The frontend follows a strict layered architecture that separates concerns and enforces
unidirectional data flow. Each layer has a single responsibility and clear import rules.

```
┌─────────────────────────────────────────────────────────────┐
│  Routes                                                     │
│  URL mapping, route guards, data sourcing                   │
│  Selects a Page and feeds it data from the State Layer      │
├─────────────────────────────────────────────────────────────┤
│  Pages & Layouts                                            │
│  App-level composition shells (DashboardLayout, AuthLayout) │
│  Arrange L2 components within structural regions            │
│  CAN use L3 layout primitives and native HTML for structure │
├─────────────────────────────────────────────────────────────┤
│  State Layer: Stores, Queries & Hooks                       │
│  Zustand stores, TanStack Query hooks, custom domain hooks  │
│  The bridge between raw data and UI consumption             │
├─────────────────────────────────────────────────────────────┤
│  L2: Domain UI Components                                   │
│  Domain-specific views that speak the business language      │
│  Composed entirely from L3 components, multiple granularity  │
│  levels: Views → Widgets → Atoms                            │
├─────────────────────────────────────────────────────────────┤
│  L3: Design System (Dumb UI)                                │
│  Shadcn/ui, Radix primitives, pure presentational           │
│  Zero business knowledge                                    │
└─────────────────────────────────────────────────────────────┘
```

#### Routes (Data Sourcing & URL Mapping)

**Responsibility**: Routes are the entry point for every URL. Their ONLY job is to
map a URL to a Page component, enforce auth guards, source data from the State Layer,
and pass it down. A route does not render anything visual itself.

**What belongs here**:
- Route definitions (one per URL segment)
- Auth guards and redirect logic
- Calls to State Layer hooks to fetch data
- Selecting which Page/Layout to render
- Passing fetched data as props to the Page

**What does NOT belong here**:
- Business logic or data transformation
- Direct API calls (use State Layer hooks instead)
- Visual styling, CSS classes, or any UI markup
- L3 components, L2 components, native HTML tags
- Loading/error state rendering

```tsx
// GOOD: Route - pure data sourcing, delegates to Page
function TaskBoardRoute() {
  const { boardId } = useParams();
  const { data: board, isLoading } = useBoardQuery(boardId);
  const { data: tasks } = useTasksByBoardQuery(boardId);

  return (
    <TaskBoardPage board={board} tasks={tasks} isLoading={isLoading} />
  );
}

// BAD: Route rendering UI directly
function TaskBoardRoute() {
  const { data, isLoading } = useBoardQuery(boardId);
  if (isLoading) return <Skeleton className="h-64" />;  // Visual rendering in route!
  return <KanbanBoard board={data} />;                   // Skipping the Page layer!
}
```

#### Pages & Layouts (App-Level Composition)

**Responsibility**: Pages and Layouts are the structural shells of the application.
They define WHERE things go on screen - sidebars, headers, content areas, footers -
but NOT what domain content is displayed. Pages select a Layout and plug L2 domain
components into its slots. Layouts define the structural regions.

This is the **only layer** where native HTML tags for structural wrappers and L3
layout primitives are acceptable, because its job is inherently structural.

**Layouts** - Reusable structural shells shared across multiple pages:

```tsx
// Layout: structural shell with named slots
// CAN use L3 layout primitives and native HTML for structure
function DashboardLayout({ sidebar, header, children }: DashboardLayoutProps) {
  return (
    <div className="flex h-screen">
      <aside className="w-64 border-r">{sidebar}</aside>
      <div className="flex flex-col flex-1">
        <header className="h-14 border-b">{header}</header>
        <main className="flex-1 overflow-auto p-6">{children}</main>
      </div>
    </div>
  );
}

// Layout: simpler shell for auth pages
function AuthLayout({ children }: AuthLayoutProps) {
  return (
    <div className="flex items-center justify-center min-h-screen bg-muted">
      <div className="w-full max-w-md">{children}</div>
    </div>
  );
}
```

**Pages** - Compose a Layout with L2 domain components:

```tsx
// Page: plugs L2 components into Layout slots
function TaskBoardPage({ board, tasks, isLoading }: TaskBoardPageProps) {
  return (
    <DashboardLayout
      sidebar={<AppSidebar />}
      header={<AppHeader />}
    >
      <KanbanBoard board={board} tasks={tasks} isLoading={isLoading} />
    </DashboardLayout>
  );
}

// Page: auth page using auth layout
function LoginPage() {
  return (
    <AuthLayout>
      <LoginForm />
    </AuthLayout>
  );
}
```

**What belongs in Pages & Layouts**:
- Layout shells with structural regions (sidebar, header, content, footer)
- Page components that select a Layout and fill its slots with L2 components
- Native HTML for structural wrappers (`<div>`, `<aside>`, `<main>`, `<header>`)
- L3 layout primitives (`Stack`, `Grid`, `Container`, `Separator`)
- Structural CSS (flexbox, grid, positioning, spacing)

**What does NOT belong here**:
- Domain logic or domain-specific rendering (that's L2)
- Data fetching or State Layer hooks (that's the Route's job)
- Non-layout L3 components (`<Button>`, `<Card>`, `<Dialog>`)
- Direct API calls

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

L2 naturally has **multiple levels of granularity**. This is not a strict sub-layer
hierarchy, but a naming convention that helps reason about composition:

```
L2 Views      Large composite views that fill a page region
              KanbanBoard, TaskListView, AuditLogPanel, LoginForm
              Compose multiple L2 Widgets together
                    │
                    ▼
L2 Widgets    Mid-size domain components that represent a single concept
              TaskCard, KanbanColumn, UserRow, OnboardingStep
              Compose L2 Atoms and L3 components
                    │
                    ▼
L2 Atoms      Small domain-specific elements with semantic meaning
              PriorityBadge, DueDateLabel, TagList, TaskStatusIcon
              Thin wrappers around L3 that map domain values to visual variants
```

**Views** compose Widgets. **Widgets** compose Atoms and L3. **Atoms** are the
thinnest domain wrappers - they translate a domain concept (like `Priority.High`)
into an L3 variant (like `<Badge variant="danger">`). The atom IS the bridge between
domain semantics and generic UI.

**What belongs here**:
- Components that represent domain concepts at any granularity level
- Domain-specific layout decisions (a task card shows title, priority badge, due date)
- Event handlers that call domain actions (onComplete, onMove, onDelete)
- Conditional rendering based on domain state (empty board, overdue task, locked account)
- Domain-specific validation feedback

**What does NOT belong here**:
- Native HTML tags (`<div>`, `<span>`, `<button>`, `<input>`) - use L3 components instead
- Direct API calls or store access (receive data via props or use State Layer hooks)
- Generic/reusable presentational logic (that belongs in L3)
- Route awareness (no `useParams`, `useNavigate` - that's the Route's job)
- Structural page layout (that's the Pages & Layouts layer)

```tsx
// L2 Atom: translates domain value to L3 variant
function PriorityBadge({ priority }: { priority: Priority }) {
  const { t } = useTranslation();
  const variantMap: Record<Priority, BadgeVariant> = {
    [Priority.Critical]: 'danger',
    [Priority.High]: 'warning',
    [Priority.Medium]: 'info',
    [Priority.Low]: 'default',
    [Priority.None]: 'secondary',
  };
  return <Badge variant={variantMap[priority]}>{t(`tasks.priority.${priority}`)}</Badge>;
}

// L2 Widget: composes L2 Atoms and L3 components
function TaskCard({ task, onComplete }: TaskCardProps) {
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

// L2 View: composes L2 Widgets into a full board
function KanbanBoard({ board, tasks, isLoading }: KanbanBoardProps) {
  if (isLoading) return <BoardSkeleton />;
  if (!board) return <EmptyBoard />;

  return (
    <ScrollArea orientation="horizontal">
      {board.columns.map(col => (
        <KanbanColumn
          key={col.id}
          column={col}
          tasks={tasks.filter(t => t.columnId === col.id)}
        />
      ))}
    </ScrollArea>
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
│   │   ├── routes/                  # Routes: URL mapping + data sourcing
│   │   │   ├── auth/                #   Auth routes (login, register, reset)
│   │   │   ├── tasks/               #   Task routes (board, list)
│   │   │   ├── admin/               #   Admin routes (users, audit, health)
│   │   │   └── onboarding/          #   Onboarding routes
│   │   ├── pages/                   # Pages: compose Layouts + L2 components
│   │   │   ├── TaskBoardPage.tsx     #   DashboardLayout + KanbanBoard
│   │   │   ├── TaskListPage.tsx      #   DashboardLayout + TaskListView
│   │   │   ├── LoginPage.tsx         #   AuthLayout + LoginForm
│   │   │   ├── RegisterPage.tsx      #   AuthLayout + RegisterForm
│   │   │   ├── AdminUsersPage.tsx    #   AdminLayout + UserManagement
│   │   │   └── ...
│   │   ├── layouts/                 # Layouts: structural shells
│   │   │   ├── DashboardLayout.tsx   #   Sidebar + Header + Content
│   │   │   ├── AuthLayout.tsx        #   Centered card
│   │   │   ├── AdminLayout.tsx       #   Admin sidebar + Content
│   │   │   ├── OnboardingLayout.tsx  #   Onboarding stepper shell
│   │   │   └── PublicLayout.tsx      #   Landing, marketing pages
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
│   │   │   │   ├── views/           #   L2 Views: KanbanBoard, TaskListView
│   │   │   │   ├── widgets/         #   L2 Widgets: TaskCard, KanbanColumn
│   │   │   │   └── atoms/           #   L2 Atoms: PriorityBadge, DueDateLabel, TagList
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
│   │   ├── onboarding/
│   │   │   ├── components/          # L2: WelcomeScreen, OnboardingStep, Celebration
│   │   │   ├── hooks/               # State: useOnboarding, useOnboardingProgress
│   │   │   ├── stores/              # State: useOnboardingStore
│   │   │   ├── api/                 # State: API client functions for onboarding
│   │   │   └── types/               # Domain types: OnboardingStep, OnboardingProgress
│   │   └── shared/                  # L2 components shared across domains
│   │       └── components/          #   AppSidebar, AppHeader, UserMenu, NotificationBell
│   │
│   ├── ui/                          # L3: Design system (domain-agnostic)
│   │   ├── primitives/              #   Radix-based primitives (Badge, Tooltip, etc.)
│   │   ├── layout/                  #   Stack, Grid, Container, Separator, ScrollArea
│   │   ├── feedback/                #   Toast, Skeleton, Spinner, EmptyState
│   │   ├── forms/                   #   Input, Select, Checkbox, DatePicker, Field
│   │   ├── data-display/            #   Card, Table, List, Avatar
│   │   ├── navigation/              #   Tabs, Breadcrumb, NavLink
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
                    ┌────────────────┐
                    │    Routes      │  Can import: Pages, State Layer
                    └──────┬─────────┘  CANNOT: L2, L3, native HTML
                           │ passes data via props
                    ┌──────▼─────────┐
                    │ Pages/Layouts  │  Can import: Layouts, L2, L3 layout primitives
                    └──────┬─────────┘  CAN use native HTML for structural wrappers
                           │ fills layout slots with L2
                    ┌──────▼────────────────┐
                    │  State Layer           │  Can import: api/, types/, lib
                    │  (Stores + Queries     │  Zustand stores, TanStack hooks,
                    │   + Custom Hooks)      │  custom composed hooks
                    └──────┬────────────────┘
                           │ consumed by Routes, Pages, and L2
                    ┌──────▼───────┐
                    │  L2 (Domain) │  Can import: L3, types/, State Layer hooks
                    └──────┬───────┘  CANNOT: Routes, Pages, native HTML
                           │ composes
                    ┌──────▼───────┐
                    │  L3 (Design) │  Can import: Radix, Tailwind, React
                    └──────────────┘  CANNOT: Routes, Pages, L2, domain types, State Layer
```

| Layer | Can Import | Cannot Import |
|-------|-----------|---------------|
| **Routes** | Pages, State Layer hooks | L2, L3, native HTML, direct API calls |
| **Pages** | Layouts, L2, State Layer hooks | L3 (except layout primitives), direct API calls |
| **Layouts** | L3 layout primitives, native HTML (structural only) | L2, State Layer, domain types |
| **State Layer** | api/ functions, domain types, lib | Routes, Pages, L2, L3 components |
| **L2** (Domain UI) | L3, domain types, State Layer hooks, `useTranslation` | Routes, Pages, native HTML, direct API calls |
| **L3** (Design System) | Radix, Tailwind, React, generic TS types | Routes, Pages, L2, State Layer, domain types, `useTranslation` |

**Subtle but important distinctions**:
- **Pages CAN import L2** (to fill layout slots) but **Layouts CANNOT import L2** (layouts are generic shells with `children`/slot props)
- **Pages CAN import State Layer hooks** when a page needs to combine data for multiple L2 components in different layout slots
- **Layouts CAN use L3 layout primitives** (`ScrollArea`, `Separator`) and native HTML for structure, but NOT interactive L3 components (`Button`, `Dialog`)
- **L2 Views/Widgets/Atoms** all share the same import rules - the granularity is organizational, not a permission boundary

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
