# LemonDo - Product Requirements Document

> **Version**: 1.0 (Initial)
> **Date**: 2026-02-13
> **Status**: Draft
> **Author**: Bruno (Product Owner / Lead Engineer)

---

## 1. Product Vision

**LemonDo** is a task management platform that combines the simplicity of a to-do list with the power of a Kanban board. It is designed for individuals and small teams who need a secure, compliant, and delightful way to organize their work.

### 1.1 Mission Statement

Empower users to capture, organize, and complete their work with zero friction, while maintaining enterprise-grade security and compliance standards.

### 1.2 Target Audience

- **Primary**: Knowledge workers, freelancers, and small team leads who need a personal/team task management tool
- **Secondary**: Organizations in regulated industries (healthcare, finance) that require HIPAA-compliant task management
- **Tertiary**: Product managers and team leads who need visibility into team progress

### 1.3 Value Proposition

LemonDo is the only task management tool that combines consumer-grade UX with enterprise-grade compliance. Users get a beautiful, fast, mobile-first experience while administrators get full auditability and HIPAA-compliant data handling.

---

## 2. Functional Requirements

### FR-001: User Authentication

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-001.1 | Email/password registration with email verification | P0 |
| FR-001.2 | Email/password login with JWT token management | P0 |
| FR-001.3 | Social OAuth login (Google, Microsoft, GitHub) | P1 |
| FR-001.4 | Password reset via email link | P0 |
| FR-001.5 | Session management with refresh tokens | P0 |
| FR-001.6 | Multi-factor authentication (TOTP) | P1 |
| FR-001.7 | Account lockout after failed attempts | P0 |
| FR-001.8 | "Remember me" functionality | P2 |

### FR-002: Role-Based Access Control (RBAC)

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-002.1 | Predefined roles: User, Admin, SystemAdmin | P0 |
| FR-002.2 | Role assignment by SystemAdmin | P0 |
| FR-002.3 | Permission-based endpoint authorization | P0 |
| FR-002.4 | Role hierarchy (SystemAdmin > Admin > User) | P0 |
| FR-002.5 | Custom permission sets per role | P1 |
| FR-002.6 | Role-based UI element visibility | P0 |

### FR-003: Task Management

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-003.1 | Create task with title, description, priority, due date | P0 |
| FR-003.2 | Edit task properties | P0 |
| FR-003.3 | Delete task (soft delete with audit trail) | P0 |
| FR-003.4 | Mark task as complete/incomplete | P0 |
| FR-003.5 | Assign priority levels (None, Low, Medium, High, Critical) | P0 |
| FR-003.6 | Set due dates with reminder notifications | P1 |
| FR-003.7 | Add tags/labels to tasks | P1 |
| FR-003.8 | Task search and filtering | P1 |
| FR-003.9 | Bulk operations (complete, delete, move) | P2 |
| FR-003.10 | Task archiving | P1 |

### FR-004: Kanban Board

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-004.1 | Default columns: To Do, In Progress, Done | P0 |
| FR-004.2 | Drag-and-drop task movement between columns | P0 |
| FR-004.3 | Custom column creation/editing/deletion | P1 |
| FR-004.4 | Column reordering | P1 |
| FR-004.5 | WIP (Work In Progress) limits per column | P2 |
| FR-004.6 | Swimlanes by priority or tag | P2 |

### FR-005: List View

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-005.1 | Sortable table/list of all tasks | P0 |
| FR-005.2 | Sort by title, priority, due date, status, created date | P0 |
| FR-005.3 | Inline editing of task properties | P1 |
| FR-005.4 | Grouping by status, priority, or due date | P1 |
| FR-005.5 | Pagination or infinite scroll | P0 |

### FR-006: System Administration

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-006.1 | User management (view, deactivate, role assignment) | P0 |
| FR-006.2 | Complete audit log of all system actions | P0 |
| FR-006.3 | Audit log search and filtering | P1 |
| FR-006.4 | System health dashboard | P1 |
| FR-006.5 | User activity reports | P1 |
| FR-006.6 | Data export capabilities | P2 |

### FR-007: HIPAA Compliance

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-007.1 | PII/PHI data encryption at rest | P0 |
| FR-007.2 | PII/PHI redaction in system logs | P0 |
| FR-007.3 | PII/PHI redaction in admin views (masked with reveal) | P0 |
| FR-007.4 | Comprehensive audit trail for data access | P0 |
| FR-007.5 | Data retention policies | P1 |
| FR-007.6 | Right to erasure (data deletion) | P1 |
| FR-007.7 | Access control audit reports | P1 |
| FR-007.8 | BAA (Business Associate Agreement) support structure | P2 |

### FR-008: Onboarding System

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-008.1 | Welcome screen after first registration | P0 |
| FR-008.2 | Guided tour: create first task | P0 |
| FR-008.3 | Guided tour: complete first task | P0 |
| FR-008.4 | Guided tour: explore Kanban board | P1 |
| FR-008.5 | Progress indicators during onboarding | P0 |
| FR-008.6 | Skip option for experienced users | P0 |
| FR-008.7 | Onboarding completion celebration | P1 |
| FR-008.8 | Re-trigger onboarding from settings | P2 |

### FR-009: Communication & Churn Prevention

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-009.1 | Welcome email after registration | P0 |
| FR-009.2 | Inactivity reminder emails (3, 7, 14 days) | P1 |
| FR-009.3 | Weekly task summary email (opt-in) | P2 |
| FR-009.4 | Achievement/milestone notifications | P2 |
| FR-009.5 | In-app notification center | P1 |

### FR-010: Product Analytics

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-010.1 | Registration funnel tracking | P0 |
| FR-010.2 | Onboarding completion rates | P0 |
| FR-010.3 | Feature adoption tracking | P1 |
| FR-010.4 | Task completion rates and velocity | P1 |
| FR-010.5 | User retention cohort analysis | P1 |
| FR-010.6 | Session duration and frequency metrics | P2 |
| FR-010.7 | Conversion funnel from signup to active user | P0 |

---

## 3. Non-Functional Requirements

### NFR-001: Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001.1 | API response time (p95) | < 200ms |
| NFR-001.2 | Frontend First Contentful Paint | < 1.5s |
| NFR-001.3 | Frontend Time to Interactive | < 3.0s |
| NFR-001.4 | Lighthouse Performance score | > 90 |
| NFR-001.5 | API throughput | > 1000 req/s |

### NFR-002: Responsive Design

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-002.1 | Mobile viewport (320px - 768px) | Full functionality |
| NFR-002.2 | Tablet viewport (768px - 1024px) | Full functionality |
| NFR-002.3 | Desktop viewport (1024px+) | Full functionality |
| NFR-002.4 | Touch-friendly tap targets | >= 44px |
| NFR-002.5 | Kanban horizontal scroll on mobile | Native gesture support |

### NFR-003: Progressive Web App

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-003.1 | Installable on mobile/desktop | PWA manifest |
| NFR-003.2 | Offline task viewing | Service worker cache |
| NFR-003.3 | Background sync for offline changes | Workbox |
| NFR-003.4 | Push notification support | Web Push API |

### NFR-004: API Documentation

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-004.1 | OpenAPI 3.1 specification | Auto-generated |
| NFR-004.2 | Scalar API reference UI | /scalar endpoint |
| NFR-004.3 | Interactive request testing | Built-in |
| NFR-004.4 | Authentication flow documentation | Included |

### NFR-005: Observability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-005.1 | Structured logging (backend) | Serilog + OTLP |
| NFR-005.2 | Distributed tracing | OpenTelemetry |
| NFR-005.3 | Metrics collection | Prometheus-compatible |
| NFR-005.4 | Frontend error tracking | OpenTelemetry browser |
| NFR-005.5 | Aspire Dashboard integration | Built-in |
| NFR-005.6 | Health check endpoints | /health, /ready |

### NFR-006: CI/CD

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-006.1 | Automated build on push | GitHub Actions |
| NFR-006.2 | Automated test suite execution | All test types |
| NFR-006.3 | Docker image building | Multi-stage builds |
| NFR-006.4 | Deployment to staging on PR merge | Automated |
| NFR-006.5 | Production deployment on release tag | Manual trigger |

### NFR-007: UI/UX

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-007.1 | Light and dark theme | System preference detection |
| NFR-007.2 | Consistent design system | Shadcn/ui + Radix |
| NFR-007.3 | WCAG 2.1 AA accessibility | Minimum standard |
| NFR-007.4 | Smooth animations and transitions | 60fps |
| NFR-007.5 | Loading states and skeletons | All async operations |
| NFR-007.6 | Error states with recovery actions | All failure points |

### NFR-008: Internationalization

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-008.1 | Frontend i18n with react-i18next | All user-facing strings |
| NFR-008.2 | Backend i18n for API messages | All error/validation messages |
| NFR-008.3 | Initial languages: English, Portuguese, Spanish | MVP |
| NFR-008.4 | RTL layout support | Infrastructure ready |
| NFR-008.5 | Date/number/currency localization | Locale-aware |

### NFR-009: Containerization & Deployment

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-009.1 | Docker multi-stage builds | API + Frontend |
| NFR-009.2 | Docker Compose for local dev | Full stack |
| NFR-009.3 | Terraform Azure configuration | Container Apps |
| NFR-009.4 | Aspire orchestration | Local + cloud |
| NFR-009.5 | Environment-based configuration | Dev, Staging, Prod |

### NFR-010: Security

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-010.1 | OWASP Top 10 compliance | All categories |
| NFR-010.2 | HTTPS everywhere | Enforced |
| NFR-010.3 | CORS properly configured | Origin whitelist |
| NFR-010.4 | Rate limiting | Per-endpoint |
| NFR-010.5 | Input validation | All endpoints |
| NFR-010.6 | SQL injection prevention | Parameterized queries |
| NFR-010.7 | XSS prevention | Content Security Policy |
| NFR-010.8 | CSRF protection | Anti-forgery tokens |

---

## 4. Technical Architecture Overview

### 4.1 Backend Stack

- **.NET 10 LTS** with ASP.NET Core 10
- **.NET Aspire 13** for orchestration and observability
- **Entity Framework Core 10** with SQLite
- **ASP.NET Core Identity** for authentication
- **Scalar** for API documentation
- **OpenTelemetry** for instrumentation
- **Serilog** for structured logging
- **FsCheck** for property-based testing
- **xUnit** for unit/integration testing

### 4.2 Frontend Stack

- **Vite 7** for build tooling
- **React 19** with TypeScript
- **Tailwind CSS** for styling
- **Shadcn/ui** with Radix primitives
- **react-i18next** for internationalization
- **vite-plugin-pwa** for PWA capabilities
- **Vitest** for unit testing
- **Playwright** for E2E testing

### 4.3 Infrastructure

- **Docker** for containerization
- **GitHub Actions** for CI/CD
- **Terraform** for Azure IaC
- **Azure Container Apps** for hosting

### 4.4 Architecture Pattern

Domain-Driven Design (DDD) with:
- Use cases (application layer)
- Rich domain entities and value objects
- Repository pattern
- CQRS-light (commands and queries separated)
- Event-driven audit trail

---

## 5. Success Metrics

| Metric | Target | Measurement |
|--------|--------|-------------|
| Registration completion rate | > 80% | Analytics funnel |
| Onboarding completion rate | > 60% | Analytics funnel |
| Day-1 retention | > 40% | Cohort analysis |
| Day-7 retention | > 25% | Cohort analysis |
| Task completion rate | > 50% of created tasks | Feature analytics |
| Lighthouse score | > 90 all categories | Automated CI check |
| Test coverage (backend domain) | > 90% | CI coverage report |
| Test coverage (frontend) | > 80% | CI coverage report |
| API p95 latency | < 200ms | APM monitoring |
| Zero critical security findings | 0 | Security scans |

---

## 6. Out of Scope (MVP)

- Real-time collaboration (multi-user editing)
- File attachments on tasks
- Calendar view
- Recurring tasks
- Time tracking
- Third-party integrations (Slack, Jira, etc.)
- Native mobile apps (iOS/Android)
- Team workspaces with shared boards
- Billing and subscription management

---

## 7. Risks and Mitigations

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| HIPAA compliance complexity | High | Medium | Consult compliance checklist, encrypt all PII at rest |
| SQLite scaling limits | Medium | Low | Abstract repository, easy to swap to PostgreSQL |
| Aspire maturity gaps | Medium | Low | Fallback to standard Docker Compose |
| OAuth provider changes | Low | Low | Abstract behind provider interface |
| Scope creep | High | High | Strict MVP scope, defer to "Out of Scope" |
