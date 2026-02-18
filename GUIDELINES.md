# Development Guidelines

> Detailed guidelines have moved to `docs/architecture/`.
> See [docs/architecture/INDEX.md](./docs/architecture/INDEX.md) for the full reference.

---

## Quick Reference

### Testing

- **TDD always**: Write failing test first, then minimum code to pass, then refactor.
- **Coverage targets**: Domain 90%, Use cases 80%, API endpoints 100%, Frontend 80%.
- See [docs/architecture/testing.md](./docs/architecture/testing.md) for the full pyramid.

### Backend Layer Order

```
Domain → Application → Infrastructure → API
```

Domain cannot import from any other layer. See [docs/architecture/backend.md](./docs/architecture/backend.md).

### Frontend Tier Order

```
Routing → Pages/Layouts → State Management → Components
```

State flows down via props only. See [docs/architecture/frontend.md](./docs/architecture/frontend.md).

### State Ownership

| State Type | Tool |
|------------|------|
| Server data | TanStack Query |
| Client/UI state | Zustand |
| Low-frequency global | React Context |

### Commit Convention

```
<type>(<scope>): <description>
```

Types: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`, `ci`, `perf`

### Security Essentials

- Never log tokens or PII unredacted
- Auth: HttpOnly cookie refresh + memory-only access token
- PII: redacted display, hash lookup, AES-256-GCM encrypted source of truth
- See [docs/architecture/security.md](./docs/architecture/security.md) for full detail.
