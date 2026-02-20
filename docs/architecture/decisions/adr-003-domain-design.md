# ADR-003: Domain Design & API Contracts

> **Source**: Extracted from docs/architecture/decisions/trade-offs.md §Domain Design, §Card Ordering & API Design
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## Domain Design

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Column-Status relationship** | Column determines status (one source of truth) | Independent status enum and column position | Two sources of truth always desync eventually; making column authoritative eliminated an entire class of bugs |
| **ColumnRole enum** | Direct `TargetStatus: TaskStatus` on Column | Separate ColumnRole enum | ColumnRole was a 1:1 mapping to TaskStatus; direct usage is clearer than an indirection layer |
| **Archive semantics** | `IsArchived` bool flag, orthogonal to status | `Archived` as a TaskStatus enum value | Visibility flags (archive, soft-delete) are orthogonal to entity lifecycle; mixing them into the status enum creates invalid state combinations |
| **Archive guard** | Any task can be archived regardless of status | Only Done tasks can be archived | Archiving is organizational (visibility), not lifecycle; a stale Todo or abandoned InProgress should be archivable |
| **Task lifecycle methods** | `SetStatus()` + `Complete()`/`Uncomplete()` convenience methods | `MoveTo()` as single source of truth | After bounded context split, Task owns its own status directly; Board owns spatial placement separately |

---

## Card Ordering & API Design

| Trade-off | Chosen approach | Alternative forgone | Why |
|---|---|---|---|
| **Ordering strategy** | Sparse decimal ranks (1000, 2000, 3000; midpoint inserts) | Dense integers, LexoRank strings, linked list pointers, CRDT | Simplest strategy that eliminates the position-collision bug class; only updates one row per move; decimal avoids float precision drift; LexoRank adds unnecessary complexity at this scale |
| **Move API contract** | Neighbor card IDs (`previousTaskId`/`nextTaskId`) | Frontend sends array index or rank directly | Intent-based ("place between these two cards") survives backend strategy changes; frontend stays dumb, backend avoids read-to-sort, API contract is unambiguous |
| **Orphan cleanup** | Delete removes card; Archive preserves card on board | Symmetric handling (both remove or both preserve) | Asymmetric by intent: deletion is destructive with no undelete, so card is removed; archive is reversible, so card stays for rank restoration on unarchive |
| **Orphan filtering** | Board query handlers filter out archived/deleted task cards at read time | Eager cleanup on every archive/delete | Preserves archived card placement in the database while presenting clean data to the frontend; read-layer filtering is cheaper than write-layer coordination |
| **E2E test isolation** | Unique user per describe block (timestamp + counter email) | Shared user + `deleteAllTasks()` cleanup between tests | Fresh users = true data isolation with zero cleanup overhead; each describe block operates on an empty board; eliminates shared auth state and token rotation conflicts |
| **E2E test execution** | `test.describe.serial` with shared page/context | Parallel execution with per-test browser context | Tests accumulate state like real users; login once in `beforeAll` instead of per test; 3x faster (20s vs 60-90s), 100% stable |
