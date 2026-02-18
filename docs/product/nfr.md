# Non-Functional Requirements

> **Source**: Extracted from docs/PRD.draft.md §3, docs/PRD.md §2, docs/PRD.2.draft.md §9
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## NFR-001: Performance

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-001.1 | API response time (p95) | < 200ms |
| NFR-001.2 | Frontend First Contentful Paint | < 1.5s |
| NFR-001.3 | Frontend Time to Interactive | < 3.0s |
| NFR-001.4 | Lighthouse Performance score | > 90 |
| NFR-001.5 | API throughput | > 1000 req/s |

---

## NFR-002: Responsive Design

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-002.1 | Mobile viewport (320px - 768px) | Full functionality |
| NFR-002.2 | Tablet viewport (768px - 1024px) | Full functionality |
| NFR-002.3 | Desktop viewport (1024px+) | Full functionality |
| NFR-002.4 | Touch-friendly tap targets | >= 44px |
| NFR-002.5 | Kanban horizontal scroll on mobile | Native gesture support |
| NFR-002.6 | Quick-add accessible via floating action button on mobile | Always visible |
| NFR-002.7 | Kanban columns scroll horizontally on mobile with snap | Native gesture |

---

## NFR-003: Progressive Web App

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-003.1 | Installable on mobile/desktop | PWA manifest |
| NFR-003.2 | Offline task viewing AND creation AND completion | Full offline CRUD |
| NFR-003.3 | Background sync for offline changes | Workbox |
| NFR-003.4 | Push notification support | Web Push API |
| NFR-003.5 | Offline change indicator on affected tasks | Visual sync status |
| NFR-003.6 | Automatic sync on reconnection with conflict resolution | Last-write-wins |

**Rationale**: Scenario S06 (airplane) shows offline must support full task lifecycle, not just viewing.

---

## NFR-004: API Documentation

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-004.1 | OpenAPI 3.1 specification | Auto-generated |
| NFR-004.2 | Scalar API reference UI | /scalar endpoint |
| NFR-004.3 | Interactive request testing | Built-in |
| NFR-004.4 | Authentication flow documentation | Included |

---

## NFR-005: Observability

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-005.1 | Structured logging (backend) | Serilog + OTLP |
| NFR-005.2 | Distributed tracing | OpenTelemetry |
| NFR-005.3 | Metrics collection | Prometheus-compatible |
| NFR-005.4 | Frontend error tracking | OpenTelemetry browser |
| NFR-005.5 | Aspire Dashboard integration | Built-in |
| NFR-005.6 | Health check endpoints | /health, /ready |

---

## NFR-006: CI/CD

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-006.1 | Automated build on push | GitHub Actions |
| NFR-006.2 | Automated test suite execution | All test types |
| NFR-006.3 | Docker image building | Multi-stage builds |
| NFR-006.4 | Deployment to staging on PR merge | Automated |
| NFR-006.5 | Production deployment on release tag | Manual trigger |

---

## NFR-007: UI/UX

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-007.1 | Light and dark theme | System preference detection |
| NFR-007.2 | Consistent design system | Shadcn/ui + Radix |
| NFR-007.3 | WCAG 2.1 AA accessibility | Minimum standard |
| NFR-007.4 | Smooth animations and transitions | 60fps |
| NFR-007.5 | Loading states and skeletons | All async operations |
| NFR-007.6 | Error states with recovery actions | All failure points |

---

## NFR-008: Internationalization

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-008.1 | Frontend i18n with react-i18next | All user-facing strings |
| NFR-008.2 | Backend i18n for API messages | All error/validation messages |
| NFR-008.3 | Initial languages: English, Portuguese, Spanish | MVP |
| NFR-008.4 | RTL layout support | Infrastructure ready |
| NFR-008.5 | Date/number/currency localization | Locale-aware |

---

## NFR-009: Containerization & Deployment

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-009.1 | Docker multi-stage builds | API + Frontend |
| NFR-009.2 | Docker Compose for local dev | Full stack |
| NFR-009.3 | Terraform Azure configuration | Container Apps |
| NFR-009.4 | Aspire orchestration | Local + cloud |
| NFR-009.5 | Environment-based configuration | Dev, Staging, Prod |

---

## NFR-010: Security

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

## NFR-011: Micro-Interactions & UX Polish

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-011.1 | Task creation animation (slide-in) | < 300ms |
| NFR-011.2 | Task completion animation (strikethrough + fade) | < 500ms |
| NFR-011.3 | Drag-and-drop with ghost element and drop shadow | Real-time |
| NFR-011.4 | Theme switch transition (no white flash) | Instant |
| NFR-011.5 | Loading skeletons for all async content | Immediate |
| NFR-011.6 | Empty states with helpful illustrations/CTAs | All empty views |
| NFR-011.7 | Toast notifications for async operations | Non-blocking |

**Rationale**: Multiple scenarios emphasize that UX polish (animations, celebrations, feedback) is core to the product, not decoration.

---

## v2 Non-Functional Requirements

> **Status**: Draft (v2)

| ID | Requirement | Target |
|----|-------------|--------|
| NFR-V2-001 | Single-user mode (Bruno only) | No multi-tenancy needed |
| NFR-V2-002 | Local-first: all project data stored locally | Git repos stay on disk |
| NFR-V2-003 | API for agent integration | RESTful, authenticated |
| NFR-V2-004 | Real-time agent session output streaming | WebSocket or SSE |
| NFR-V2-005 | Communication channel adapters must be pluggable | Adapter pattern |
| NFR-V2-006 | Budget tracking accurate to sub-dollar granularity | For agent cost control |
| NFR-V2-007 | Graceful degradation when external services are unavailable | Offline-capable for local features |
| NFR-V2-008 | All v1 features and tests continue to pass | Non-breaking evolution |
