namespace LemonDo.Domain.Boards.Entities;

using LemonDo.Domain.Boards.Events;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Aggregate root for the Board bounded context (downstream conformist to Task context).
/// Owns spatial placement of tasks via <see cref="TaskCard"/> and column structure.
/// </summary>
/// <remarks>
/// <para>Each column has a <see cref="Column.TargetStatus"/> that determines which
/// <see cref="Tasks.ValueObjects.TaskStatus"/> a task receives when placed or moved into it.
/// The application layer coordinates this: <see cref="PlaceTask"/> and <see cref="MoveCard"/>
/// return the target status so the caller can sync the Task aggregate.</para>
/// <para>Invariants: a board must always have at least one Todo column and one Done column.</para>
/// </remarks>
public sealed class Board : Entity<BoardId>
{
    private readonly List<Column> _columns = [];
    private readonly List<TaskCard> _cards = [];

    /// <summary>The user who owns this board. Immutable after creation.</summary>
    public UserId OwnerId { get; }

    /// <summary>Display name for this board. Can be changed via board update commands.</summary>
    public BoardName Name { get; private set; }

    /// <summary>The columns on this board, ordered by <see cref="Column.Position"/>. Minimum 1 column required.</summary>
    public IReadOnlyList<Column> Columns => _columns.AsReadOnly();

    /// <summary>Task cards currently placed on this board, each mapped to a column with a rank for ordering.</summary>
    public IReadOnlyList<TaskCard> Cards => _cards.AsReadOnly();

    private Board(BoardId id, UserId ownerId, BoardName name) : base(id)
    {
        OwnerId = ownerId;
        Name = name;
    }

    /// <summary>
    /// Creates a default board named "My Board" with three columns: To Do, In Progress, and Done.
    /// </summary>
    public static Result<Board, DomainError> CreateDefault(UserId ownerId)
    {
        var nameResult = BoardName.Create("My Board");
        if (nameResult.IsFailure)
            return Result<Board, DomainError>.Failure(nameResult.Error);

        var board = new Board(BoardId.New(), ownerId, nameResult.Value);

        board._columns.Add(Column.Create(ColumnName.Create("To Do").Value, 0, TaskStatus.Todo));
        board._columns.Add(Column.Create(ColumnName.Create("In Progress").Value, 1, TaskStatus.InProgress));
        board._columns.Add(Column.Create(ColumnName.Create("Done").Value, 2, TaskStatus.Done));

        board.RaiseDomainEvent(new BoardCreatedEvent(board.Id, ownerId));
        return Result<Board, DomainError>.Success(board);
    }

    /// <summary>
    /// Creates a board with a custom name and two default columns: To Do and Done.
    /// </summary>
    public static Result<Board, DomainError> Create(UserId ownerId, BoardName name)
    {
        var board = new Board(BoardId.New(), ownerId, name);
        board._columns.Add(Column.Create(ColumnName.Create("To Do").Value, 0, TaskStatus.Todo));
        board._columns.Add(Column.Create(ColumnName.Create("Done").Value, 1, TaskStatus.Done));
        board.RaiseDomainEvent(new BoardCreatedEvent(board.Id, ownerId));
        return Result<Board, DomainError>.Success(board);
    }

    /// <summary>Returns the first Todo-targeting column (by position). Used to place new tasks.</summary>
    public Column GetInitialColumn()
    {
        return _columns.Where(c => c.TargetStatus == TaskStatus.Todo).OrderBy(c => c.Position).First();
    }

    /// <summary>Returns the first Done-targeting column (by position). Used to place completed tasks.</summary>
    public Column GetDoneColumn()
    {
        return _columns.Where(c => c.TargetStatus == TaskStatus.Done).OrderBy(c => c.Position).First();
    }

    /// <summary>Finds a column by its ID, or <c>null</c> if not found.</summary>
    public Column? FindColumnById(ColumnId columnId)
    {
        return _columns.Find(c => c.Id == columnId);
    }

    /// <summary>Adds a new column to the board. Fails if a column with the same name already exists.</summary>
    public Result<DomainError> AddColumn(ColumnName name, TaskStatus targetStatus, int? position = null)
    {
        if (_columns.Any(c => c.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)))
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("board.duplicate_column_name", "A column with this name already exists."));

        var targetPosition = position ?? _columns.Count;

        var column = Column.Create(name, targetPosition, targetStatus);

        if (targetPosition < _columns.Count)
        {
            _columns.Insert(targetPosition, column);
            ReindexPositions();
        }
        else
        {
            _columns.Add(column);
        }

        RaiseDomainEvent(new ColumnAddedEvent(Id, column.Id, name.Value));
        return Result<DomainError>.Success();
    }

    /// <summary>
    /// Removes a column from the board. Cannot remove the last column, nor the last column
    /// targeting <see cref="TaskStatus.Todo"/> or <see cref="TaskStatus.Done"/>.
    /// </summary>
    public Result<DomainError> RemoveColumn(ColumnId columnId)
    {
        var column = _columns.Find(c => c.Id == columnId);
        if (column is null)
            return Result<DomainError>.Failure(
                new DomainError("board.column_not_found", "Column not found."));

        if (_columns.Count <= 1)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("board.cannot_remove_last_column", "Cannot remove the last column."));

        // Cannot remove the last column targeting Todo or Done status
        if (column.TargetStatus is TaskStatus.Todo or TaskStatus.Done)
        {
            var sameStatusCount = _columns.Count(c => c.TargetStatus == column.TargetStatus);
            if (sameStatusCount <= 1)
                return Result<DomainError>.Failure(
                    DomainError.BusinessRule("board.cannot_remove_last_status_column",
                        $"Cannot remove the last {column.TargetStatus} column. Board must have at least one."));
        }

        _columns.Remove(column);
        ReindexPositions();

        RaiseDomainEvent(new ColumnRemovedEvent(Id, columnId));
        return Result<DomainError>.Success();
    }

    /// <summary>Moves a column to a new position, re-indexing all sibling positions.</summary>
    public Result<DomainError> ReorderColumn(ColumnId columnId, int newPosition)
    {
        if (newPosition < 0 || newPosition >= _columns.Count)
            return Result<DomainError>.Failure(
                new DomainError("position.validation", "Position is out of range."));

        var column = _columns.Find(c => c.Id == columnId);
        if (column is null)
            return Result<DomainError>.Failure(
                new DomainError("board.column_not_found", "Column not found."));

        var oldPosition = column.Position;

        _columns.Remove(column);
        _columns.Insert(newPosition, column);
        ReindexPositions();

        RaiseDomainEvent(new ColumnReorderedEvent(Id, columnId, oldPosition, newPosition));
        return Result<DomainError>.Success();
    }

    /// <summary>Renames a column. Fails if the new name duplicates another column on this board.</summary>
    public Result<DomainError> RenameColumn(ColumnId columnId, ColumnName newName)
    {
        var column = _columns.Find(c => c.Id == columnId);
        if (column is null)
            return Result<DomainError>.Failure(
                new DomainError("board.column_not_found", "Column not found."));

        var isDuplicate = _columns.Any(c =>
            c.Id != columnId &&
            c.Name.Value.Equals(newName.Value, StringComparison.OrdinalIgnoreCase));

        if (isDuplicate)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("board.duplicate_column_name", "A column with this name already exists."));

        column.Rename(newName);

        RaiseDomainEvent(new ColumnRenamedEvent(Id, columnId, newName.Value));
        return Result<DomainError>.Success();
    }

    // --- Card Management ---

    /// <summary>
    /// Places a task card onto this board at the end of the specified column.
    /// Rank is auto-assigned from the column's <see cref="Column.NextRank"/> counter.
    /// </summary>
    /// <returns>The column's <see cref="Column.TargetStatus"/> so the caller can sync the Task aggregate's status.</returns>
    public Result<TaskStatus, DomainError> PlaceTask(TaskId taskId, ColumnId columnId)
    {
        var column = _columns.Find(c => c.Id == columnId);
        if (column is null)
            return Result<TaskStatus, DomainError>.Failure(
                new DomainError("board.column_not_found", "Column not found."));

        if (_cards.Any(c => c.TaskId == taskId))
            return Result<TaskStatus, DomainError>.Failure(
                DomainError.BusinessRule("board.task_already_placed", "Task is already on this board."));

        var rank = column.NextRank;
        column.NextRank += 1000m;

        _cards.Add(new TaskCard(taskId, columnId, rank));
        RaiseDomainEvent(new CardPlacedEvent(Id, taskId, columnId, rank));
        return Result<TaskStatus, DomainError>.Success(column.TargetStatus);
    }

    /// <summary>
    /// Moves an existing card to a different column and/or position using neighbor-based ranking.
    /// The new rank is computed from the ranks of the previous and next cards at the drop target.
    /// </summary>
    /// <returns>The target column's <see cref="Column.TargetStatus"/> so the caller can sync the Task aggregate's status.</returns>
    public Result<TaskStatus, DomainError> MoveCard(
        TaskId taskId,
        ColumnId toColumnId,
        TaskId? previousTaskId,
        TaskId? nextTaskId)
    {
        var column = _columns.Find(c => c.Id == toColumnId);
        if (column is null)
            return Result<TaskStatus, DomainError>.Failure(
                new DomainError("board.column_not_found", "Column not found."));

        var card = _cards.Find(c => c.TaskId == taskId);
        if (card is null)
            return Result<TaskStatus, DomainError>.Failure(
                new DomainError("board.card_not_found", "Task is not on this board."));

        // Validate neighbor references
        if (previousTaskId is not null && _cards.Find(c => c.TaskId == previousTaskId) is null)
            return Result<TaskStatus, DomainError>.Failure(
                new DomainError("board.card_not_found", "Previous task is not on this board."));

        if (nextTaskId is not null && _cards.Find(c => c.TaskId == nextTaskId) is null)
            return Result<TaskStatus, DomainError>.Failure(
                new DomainError("board.card_not_found", "Next task is not on this board."));

        // Compute rank from neighbors
        var previousRank = previousTaskId is not null
            ? _cards.Find(c => c.TaskId == previousTaskId)!.Rank
            : (decimal?)null;

        var nextRank = nextTaskId is not null
            ? _cards.Find(c => c.TaskId == nextTaskId)!.Rank
            : (decimal?)null;

        decimal newRank;
        if (previousRank is null && nextRank is null)
        {
            // Empty column or end of column — use column's NextRank
            newRank = column.NextRank;
            column.NextRank += 1000m;
        }
        else if (previousRank is null)
        {
            // Top of column — half of the first card's rank
            newRank = nextRank!.Value / 2m;
        }
        else if (nextRank is null)
        {
            // Bottom of column — previous + 1000
            newRank = previousRank.Value + 1000m;
            if (newRank >= column.NextRank)
                column.NextRank = newRank + 1000m;
        }
        else
        {
            // Between two cards — midpoint
            newRank = (previousRank.Value + nextRank.Value) / 2m;
        }

        var fromColumnId = card.ColumnId;
        _cards.Remove(card);
        _cards.Add(new TaskCard(taskId, toColumnId, newRank));

        RaiseDomainEvent(new CardMovedEvent(Id, taskId, fromColumnId, toColumnId, newRank));
        return Result<TaskStatus, DomainError>.Success(column.TargetStatus);
    }

    /// <summary>Removes a task card from the board. Fails if the task has no card on this board.</summary>
    public Result<DomainError> RemoveCard(TaskId taskId)
    {
        var card = _cards.Find(c => c.TaskId == taskId);
        if (card is null)
            return Result<DomainError>.Failure(
                new DomainError("board.card_not_found", "Task is not on this board."));

        _cards.Remove(card);
        RaiseDomainEvent(new CardRemovedEvent(Id, taskId));
        return Result<DomainError>.Success();
    }

    /// <summary>Finds a card by its task ID, or <c>null</c> if the task has no card on this board.</summary>
    public TaskCard? FindCardByTaskId(TaskId taskId)
    {
        return _cards.Find(c => c.TaskId == taskId);
    }

    /// <summary>Returns the number of cards currently placed in the specified column.</summary>
    public int GetCardCountInColumn(ColumnId columnId)
    {
        return _cards.Count(c => c.ColumnId == columnId);
    }

    private void ReindexPositions()
    {
        for (var i = 0; i < _columns.Count; i++)
        {
            _columns[i].Position = i;
        }
    }

    // EF Core constructor
    private Board() : base(default!) { OwnerId = default!; Name = default!; }
}
