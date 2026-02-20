# Shared Kernel

> **Source**: Extracted from docs/DOMAIN.md §9
> **Status**: Active
> **Last Updated**: 2026-02-18

---

Types shared across all bounded contexts:

```
UserId              -> Guid wrapper (shared identity)
DateTimeOffset      -> All timestamps in UTC
Result<T, E>        -> Discriminated union for operation results
PagedResult<T>      -> { Items, TotalCount, Page, PageSize }
DomainEvent         -> Base class for all domain events
Entity<TId>         -> Base class with Id, CreatedAt, UpdatedAt
ValueObject         -> Base class with structural equality
```
