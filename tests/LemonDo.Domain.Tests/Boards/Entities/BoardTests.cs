namespace LemonDo.Domain.Tests.Boards.Entities;

using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Events;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskStatus = LemonDo.Domain.Tasks.ValueObjects.TaskStatus;

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

        Assert.AreEqual(TaskStatus.Todo, board.Columns[0].TargetStatus);
        Assert.AreEqual(TaskStatus.InProgress, board.Columns[1].TargetStatus);
        Assert.AreEqual(TaskStatus.Done, board.Columns[2].TargetStatus);
    }

    // --- GetInitialColumn / GetDoneColumn / FindColumnById ---

    [TestMethod]
    public void Should_ReturnFirstTodoColumn_When_GetInitialColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var initial = board.GetInitialColumn();

        Assert.AreEqual("To Do", initial.Name.Value);
        Assert.AreEqual(TaskStatus.Todo, initial.TargetStatus);
    }

    [TestMethod]
    public void Should_ReturnFirstDoneColumn_When_GetDoneColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var done = board.GetDoneColumn();

        Assert.AreEqual("Done", done.Name.Value);
        Assert.AreEqual(TaskStatus.Done, done.TargetStatus);
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

        var result = board.AddColumn(colName, TaskStatus.InProgress);

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

        var result = board.AddColumn(colName, TaskStatus.InProgress, 1);

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

        board.AddColumn(colName, TaskStatus.InProgress);

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

        var result = board.AddColumn(colName, TaskStatus.Todo);

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

    // --- Card Management: PlaceTask ---

    [TestMethod]
    public void Should_PlaceTask_When_ValidColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        var columnId = board.Columns[0].Id;

        var result = board.PlaceTask(taskId, columnId);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.Todo, result.Value);
        Assert.IsNotNull(board.FindCardByTaskId(taskId));
    }

    [TestMethod]
    public void Should_AssignRankFromColumnNextRank_When_TaskPlaced()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        var columnId = board.Columns[0].Id;

        board.PlaceTask(taskId, columnId);

        var card = board.FindCardByTaskId(taskId);
        Assert.IsNotNull(card);
        Assert.AreEqual(1000m, card.Rank);
    }

    [TestMethod]
    public void Should_IncrementColumnNextRank_When_TaskPlaced()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;

        board.PlaceTask(TaskId.New(), columnId);

        var column = board.FindColumnById(columnId)!;
        Assert.AreEqual(2000m, column.NextRank);
    }

    [TestMethod]
    public void Should_AssignMonotonicallyIncreasingRanks_When_MultiplePlaced()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        var taskId1 = TaskId.New();
        var taskId2 = TaskId.New();
        var taskId3 = TaskId.New();

        board.PlaceTask(taskId1, columnId);
        board.PlaceTask(taskId2, columnId);
        board.PlaceTask(taskId3, columnId);

        var rank1 = board.FindCardByTaskId(taskId1)!.Rank;
        var rank2 = board.FindCardByTaskId(taskId2)!.Rank;
        var rank3 = board.FindCardByTaskId(taskId3)!.Rank;

        Assert.IsLessThan(rank2, rank1, $"rank1 ({rank1}) should be less than rank2 ({rank2})");
        Assert.IsLessThan(rank3, rank2, $"rank2 ({rank2}) should be less than rank3 ({rank3})");
    }

    [TestMethod]
    public void Should_FailPlaceTask_When_InvalidColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var result = board.PlaceTask(TaskId.New(), ColumnId.New());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.column_not_found", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailPlaceTask_When_TaskAlreadyPlaced()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        var columnId = board.Columns[0].Id;
        board.PlaceTask(taskId, columnId);

        var result = board.PlaceTask(taskId, columnId);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.task_already_placed", result.Error.Code);
    }

    [TestMethod]
    public void Should_RaiseCardPlacedEvent_When_TaskPlaced()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        board.ClearDomainEvents();
        var taskId = TaskId.New();
        var columnId = board.Columns[0].Id;

        board.PlaceTask(taskId, columnId);

        var evt = board.DomainEvents.OfType<CardPlacedEvent>().SingleOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(board.Id, evt.BoardId);
        Assert.AreEqual(taskId, evt.TaskId);
        Assert.AreEqual(columnId, evt.ColumnId);
        Assert.AreEqual(1000m, evt.Rank);
    }

    // --- Card Management: MoveCard (neighbor-based rank) ---

    [TestMethod]
    public void Should_MoveCardToEmptyColumn_When_BothNeighborsNull()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        var todoColumnId = board.Columns[0].Id;
        var doneColumnId = board.Columns[2].Id;
        board.PlaceTask(taskId, todoColumnId);

        // Move to empty Done column — no neighbors
        var result = board.MoveCard(taskId, doneColumnId, previousTaskId: null, nextTaskId: null);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.Done, result.Value);
        var card = board.FindCardByTaskId(taskId);
        Assert.IsNotNull(card);
        Assert.AreEqual(doneColumnId, card.ColumnId);
        Assert.IsGreaterThan(0, card.Rank, "Card in empty column should have a positive rank");
    }

    [TestMethod]
    public void Should_MoveCardBetweenTwoCards_When_BothNeighborsProvided()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        var taskA = TaskId.New();
        var taskB = TaskId.New();
        var taskC = TaskId.New();
        board.PlaceTask(taskA, columnId);
        board.PlaceTask(taskB, columnId);
        board.PlaceTask(taskC, columnId);
        var rankA = board.FindCardByTaskId(taskA)!.Rank;
        var rankB = board.FindCardByTaskId(taskB)!.Rank;

        // Move C between A and B (same column)
        var result = board.MoveCard(taskC, columnId, previousTaskId: taskA, nextTaskId: taskB);

        Assert.IsTrue(result.IsSuccess);
        var movedRank = board.FindCardByTaskId(taskC)!.Rank;
        Assert.IsGreaterThan(rankA, movedRank, $"Rank ({movedRank}) should be > rankA ({rankA})");
        Assert.IsLessThan(rankB, movedRank, $"Rank ({movedRank}) should be < rankB ({rankB})");
    }

    [TestMethod]
    public void Should_MoveCardToTopOfColumn_When_PreviousIsNull()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        var taskA = TaskId.New();
        var taskB = TaskId.New();
        board.PlaceTask(taskA, columnId);
        board.PlaceTask(taskB, columnId);
        var rankA = board.FindCardByTaskId(taskA)!.Rank;

        // Move B to top — before A
        var result = board.MoveCard(taskB, columnId, previousTaskId: null, nextTaskId: taskA);

        Assert.IsTrue(result.IsSuccess);
        var movedRank = board.FindCardByTaskId(taskB)!.Rank;
        Assert.IsLessThan(rankA, movedRank, $"Rank ({movedRank}) should be < rankA ({rankA})");
        Assert.IsGreaterThan(0, movedRank, "Rank at top should still be positive");
    }

    [TestMethod]
    public void Should_MoveCardToBottomOfColumn_When_NextIsNull()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        var taskA = TaskId.New();
        var taskB = TaskId.New();
        var taskC = TaskId.New();
        board.PlaceTask(taskA, columnId);
        board.PlaceTask(taskB, columnId);
        board.PlaceTask(taskC, columnId);
        var rankC = board.FindCardByTaskId(taskC)!.Rank;

        // Move A to bottom — after C
        var result = board.MoveCard(taskA, columnId, previousTaskId: taskC, nextTaskId: null);

        Assert.IsTrue(result.IsSuccess);
        var movedRank = board.FindCardByTaskId(taskA)!.Rank;
        Assert.IsGreaterThan(rankC, movedRank, $"Rank ({movedRank}) should be > rankC ({rankC})");
    }

    [TestMethod]
    public void Should_BumpColumnNextRank_When_MoveProducesRankExceedingIt()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var todoColumnId = board.Columns[0].Id;
        var doneColumnId = board.Columns[2].Id;
        var taskA = TaskId.New();
        board.PlaceTask(taskA, todoColumnId);

        // Done column NextRank starts at 1000. Move A to bottom of Done (empty)
        board.MoveCard(taskA, doneColumnId, previousTaskId: null, nextTaskId: null);

        var doneColumn = board.FindColumnById(doneColumnId)!;
        var movedRank = board.FindCardByTaskId(taskA)!.Rank;
        Assert.IsGreaterThan(movedRank, doneColumn.NextRank,
            $"Column NextRank ({doneColumn.NextRank}) should be > moved card rank ({movedRank})");
    }

    [TestMethod]
    public void Should_ProduceUniqueRanks_When_MultipleMovesInSameColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        var taskA = TaskId.New();
        var taskB = TaskId.New();
        var taskC = TaskId.New();
        board.PlaceTask(taskA, columnId);
        board.PlaceTask(taskB, columnId);
        board.PlaceTask(taskC, columnId);

        // Move C between A and B
        board.MoveCard(taskC, columnId, previousTaskId: taskA, nextTaskId: taskB);
        // Move B to top
        board.MoveCard(taskB, columnId, previousTaskId: null, nextTaskId: taskC);

        var rankA = board.FindCardByTaskId(taskA)!.Rank;
        var rankB = board.FindCardByTaskId(taskB)!.Rank;
        var rankC = board.FindCardByTaskId(taskC)!.Rank;

        // All three ranks must be distinct
        Assert.AreNotEqual(rankA, rankB, "A and B should have different ranks");
        Assert.AreNotEqual(rankB, rankC, "B and C should have different ranks");
        Assert.AreNotEqual(rankA, rankC, "A and C should have different ranks");

        // Order should be B, A, C:
        // After move 1 (C between A,B): A(1000), C(1500), B(2000)
        // After move 2 (B before C):    B(750),  A(1000), C(1500)
        Assert.IsLessThan(rankA, rankB, $"B ({rankB}) should be before A ({rankA})");
        Assert.IsLessThan(rankC, rankA, $"A ({rankA}) should be before C ({rankC})");
    }

    // --- Cross-Column MoveCard (non-empty target) ---

    [TestMethod]
    public void Should_MoveCardBetweenTwoCards_When_CrossColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var todoColumnId = board.Columns[0].Id;
        var inProgressColumnId = board.Columns[1].Id;

        // Place A in Todo, P and Q in InProgress
        var taskA = TaskId.New();
        var taskP = TaskId.New();
        var taskQ = TaskId.New();
        board.PlaceTask(taskA, todoColumnId);
        board.PlaceTask(taskP, inProgressColumnId);
        board.PlaceTask(taskQ, inProgressColumnId);

        var rankP = board.FindCardByTaskId(taskP)!.Rank;
        var rankQ = board.FindCardByTaskId(taskQ)!.Rank;

        // Move A from Todo to between P and Q in InProgress
        var result = board.MoveCard(taskA, inProgressColumnId, previousTaskId: taskP, nextTaskId: taskQ);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.InProgress, result.Value);
        var movedCard = board.FindCardByTaskId(taskA)!;
        Assert.AreEqual(inProgressColumnId, movedCard.ColumnId);
        Assert.IsGreaterThan(rankP, movedCard.Rank, $"Rank ({movedCard.Rank}) should be > rankP ({rankP})");
        Assert.IsLessThan(rankQ, movedCard.Rank, $"Rank ({movedCard.Rank}) should be < rankQ ({rankQ})");
    }

    [TestMethod]
    public void Should_MoveCardToTopOfNonEmptyColumn_When_CrossColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var todoColumnId = board.Columns[0].Id;
        var inProgressColumnId = board.Columns[1].Id;

        var taskA = TaskId.New();
        var taskP = TaskId.New();
        board.PlaceTask(taskA, todoColumnId);
        board.PlaceTask(taskP, inProgressColumnId);

        var rankP = board.FindCardByTaskId(taskP)!.Rank;

        // Move A to top of InProgress (before P)
        var result = board.MoveCard(taskA, inProgressColumnId, previousTaskId: null, nextTaskId: taskP);

        Assert.IsTrue(result.IsSuccess);
        var movedCard = board.FindCardByTaskId(taskA)!;
        Assert.AreEqual(inProgressColumnId, movedCard.ColumnId);
        Assert.IsLessThan(rankP, movedCard.Rank, $"Rank ({movedCard.Rank}) should be < rankP ({rankP})");
        Assert.IsGreaterThan(0, movedCard.Rank, "Rank at top should still be positive");
    }

    [TestMethod]
    public void Should_MoveCardToBottomOfNonEmptyColumn_When_CrossColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var todoColumnId = board.Columns[0].Id;
        var inProgressColumnId = board.Columns[1].Id;

        var taskA = TaskId.New();
        var taskP = TaskId.New();
        var taskQ = TaskId.New();
        board.PlaceTask(taskA, todoColumnId);
        board.PlaceTask(taskP, inProgressColumnId);
        board.PlaceTask(taskQ, inProgressColumnId);

        var rankQ = board.FindCardByTaskId(taskQ)!.Rank;

        // Move A to bottom of InProgress (after Q)
        var result = board.MoveCard(taskA, inProgressColumnId, previousTaskId: taskQ, nextTaskId: null);

        Assert.IsTrue(result.IsSuccess);
        var movedCard = board.FindCardByTaskId(taskA)!;
        Assert.AreEqual(inProgressColumnId, movedCard.ColumnId);
        Assert.IsGreaterThan(rankQ, movedCard.Rank, $"Rank ({movedCard.Rank}) should be > rankQ ({rankQ})");
    }

    [TestMethod]
    public void Should_FailMoveCard_When_CardNotFound()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;

        var result = board.MoveCard(TaskId.New(), columnId, previousTaskId: null, nextTaskId: null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.card_not_found", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailMoveCard_When_InvalidColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        board.PlaceTask(taskId, board.Columns[0].Id);

        var result = board.MoveCard(taskId, ColumnId.New(), previousTaskId: null, nextTaskId: null);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.column_not_found", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailMoveCard_When_PreviousTaskNotOnBoard()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        var columnId = board.Columns[0].Id;
        board.PlaceTask(taskId, columnId);

        var result = board.MoveCard(taskId, columnId, previousTaskId: TaskId.New(), nextTaskId: null);

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_FailMoveCard_When_NextTaskNotOnBoard()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        var columnId = board.Columns[0].Id;
        board.PlaceTask(taskId, columnId);

        var result = board.MoveCard(taskId, columnId, previousTaskId: null, nextTaskId: TaskId.New());

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public void Should_RaiseCardMovedEvent_When_CardMoved()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        var fromColumnId = board.Columns[0].Id;
        var toColumnId = board.Columns[1].Id;
        board.PlaceTask(taskId, fromColumnId);
        board.ClearDomainEvents();

        board.MoveCard(taskId, toColumnId, previousTaskId: null, nextTaskId: null);

        var evt = board.DomainEvents.OfType<CardMovedEvent>().SingleOrDefault();
        Assert.IsNotNull(evt);
        Assert.AreEqual(board.Id, evt.BoardId);
        Assert.AreEqual(taskId, evt.TaskId);
        Assert.AreEqual(fromColumnId, evt.FromColumnId);
        Assert.AreEqual(toColumnId, evt.ToColumnId);
        Assert.IsGreaterThan(0, evt.NewRank, "Event should carry the computed rank");
    }

    // --- Card Management: RemoveCard ---

    [TestMethod]
    public void Should_RemoveCard_When_Exists()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var taskId = TaskId.New();
        board.PlaceTask(taskId, board.Columns[0].Id);

        var result = board.RemoveCard(taskId);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(board.FindCardByTaskId(taskId));
    }

    [TestMethod]
    public void Should_FailRemoveCard_When_NotFound()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;

        var result = board.RemoveCard(TaskId.New());

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.card_not_found", result.Error.Code);
    }

    // --- Card Management: GetCardCountInColumn ---

    [TestMethod]
    public void Should_GetCardCountInColumn()
    {
        var board = Board.CreateDefault(DefaultOwner).Value;
        var columnId = board.Columns[0].Id;
        board.PlaceTask(TaskId.New(), columnId);
        board.PlaceTask(TaskId.New(), columnId);
        board.PlaceTask(TaskId.New(), board.Columns[1].Id);

        var count = board.GetCardCountInColumn(columnId);

        Assert.AreEqual(2, count);
    }
}
