namespace LemonDo.Domain.Tests.Tasks.Entities;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Events;
using LemonDo.Domain.Tasks.ValueObjects;

[TestClass]
public sealed class BoardTests
{
    private static UserId DefaultOwner => UserId.Default;

    // --- CreateDefault ---

    [TestMethod]
    public void Should_CreateDefaultBoard_With3Columns()
    {
        var result = Board.CreateDefault(DefaultOwner);

        Assert.IsTrue(result.IsSuccess);
        var board = result.Value;
        Assert.AreEqual("My Board", board.Name.Value);
        Assert.HasCount(3, board.Columns);
        Assert.AreEqual("To Do", board.Columns[0].Name.Value);
        Assert.AreEqual("In Progress", board.Columns[1].Name.Value);
        Assert.AreEqual("Done", board.Columns[2].Name.Value);
    }

    [TestMethod]
    public void Should_AssignSequentialPositions_When_DefaultCreated()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        Assert.AreEqual(0, board.Columns[0].Position);
        Assert.AreEqual(1, board.Columns[1].Position);
        Assert.AreEqual(2, board.Columns[2].Position);
    }

    [TestMethod]
    public void Should_RaiseBoardCreatedEvent_When_Created()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var evt = board.DomainEvents.OfType<BoardCreatedEvent>().SingleOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(board.Id, evt.BoardId);
        Assert.AreEqual(DefaultOwner, evt.OwnerId);
    }

    [TestMethod]
    public void Should_PreserveOwnerId_AfterCreation()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        Assert.AreEqual(DefaultOwner, board.OwnerId);
    }

    // --- Create (custom) ---

    [TestMethod]
    public void Should_CreateBoard_When_ValidInputs()
    {
        var name = BoardName.Create("Sprint Board").Value;
        var result = Board.Create(DefaultOwner, name);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Sprint Board", result.Value.Name.Value);
        Assert.HasCount(2, result.Value.Columns);
        Assert.AreEqual("To Do", result.Value.Columns[0].Name.Value);
        Assert.AreEqual("Done", result.Value.Columns[1].Name.Value);
    }

    // --- Column TargetStatus ---

    [TestMethod]
    public void Should_AssignTargetStatus_When_DefaultCreated()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        Assert.AreEqual(BoardTaskStatus.Todo, board.Columns[0].TargetStatus);
        Assert.AreEqual(BoardTaskStatus.InProgress, board.Columns[1].TargetStatus);
        Assert.AreEqual(BoardTaskStatus.Done, board.Columns[2].TargetStatus);
    }

    // --- GetInitialColumn / GetDoneColumn / FindColumnById ---

    [TestMethod]
    public void Should_ReturnFirstTodoColumn_When_GetInitialColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var initial = board.GetInitialColumn();

        Assert.AreEqual("To Do", initial.Name.Value);
        Assert.AreEqual(BoardTaskStatus.Todo, initial.TargetStatus);
    }

    [TestMethod]
    public void Should_ReturnFirstDoneColumn_When_GetDoneColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var done = board.GetDoneColumn();

        Assert.AreEqual("Done", done.Name.Value);
        Assert.AreEqual(BoardTaskStatus.Done, done.TargetStatus);
    }

    [TestMethod]
    public void Should_ReturnColumn_When_FindColumnByIdExists()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var expectedColumn = board.Columns[1];

        var found = board.FindColumnById(expectedColumn.Id);

        Assert.IsNotNull(found);
        Assert.AreEqual(expectedColumn.Id, found.Id);
    }

    [TestMethod]
    public void Should_ReturnNull_When_FindColumnByIdNotFound()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var found = board.FindColumnById(ColumnId.New());

        Assert.IsNull(found);
    }

    // --- AddColumn ---

    [TestMethod]
    public void Should_AddColumn_When_ValidName()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var colName = ColumnName.Create("Review").Value;

        var result = board.AddColumn(colName, BoardTaskStatus.InProgress);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(4, board.Columns);
        Assert.AreEqual("Review", board.Columns[3].Name.Value);
        Assert.AreEqual(3, board.Columns[3].Position);
    }

    [TestMethod]
    public void Should_AddColumnAtPosition_When_PositionProvided()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var colName = ColumnName.Create("Review").Value;

        var result = board.AddColumn(colName, BoardTaskStatus.InProgress, 1);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(4, board.Columns);
        Assert.AreEqual("Review", board.Columns[1].Name.Value);
        Assert.AreEqual(1, board.Columns[1].Position);
        Assert.AreEqual(2, board.Columns[2].Position);
        Assert.AreEqual(3, board.Columns[3].Position);
    }

    [TestMethod]
    public void Should_RaiseColumnAddedEvent_When_ColumnAdded()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var colName = ColumnName.Create("Review").Value;

        board.AddColumn(colName, BoardTaskStatus.InProgress);

        var evt = board.DomainEvents.OfType<ColumnAddedEvent>().SingleOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(board.Id, evt.BoardId);
        Assert.AreEqual("Review", evt.ColumnName);
    }

    [TestMethod]
    public void Should_FailAddColumn_When_DuplicateName()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var colName = ColumnName.Create("To Do").Value;

        var result = board.AddColumn(colName, BoardTaskStatus.Todo);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.duplicate_column_name", result.Error.Code);
    }

    // --- RemoveColumn ---

    [TestMethod]
    public void Should_RemoveColumn_When_Exists()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var columnId = board.Columns[1].Id;

        var result = board.RemoveColumn(columnId);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, board.Columns);
        Assert.AreEqual("To Do", board.Columns[0].Name.Value);
        Assert.AreEqual("Done", board.Columns[1].Name.Value);
    }

    [TestMethod]
    public void Should_ReindexPositions_When_ColumnRemoved()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        // Remove InProgress column (not a required status)
        var columnId = board.Columns[1].Id;

        board.RemoveColumn(columnId);

        Assert.AreEqual(0, board.Columns[0].Position);
        Assert.AreEqual(1, board.Columns[1].Position);
    }

    [TestMethod]
    public void Should_RaiseColumnRemovedEvent_When_ColumnRemoved()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var columnId = board.Columns[1].Id;

        board.RemoveColumn(columnId);

        var evt = board.DomainEvents.OfType<ColumnRemovedEvent>().SingleOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(board.Id, evt.BoardId);
        Assert.AreEqual(columnId, evt.ColumnId);
    }

    [TestMethod]
    public void Should_FailRemoveColumn_When_LastTodoColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        // Remove InProgress first (allowed)
        board.RemoveColumn(board.Columns[1].Id);
        // Now only To Do and Done remain — try removing the last Todo column
        var todoColumnId = board.Columns[0].Id;

        var result = board.RemoveColumn(todoColumnId);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.cannot_remove_last_status_column", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailRemoveColumn_When_LastDoneColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        // Remove InProgress first (allowed)
        board.RemoveColumn(board.Columns[1].Id);
        // Now only To Do and Done remain — try removing the last Done column
        var doneColumnId = board.Columns[1].Id;

        var result = board.RemoveColumn(doneColumnId);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.cannot_remove_last_status_column", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailRemoveColumn_When_NotFound()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var result = board.RemoveColumn(ColumnId.New());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.column_not_found", result.Error.Code);
    }

    // --- ReorderColumn ---

    [TestMethod]
    public void Should_ReorderColumn_When_ValidNewPosition()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var columnId = board.Columns[0].Id;

        var result = board.ReorderColumn(columnId, 2);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("In Progress", board.Columns[0].Name.Value);
        Assert.AreEqual("Done", board.Columns[1].Name.Value);
        Assert.AreEqual("To Do", board.Columns[2].Name.Value);
    }

    [TestMethod]
    public void Should_ReindexPositions_When_ColumnReordered()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[2].Id;

        board.ReorderColumn(columnId, 0);

        for (var i = 0; i < board.Columns.Count; i++)
        {
            Assert.AreEqual(i, board.Columns[i].Position);
        }
    }

    [TestMethod]
    public void Should_RaiseColumnReorderedEvent_When_Reordered()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var columnId = board.Columns[0].Id;

        board.ReorderColumn(columnId, 2);

        var evt = board.DomainEvents.OfType<ColumnReorderedEvent>().SingleOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(0, evt.OldPosition);
        Assert.AreEqual(2, evt.NewPosition);
    }

    [TestMethod]
    public void Should_FailReorder_When_ColumnNotFound()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var result = board.ReorderColumn(ColumnId.New(), 0);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.column_not_found", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailReorder_When_NegativePosition()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;

        var result = board.ReorderColumn(columnId, -1);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("position.validation", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailReorder_When_PositionExceedsCount()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;

        var result = board.ReorderColumn(columnId, 5);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("position.validation", result.Error.Code);
    }

    // --- RenameColumn ---

    [TestMethod]
    public void Should_RenameColumn_When_ValidName()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var columnId = board.Columns[0].Id;
        var newName = ColumnName.Create("Backlog").Value;

        var result = board.RenameColumn(columnId, newName);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Backlog", board.Columns[0].Name.Value);
    }

    [TestMethod]
    public void Should_RaiseColumnRenamedEvent_When_Renamed()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var columnId = board.Columns[0].Id;
        var newName = ColumnName.Create("Backlog").Value;

        board.RenameColumn(columnId, newName);

        var evt = board.DomainEvents.OfType<ColumnRenamedEvent>().SingleOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(board.Id, evt.BoardId);
        Assert.AreEqual(columnId, evt.ColumnId);
        Assert.AreEqual("Backlog", evt.Name);
    }

    [TestMethod]
    public void Should_FailRename_When_DuplicateName()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        var newName = ColumnName.Create("Done").Value;

        var result = board.RenameColumn(columnId, newName);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.duplicate_column_name", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailRename_When_ColumnNotFound()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var newName = ColumnName.Create("Backlog").Value;

        var result = board.RenameColumn(ColumnId.New(), newName);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.column_not_found", result.Error.Code);
    }

    [TestMethod]
    public void Should_SucceedRename_When_SameNameAsCurrent()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        var sameName = ColumnName.Create("To Do").Value;

        var result = board.RenameColumn(columnId, sameName);

        Assert.IsTrue(result.IsSuccess);
    }
}
