# Board Context

> **Source**: Extracted from docs/DOMAIN.md §4
> **Status**: Active
> **Last Updated**: 2026-02-18

---

## 4.1 Design Principles

1. **Board owns spatial placement, not task lifecycle** — Boards manage where tasks appear on the kanban (which column, what position). Task status is owned by the Task context.
2. **Board is conformist to Task** — Board imports `TaskId` and `TaskStatus` from the Task context. The Task context knows nothing about boards.
3. **Status changes coordinated at application layer** — When a card moves to a new column, the application handler syncs the task's status to the column's `TargetStatus`. The Board's `MoveCard()` and `PlaceTask()` methods return the target status so the handler can call `task.SetStatus()`.
4. **TaskCard is a value object on Board** — The `TaskCard` represents a task's placement on the board (column + position). It references `TaskId` but does not contain task data.

## 4.2 Entities

### Board (Aggregate Root)

```
Board
├── Id: BoardId (value object)
├── OwnerId: UserId
├── Name: BoardName (value object)
├── Columns: IReadOnlyList<Column> (ordered by position)
├── Cards: IReadOnlyList<TaskCard> (task placements on this board)
├── CreatedAt: DateTimeOffset
│
├── Methods:
│   ├── CreateDefault(ownerId) -> BoardCreatedEvent (3 columns: To Do, In Progress, Done)
│   ├── Create(ownerId, name) -> BoardCreatedEvent (2 columns: To Do, Done)
│   ├── GetInitialColumn() -> Column (first Todo column by position)
│   ├── GetDoneColumn() -> Column (first Done column by position)
│   ├── FindColumnById(columnId) -> Column? (lookup for validation)
│   ├── PlaceTask(taskId, columnId) -> TaskStatus (returns column's TargetStatus)
│   │       + CardPlacedEvent (rank auto-assigned from Column.NextRank)
│   ├── MoveCard(taskId, toColumnId, previousTaskId?, nextTaskId?) -> TaskStatus
│   │       + CardMovedEvent (rank computed from neighbor ranks)
│   ├── RemoveCard(taskId) -> CardRemovedEvent
│   ├── FindCardByTaskId(taskId) -> TaskCard?
│   ├── GetCardCountInColumn(columnId) -> int
│   ├── AddColumn(name, targetStatus, position?) -> ColumnAddedEvent
│   ├── RemoveColumn(columnId) -> ColumnRemovedEvent
│   ├── ReorderColumn(columnId, newPosition) -> ColumnReorderedEvent
│   └── RenameColumn(columnId, name) -> ColumnRenamedEvent
│
└── Invariants:
    ├── Must have at least one column with TargetStatus = Todo
    ├── Must have at least one column with TargetStatus = Done
    ├── Default board has 3 columns: To Do (Todo), In Progress (InProgress), Done (Done)
    ├── Custom board starts with at least To Do + Done columns
    ├── Column names must be unique within a board (case-insensitive)
    ├── Cannot remove the last column targeting a required status (Todo or Done)
    ├── A task can only have one card per board (no duplicate TaskIds)
    └── Cannot move a card to a column that doesn't exist on this board
```

### Column (Entity, owned by Board)

```
Column
├── Id: ColumnId (value object)
├── Name: ColumnName (value object)
├── TargetStatus: TaskStatus (the status tasks get when placed here)
├── Position: int
├── MaxTasks: int? (null = unlimited)
├── NextRank: decimal (monotonic counter, starts at 1000, incremented by 1000 per placement)
│
└── Invariants:
    ├── Name must be 1-50 characters
    ├── Position must be >= 0
    ├── MaxTasks must be > 0 if set
    └── TargetStatus is immutable after creation
```

### TaskCard (Value Object, owned by Board)

```
TaskCard
├── TaskId: TaskId (references Task context)
├── ColumnId: ColumnId (references Column on this board)
├── Rank: decimal (sparse sort key within column, e.g. 1000, 2000, 1500)
│
└── Invariants:
    ├── TaskId must be non-empty
    ├── ColumnId must reference a column on the owning board
    └── Rank must be > 0
```

## 4.3 Value Objects

```
BoardId         -> Guid wrapper
ColumnId        -> Guid wrapper
BoardName       -> Non-empty string, 1-100 chars
ColumnName      -> Non-empty string, 1-50 chars
TaskCard        -> TaskId + ColumnId + Rank (placement of a task on the board)
```

## 4.4 Domain Events

```
BoardCreatedEvent           { BoardId, OwnerId }
ColumnAddedEvent            { BoardId, ColumnId, ColumnName }
ColumnRemovedEvent          { BoardId, ColumnId }
ColumnReorderedEvent        { BoardId, ColumnId, OldPosition, NewPosition }
ColumnRenamedEvent          { BoardId, ColumnId, Name }
CardPlacedEvent             { BoardId, TaskId, ColumnId, Rank }
CardMovedEvent              { BoardId, TaskId, FromColumnId, ToColumnId, NewRank }
CardRemovedEvent            { BoardId, TaskId }
```

## 4.5 Use Cases

```
Commands:
├── MoveCardCommand              { TaskId, ColumnId, PreviousTaskId?, NextTaskId? }
│       → Moves card on board (rank from neighbors), gets target status, calls task.SetStatus(targetStatus)
├── AddColumnCommand             { BoardId, Name, TargetStatus, Position? }
├── RemoveColumnCommand          { BoardId, ColumnId }
├── ReorderColumnCommand         { BoardId, ColumnId, NewPosition }
└── RenameColumnCommand          { BoardId, ColumnId, Name }

Queries:
├── GetDefaultBoardQuery         {} -> BoardDto (auto-creates if missing, includes Cards)
└── GetBoardByIdQuery            { BoardId } -> BoardDto (includes Cards)
```

## 4.6 Repository Interface

```csharp
public interface IBoardRepository
{
    Task<Board?> GetByIdAsync(BoardId id, CancellationToken ct);
    Task<Board?> GetDefaultForUserAsync(UserId ownerId, CancellationToken ct);
    Task AddAsync(Board board, CancellationToken ct);
    Task UpdateAsync(Board board, CancellationToken ct);
}
```

## 4.7 Application Layer Coordination

The Task and Board contexts are coordinated at the application layer (command handlers). The following cross-context workflows exist:

| Operation | Task Context | Board Context |
|-----------|-------------|---------------|
| **Create Task** | `Task.Create()` (status = Todo) | `board.PlaceTask(taskId, initialColumn)` (rank auto-assigned) |
| **Move Card** | `task.SetStatus(targetStatus)` | `board.MoveCard(taskId, toColumnId, previousTaskId?, nextTaskId?)` returns `targetStatus` |
| **Complete Task** | `task.Complete()` | `board.MoveCard(taskId, doneColumn, null, null)` |
| **Uncomplete Task** | `task.Uncomplete()` | `board.MoveCard(taskId, todoColumn, null, null)` |
| **Delete Task** | `task.Delete()` | `board.RemoveCard(taskId)` |
