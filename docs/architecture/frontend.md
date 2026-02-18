# Frontend Architecture

> **Source**: Extracted from GUIDELINES.md §2 (Architecture: Domain-Driven Design — frontend sections), §3 (Code Quality — frontend naming), §5 (Dependency Injection — frontend), §6 (Error Handling — frontend), §8 (Performance Guidelines), §9 (Accessibility Guidelines), §10 (i18n Guidelines)
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Overview

The frontend is organized along two orthogonal dimensions:

1. **Architecture Tiers** - Separation of concerns and data flow. Each tier has a single
   responsibility: routing, structural composition, state management, or visual rendering.
2. **Component Taxonomy** - Composition granularity within the visual rendering tier. Small
   primitives compose into bigger domain-aware pieces.

These are independent concepts. Architecture Tiers answer *"what is this code responsible
for?"* Component Taxonomy answers *"how big and domain-aware is this piece of UI?"*

---

## Part 1: Architecture Tiers

The application flows data top-down through four tiers:

```
┌─────────────────────────────────────────────────────────────┐
│  Routing                                                     │
│  URL mapping, route guards, data sourcing                    │
│  Selects a Page and feeds it data from the State tier        │
├─────────────────────────────────────────────────────────────┤
│  Pages & Layouts                                             │
│  Structural composition shells (DashboardLayout, AuthLayout) │
│  Arrange Domain Components within structural regions         │
│  CAN use Design System layout primitives and native HTML     │
├─────────────────────────────────────────────────────────────┤
│  State Management                                            │
│  Zustand stores, TanStack Query hooks, custom domain hooks   │
│  The bridge between raw API data and UI consumption          │
├─────────────────────────────────────────────────────────────┤
│  Components                                                  │
│  All visual UI - organized by the Component Taxonomy below   │
│  Domain-aware and domain-agnostic pieces live here           │
└─────────────────────────────────────────────────────────────┘
```

### Routing

**Responsibility**: Routes are the entry point for every URL. Their ONLY job is to map
a URL to a Page, enforce auth guards, source data from the State tier, and pass it down.
A route does not render anything visual itself.

**What belongs here**:
- Route definitions (one per URL segment)
- Auth guards and redirect logic
- Calls to State tier hooks to fetch data
- Selecting which Page/Layout to render
- Passing fetched data as props to the Page

**What does NOT belong here**:
- Business logic or data transformation
- Direct API calls (use State tier hooks instead)
- Visual styling, CSS classes, or any UI markup
- Design System components, Domain Components, native HTML tags
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
  return <KanbanBoard board={data} />;                   // Skipping the Page tier!
}
```

### Pages & Layouts

**Responsibility**: Pages and Layouts are the structural shells of the application.
They define WHERE things go on screen - sidebars, headers, content areas, footers -
but NOT what domain content is displayed. Pages select a Layout and plug Domain
Components into its slots. Layouts define the structural regions.

This is the **only tier** where native HTML tags for structural wrappers and Design
System layout primitives are acceptable, because its job is inherently structural.

**Layouts** - Reusable structural shells shared across multiple pages:

```tsx
// Layout: structural shell with named slots
// CAN use Design System layout primitives and native HTML for structure
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

**Pages** - Compose a Layout with Domain Components:

```tsx
// Page: plugs Domain Components into Layout slots
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
- Page components that select a Layout and fill its slots with Domain Components
- Native HTML for structural wrappers (`<div>`, `<aside>`, `<main>`, `<header>`)
- Design System layout primitives (`Stack`, `Grid`, `Container`, `Separator`)
- Structural CSS (flexbox, grid, positioning, spacing)

**What does NOT belong here**:
- Domain logic or domain-specific rendering (that's a Domain Component)
- Data fetching or State tier hooks (that's the Route's job)
- Non-layout Design System components (`<Button>`, `<Card>`, `<Dialog>`)
- Direct API calls

### State Management

**Responsibility**: This tier is the single source of truth for all application state.
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
- Provide a clean API for Routes and Domain Components

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

**Rules for the State Management tier**:
- TanStack Query owns ALL server data. Never `useState` + `useEffect` + `fetch`.
- Zustand owns ALL client-only state. Never React Context for frequently-changing state.
- React Context is reserved for low-frequency cross-cutting concerns (theme provider, i18n provider, auth provider that wraps the query client).
- Custom hooks compose stores + queries. Components never import both a store AND a query directly - they use the composed hook.
- Query keys follow convention: `[domain, resource, ...params]` (e.g., `['tasks', 'list', { status: 'todo' }]`).
- Zustand stores follow convention: `use{Domain}{Concept}Store` (e.g., `useTaskViewStore`, `useAuthSessionStore`).

### Components

Components are the visual building blocks of the application. They are organized by the
**Component Taxonomy** described in Part 2 below. This tier contains both domain-aware
components (Domain Views, Widgets, Atoms) and domain-agnostic primitives (Design System).

---

## Part 2: Component Taxonomy

Within the Components tier, UI pieces are organized by **domain awareness** and
**composition granularity**. This answers: *"how big is this component, and does it
know about the business domain?"*

```
Domain Views    ← Large compositions that fill a page region
     │              KanbanBoard, TaskListView, AuditLogPanel, LoginForm
     │              Compose Domain Widgets together
     ▼
Domain Widgets  ← Mid-size blocks representing a single concept
     │              TaskCard, KanbanColumn, UserRow, OnboardingStep
     │              Compose Domain Atoms + Design System
     ▼
Domain Atoms    ← Smallest domain-aware units with semantic meaning
     │              PriorityBadge, DueDateLabel, TagList, TaskStatusIcon
     │              Thin bridge: maps domain values → Design System variants
     ▼
Design System   ← Pure visual primitives, zero business knowledge
                    Button, Badge, Card, Dialog, Input, Skeleton
                    Shadcn/ui, Radix, Tailwind - completely generic
```

The top three levels are **domain-aware** - they know about tasks, priorities, boards,
users. The bottom level is **domain-agnostic** - it only knows about visual concepts
like "variant", "size", "children".

### Design System (Domain-Agnostic)

**Responsibility**: The Design System is the project's library of generic, reusable visual
primitives. These components have ZERO knowledge of any business domain. Shadcn/ui
components live here, along with any custom primitives built on Radix.

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

**Accessibility is paramount in the Design System**: Since all UI ultimately renders
through these primitives, this is where WCAG 2.1 AA compliance is enforced. Radix
primitives handle focus management, keyboard navigation, and ARIA attributes. Every
Design System component must:
- Be keyboard accessible
- Have proper ARIA roles and labels
- Meet 4.5:1 contrast ratio
- Support screen readers

```tsx
// Design System: generic component - no domain knowledge
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

// BAD: Design System component with domain knowledge
function PriorityBadge({ priority }: { priority: Priority }) {
  // This knows about Priority domain type! Should be a Domain Atom.
}
```

### Domain Atoms (Domain-Aware, Smallest)

**Responsibility**: Domain Atoms are the thinnest bridge between domain semantics and
the Design System. They translate a domain concept (like `Priority.High`) into a Design
System variant (like `<Badge variant="danger">`). An atom knows what a domain value
*means* visually.

```tsx
// Domain Atom: translates domain value → Design System variant
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

// Domain Atom: date display with overdue awareness
function DueDateLabel({ date }: { date: Date }) {
  const isOverdue = date < new Date();
  return <Text color={isOverdue ? 'danger' : 'muted'}>{formatDate(date)}</Text>;
}
```

### Domain Widgets (Domain-Aware, Mid-Size)

**Responsibility**: Domain Widgets are reusable blocks that represent a single domain
concept. They compose Domain Atoms and Design System components into a cohesive unit
that can be used in multiple Domain Views.

```tsx
// Domain Widget: composes Domain Atoms + Design System
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
```

### Domain Views (Domain-Aware, Largest)

**Responsibility**: Domain Views are the largest domain-aware compositions. They fill
an entire page region (the content area, a sidebar panel, a modal body). Pages plug
Domain Views into Layout slots.

```tsx
// Domain View: composes Domain Widgets into a full board
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
```

### What Domain Components Cannot Do

All three domain levels (Views, Widgets, Atoms) share these constraints:
- **No native HTML tags** (`<div>`, `<span>`, `<button>`, `<input>`) - use Design System components instead
- **No direct API calls** - receive data via props or use State tier hooks
- **No route awareness** - no `useParams`, `useNavigate` (that's the Routing tier's job)
- **No structural page layout** - no sidebars, headers (that's the Pages & Layouts tier)

Domain Components **CAN**:
- Import and use Design System components
- Import domain types (enums, interfaces from the `types/` folder)
- Use State tier hooks for user interactions (mutations, client state)
- Use `useTranslation` for i18n

> **Note**: The Views/Widgets/Atoms taxonomy is organizational - not a permission
> boundary. All three share the same import rules. However, the recommendation is that Atoms
> prefer pure props over State hooks to maximize reusability and testability.

---

## Frontend Project Structure

```
client/
├── src/
│   ├── app/                         # App shell, routing, providers
│   │   ├── routes/                  # Routing tier: URL mapping + data sourcing
│   │   │   ├── auth/                #   Auth routes (login, register, reset)
│   │   │   ├── tasks/               #   Task routes (board, list)
│   │   │   ├── admin/               #   Admin routes (users, audit, health)
│   │   │   └── onboarding/          #   Onboarding routes
│   │   ├── pages/                   # Pages: compose Layouts + Domain Components
│   │   │   ├── TaskBoardPage.tsx
│   │   │   ├── TaskListPage.tsx
│   │   │   ├── LoginPage.tsx
│   │   │   ├── RegisterPage.tsx
│   │   │   ├── AdminUsersPage.tsx
│   │   │   └── ...
│   │   ├── layouts/                 # Layouts: structural shells
│   │   │   ├── DashboardLayout.tsx  #   Sidebar + Header + Content
│   │   │   ├── AuthLayout.tsx       #   Centered card
│   │   │   ├── AdminLayout.tsx      #   Admin sidebar + Content
│   │   │   ├── OnboardingLayout.tsx #   Onboarding stepper shell
│   │   │   └── PublicLayout.tsx     #   Landing, marketing pages
│   │   └── providers/               # Global providers (QueryClient, Auth, Theme, i18n)
│   │
│   ├── domains/                     # Domain Components + State, organized by domain
│   │   ├── auth/
│   │   │   ├── components/          # Domain Components: LoginForm, RegisterForm, MfaSetup
│   │   │   ├── hooks/               # State: useAuth, useCurrentUser, useLoginMutation
│   │   │   ├── stores/              # State: useAuthSessionStore (Zustand)
│   │   │   ├── api/                 # State: API client functions for auth endpoints
│   │   │   └── types/               # Domain types: User, Role, AuthState
│   │   ├── tasks/
│   │   │   ├── components/          # Domain Components
│   │   │   │   ├── views/           #   Domain Views: KanbanBoard, TaskListView
│   │   │   │   ├── widgets/         #   Domain Widgets: TaskCard, KanbanColumn
│   │   │   │   └── atoms/           #   Domain Atoms: PriorityBadge, DueDateLabel, TagList
│   │   │   ├── hooks/               # State: useTasks, useBoard, useCompleteTask
│   │   │   ├── stores/              # State: useTaskViewStore, useOfflineQueueStore
│   │   │   ├── api/                 # State: API client functions for task endpoints
│   │   │   └── types/               # Domain types: TaskItem, Board, Column, Priority
│   │   ├── admin/
│   │   │   ├── components/          # Domain Components: AuditLogTable, UserManagement
│   │   │   ├── hooks/               # State: useAuditLog, useAdminUsers
│   │   │   ├── stores/              # State: useAdminFilterStore
│   │   │   ├── api/                 # State: API client functions for admin endpoints
│   │   │   └── types/               # Domain types: AuditEntry, AdminUser
│   │   ├── onboarding/
│   │   │   ├── components/          # Domain Components: WelcomeScreen, Celebration
│   │   │   ├── hooks/               # State: useOnboarding, useOnboardingProgress
│   │   │   ├── stores/              # State: useOnboardingStore
│   │   │   ├── api/                 # State: API client functions for onboarding
│   │   │   └── types/               # Domain types: OnboardingStep, OnboardingProgress
│   │   └── shared/                  # Shared Domain Components across domains
│   │       └── components/          #   AppSidebar, AppHeader, UserMenu, NotificationBell
│   │
│   ├── ui/                          # Design System (domain-agnostic)
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

---

## Import Rules

```
                    ┌────────────────┐
                    │    Routing     │  Can import: Pages, State hooks
                    └──────┬─────────┘  CANNOT: Domain Components, Design System, native HTML
                           │ passes data via props
                    ┌──────▼─────────┐
                    │ Pages/Layouts  │  Can import: Layouts, Domain Components,
                    └──────┬─────────┘  Design System layout primitives, native HTML
                           │ fills layout slots
                    ┌──────▼────────────────┐
                    │  State Management      │  Can import: api/, types/, lib/
                    │  (Stores + Queries     │  Zustand stores, TanStack hooks,
                    │   + Custom Hooks)      │  custom composed hooks
                    └──────┬────────────────┘
                           │ consumed by Routing, Pages, and Domain Components
              ┌────────────▼───────────────────────┐
              │         Components                  │
              │  ┌─────────────┐  ┌──────────────┐ │
              │  │   Domain    │  │   Design     │ │
              │  │   Views /   │→ │   System     │ │
              │  │   Widgets / │  │  (Shadcn/ui, │ │
              │  │   Atoms     │  │   Radix)     │ │
              │  └─────────────┘  └──────────────┘ │
              └────────────────────────────────────┘
```

| Tier / Level | Can Import | Cannot Import |
|---|---|---|
| **Routing** | Pages, State hooks | Domain Components, Design System, native HTML, direct API calls |
| **Pages** | Layouts, Domain Components, State hooks | Design System (except layout primitives), direct API calls |
| **Layouts** | Design System layout primitives, native HTML (structural only) | Domain Components, State hooks, domain types |
| **State Management** | api/ functions, domain types, lib/ | Routing, Pages, Domain Components, Design System |
| **Domain Components** (Views, Widgets, Atoms) | Design System, domain types, State hooks, `useTranslation` | Routing, Pages, native HTML, direct API calls |
| **Design System** | Radix, Tailwind, React, generic TS types | Routing, Pages, Domain Components, State, domain types, `useTranslation` |

**Key distinctions**:
- **Pages CAN import Domain Components** (to fill layout slots) but **Layouts CANNOT** (layouts are generic shells with `children`/slot props)
- **Pages CAN import State hooks** when a page needs to combine data for multiple Domain Components in different layout slots
- **Layouts CAN use Design System layout primitives** (`ScrollArea`, `Separator`) and native HTML for structure, but NOT interactive Design System components (`Button`, `Dialog`)
- **Domain Views/Widgets/Atoms** share the same import rules - the taxonomy is organizational, not a permission boundary

---

## Dependency Injection (Frontend)

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

## Naming Conventions

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

---

## Error Handling (Frontend)

- API calls: try/catch with error boundary fallbacks
- Component errors: React Error Boundaries
- User-facing errors: Toast notifications with actionable messages
- Never show stack traces or technical details to users

---

## Performance Guidelines (Frontend)

- React.memo for expensive components, useMemo/useCallback where measured
- Code-split routes with React.lazy
- Optimize images, use WebP where possible
- Cache static assets with service worker

---

## Accessibility Guidelines

- All interactive elements must be keyboard-navigable
- All images must have alt text
- Color contrast must meet WCAG 2.1 AA (4.5:1 for text)
- Form inputs must have associated labels
- Error messages must be associated with their fields (aria-describedby)
- Focus management: return focus after modal close
- Screen reader testing for all user flows
- Use Radix UI primitives (they handle most accessibility)

---

## i18n Guidelines

- All user-facing strings must use translation keys
- No hardcoded strings in components
- Translation keys follow: `{domain}.{component}.{element}`
  - Example: `tasks.kanban.columnHeader`, `auth.login.submitButton`
- Date/number formatting via locale-aware formatters
- Backend validation messages also use translation keys
- Default language: English (en)
- MVP languages: English (en), Portuguese (pt-BR), Spanish (es)
