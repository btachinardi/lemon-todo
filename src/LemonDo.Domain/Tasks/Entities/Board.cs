namespace LemonDo.Domain.Tasks.Entities;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Events;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed class Board : Entity<BoardId>
{
    private readonly List<Column> _columns = [];

    public UserId OwnerId { get; }
    public BoardName Name { get; private set; }
    public IReadOnlyList<Column> Columns => _columns.AsReadOnly();

    private Board(BoardId id, UserId ownerId, BoardName name) : base(id)
    {
        OwnerId = ownerId;
        Name = name;
    }

    public static Result<Board, DomainError> CreateDefault(UserId ownerId)
    {
        var nameResult = BoardName.Create("My Board");
        if (nameResult.IsFailure)
            return Result<Board, DomainError>.Failure(nameResult.Error);

        var board = new Board(BoardId.New(), ownerId, nameResult.Value);

        board._columns.Add(Column.Create(ColumnName.Create("To Do").Value, 0));
        board._columns.Add(Column.Create(ColumnName.Create("In Progress").Value, 1));
        board._columns.Add(Column.Create(ColumnName.Create("Done").Value, 2));

        board.RaiseDomainEvent(new BoardCreatedEvent(board.Id, ownerId));
        return Result<Board, DomainError>.Success(board);
    }

    public static Result<Board, DomainError> Create(UserId ownerId, BoardName name)
    {
        var board = new Board(BoardId.New(), ownerId, name);
        board._columns.Add(Column.Create(ColumnName.Create("To Do").Value, 0));
        board.RaiseDomainEvent(new BoardCreatedEvent(board.Id, ownerId));
        return Result<Board, DomainError>.Success(board);
    }

    public Result<DomainError> AddColumn(ColumnName name, int? position = null)
    {
        if (_columns.Any(c => c.Name.Value.Equals(name.Value, StringComparison.OrdinalIgnoreCase)))
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("board.duplicate_column_name", "A column with this name already exists."));

        var targetPosition = position ?? _columns.Count;

        var column = Column.Create(name, targetPosition);

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

    public Result<DomainError> RemoveColumn(ColumnId columnId)
    {
        var column = _columns.Find(c => c.Id == columnId);
        if (column is null)
            return Result<DomainError>.Failure(
                new DomainError("board.column_not_found", "Column not found."));

        if (_columns.Count <= 1)
            return Result<DomainError>.Failure(
                DomainError.BusinessRule("board.cannot_remove_last_column", "Cannot remove the last column."));

        _columns.Remove(column);
        ReindexPositions();

        RaiseDomainEvent(new ColumnRemovedEvent(Id, columnId));
        return Result<DomainError>.Success();
    }

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
