namespace LemonDo.Domain.Tests.Tasks.Entities;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Events;
using LemonDo.Domain.Tasks.ValueObjects;

[TestClass]
public sealed class BoardTaskTests
{
    private static TaskTitle ValidTitle => TaskTitle.Create("Buy groceries").Value;
    private static TaskDescription ValidDescription => TaskDescription.Create("Milk, eggs, bread").Value;
    private static Tag ValidTag => Tag.Create("shopping").Value;
    private static UserId DefaultOwner => UserId.Default;
    private static ColumnId DefaultColumnId => ColumnId.New();

    private static BoardTask CreateValidTask(
        Priority priority = Priority.None,
        DateTimeOffset? dueDate = null,
        IEnumerable<Tag>? tags = null,
        ColumnId? columnId = null,
        BoardTaskStatus initialStatus = BoardTaskStatus.Todo)
    {
        var colId = columnId ?? DefaultColumnId;
        var result = BoardTask.Create(DefaultOwner, colId, 0, initialStatus, ValidTitle, ValidDescription, priority, dueDate, tags);
        Assert.IsTrue(result.IsSuccess);
        return result.Value;
    }

    // --- Creation ---

    [TestMethod]
    public void Should_CreateTask_When_ValidInputs()
    {
        var columnId = ColumnId.New();
        var task = CreateValidTask(Priority.High, columnId: columnId);

        Assert.AreEqual("Buy groceries", task.Title.Value);
        Assert.AreEqual("Milk, eggs, bread", task.Description!.Value);
        Assert.AreEqual(Priority.High, task.Priority);
        Assert.AreEqual(BoardTaskStatus.Todo, task.Status);
        Assert.AreEqual(DefaultOwner, task.OwnerId);
        Assert.AreEqual(columnId, task.ColumnId);
        Assert.AreEqual(0, task.Position);
        Assert.IsFalse(task.IsArchived);
        Assert.IsFalse(task.IsDeleted);
        Assert.IsNull(task.CompletedAt);
    }

    [TestMethod]
    public void Should_RaiseTaskCreatedEvent_When_Created()
    {
        var columnId = ColumnId.New();
        var task = CreateValidTask(Priority.Medium, columnId: columnId);

        Assert.HasCount(1, task.DomainEvents);
        var evt = task.DomainEvents[0] as TaskCreatedEvent;
        Assert.IsNotNull(evt);
        Assert.AreEqual(task.Id, evt.BoardTaskId);
        Assert.AreEqual(Priority.Medium, evt.Priority);
        Assert.AreEqual(columnId, evt.ColumnId);
        Assert.AreEqual(0, evt.Position);
        Assert.AreEqual(BoardTaskStatus.Todo, evt.InitialStatus);
    }

    [TestMethod]
    public void Should_CreateTaskWithTags_When_TagsProvided()
    {
        var tags = new[] { ValidTag, Tag.Create("food").Value };
        var task = CreateValidTask(tags: tags);

        Assert.HasCount(2, task.Tags);
    }

    [TestMethod]
    public void Should_FailCreate_When_NegativePosition()
    {
        var result = BoardTask.Create(DefaultOwner, DefaultColumnId, -1, BoardTaskStatus.Todo, ValidTitle);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("position.validation", result.Error.Code);
    }

    // --- UpdateTitle ---

    [TestMethod]
    public void Should_UpdateTitle_When_Valid()
    {
        var task = CreateValidTask();
        var newTitle = TaskTitle.Create("Clean house").Value;

        var result = task.UpdateTitle(newTitle);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Clean house", task.Title.Value);
    }

    [TestMethod]
    public void Should_RaiseTaskUpdatedEvent_When_TitleChanged()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();
        var newTitle = TaskTitle.Create("Clean house").Value;

        task.UpdateTitle(newTitle);

        Assert.HasCount(1, task.DomainEvents);
        var evt = task.DomainEvents[0] as TaskUpdatedEvent;
        Assert.IsNotNull(evt);
        Assert.AreEqual("Title", evt.FieldName);
    }

    // --- UpdateDescription ---

    [TestMethod]
    public void Should_UpdateDescription_When_Valid()
    {
        var task = CreateValidTask();
        var newDesc = TaskDescription.Create("New description").Value;

        var result = task.UpdateDescription(newDesc);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("New description", task.Description!.Value);
    }

    // --- SetPriority ---

    [TestMethod]
    public void Should_SetPriority_When_Valid()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();

        var result = task.SetPriority(Priority.Critical);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(Priority.Critical, task.Priority);

        var evt = task.DomainEvents[0] as TaskPriorityChangedEvent;
        Assert.IsNotNull(evt);
        Assert.AreEqual(Priority.None, evt.OldPriority);
        Assert.AreEqual(Priority.Critical, evt.NewPriority);
    }

    // --- SetDueDate ---

    [TestMethod]
    public void Should_SetDueDate_When_Valid()
    {
        var task = CreateValidTask();
        var dueDate = DateTimeOffset.UtcNow.AddDays(7);

        var result = task.SetDueDate(dueDate);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(dueDate, task.DueDate);
    }

    [TestMethod]
    public void Should_ClearDueDate_When_SetToNull()
    {
        var task = CreateValidTask(dueDate: DateTimeOffset.UtcNow.AddDays(7));

        var result = task.SetDueDate(null);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(task.DueDate);
    }

    // --- Tags ---

    [TestMethod]
    public void Should_AddTag_When_Valid()
    {
        var task = CreateValidTask();

        var result = task.AddTag(ValidTag);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, task.Tags);
        Assert.AreEqual("shopping", task.Tags[0].Value);
    }

    [TestMethod]
    public void Should_FailAddTag_When_Duplicate()
    {
        var task = CreateValidTask();
        task.AddTag(ValidTag);

        var result = task.AddTag(Tag.Create("shopping").Value);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("task.duplicate_tag", result.Error.Code);
    }

    [TestMethod]
    public void Should_RemoveTag_When_Exists()
    {
        var task = CreateValidTask();
        task.AddTag(ValidTag);

        var result = task.RemoveTag(Tag.Create("shopping").Value);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(task.Tags);
    }

    [TestMethod]
    public void Should_FailRemoveTag_When_NotExists()
    {
        var task = CreateValidTask();

        var result = task.RemoveTag(ValidTag);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("task.tag_not_found", result.Error.Code);
    }

    // --- MoveTo (column-status invariant) ---

    [TestMethod]
    public void Should_SetStatusFromColumn_When_MovedToDoneColumn()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();
        var doneColumnId = ColumnId.New();

        var result = task.MoveTo(doneColumnId, 0, BoardTaskStatus.Done);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(BoardTaskStatus.Done, task.Status);
        Assert.AreEqual(doneColumnId, task.ColumnId);
        Assert.AreEqual(0, task.Position);
    }

    [TestMethod]
    public void Should_SetCompletedAt_When_MovedToDone()
    {
        var task = CreateValidTask();

        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Done);

        Assert.IsNotNull(task.CompletedAt);
    }

    [TestMethod]
    public void Should_ClearCompletedAt_When_MovedFromDoneToTodo()
    {
        var task = CreateValidTask();
        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Done);
        Assert.IsNotNull(task.CompletedAt);

        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Todo);

        Assert.IsNull(task.CompletedAt);
        Assert.AreEqual(BoardTaskStatus.Todo, task.Status);
    }

    [TestMethod]
    public void Should_ClearIsArchived_When_MovedFromDoneToNonDone()
    {
        var task = CreateValidTask();
        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Done);
        task.Archive();
        Assert.IsTrue(task.IsArchived);

        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Todo);

        Assert.IsFalse(task.IsArchived);
    }

    [TestMethod]
    public void Should_RaiseMovedEvent_When_Moved()
    {
        var fromColumnId = ColumnId.New();
        var task = CreateValidTask(columnId: fromColumnId);
        task.ClearDomainEvents();
        var toColumnId = ColumnId.New();

        task.MoveTo(toColumnId, 3, BoardTaskStatus.InProgress);

        var movedEvt = task.DomainEvents.OfType<TaskMovedEvent>().Single();
        Assert.AreEqual(fromColumnId, movedEvt.FromColumnId);
        Assert.AreEqual(toColumnId, movedEvt.ToColumnId);
        Assert.AreEqual(3, movedEvt.NewPosition);
    }

    [TestMethod]
    public void Should_RaiseStatusChangedEvent_When_StatusChanges()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();

        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Done);

        var statusEvt = task.DomainEvents.OfType<TaskStatusChangedEvent>().Single();
        Assert.AreEqual(BoardTaskStatus.Todo, statusEvt.OldStatus);
        Assert.AreEqual(BoardTaskStatus.Done, statusEvt.NewStatus);
    }

    [TestMethod]
    public void Should_NotRaiseStatusChangedEvent_When_StatusUnchanged()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();

        task.MoveTo(ColumnId.New(), 5, BoardTaskStatus.Todo);

        Assert.IsFalse(task.DomainEvents.OfType<TaskStatusChangedEvent>().Any());
        Assert.IsTrue(task.DomainEvents.OfType<TaskMovedEvent>().Any());
    }

    [TestMethod]
    public void Should_FailMove_When_NegativePosition()
    {
        var task = CreateValidTask();

        var result = task.MoveTo(ColumnId.New(), -1, BoardTaskStatus.Todo);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("position.validation", result.Error.Code);
    }

    // --- Archive / Unarchive ---

    [TestMethod]
    public void Should_Archive_When_Done()
    {
        var task = CreateValidTask();
        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Done);

        var result = task.Archive();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(task.IsArchived);
        Assert.AreEqual(BoardTaskStatus.Done, task.Status); // Status stays Done
    }

    [TestMethod]
    public void Should_FailArchive_When_NotDone()
    {
        var task = CreateValidTask();

        var result = task.Archive();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("task.not_completed", result.Error.Code);
    }

    [TestMethod]
    public void Should_Unarchive_When_Archived()
    {
        var task = CreateValidTask();
        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Done);
        task.Archive();

        var result = task.Unarchive();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(task.IsArchived);
        Assert.AreEqual(BoardTaskStatus.Done, task.Status); // Status stays Done
    }

    // --- Delete ---

    [TestMethod]
    public void Should_SoftDelete_When_NotDeleted()
    {
        var task = CreateValidTask();

        var result = task.Delete();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(task.IsDeleted);
    }

    [TestMethod]
    public void Should_FailDelete_When_AlreadyDeleted()
    {
        var task = CreateValidTask();
        task.Delete();

        var result = task.Delete();

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("task.already_deleted", result.Error.Code);
    }

    [TestMethod]
    public void Should_FailEdit_When_Deleted()
    {
        var task = CreateValidTask();
        task.Delete();

        var titleResult = task.UpdateTitle(TaskTitle.Create("New").Value);
        var priorityResult = task.SetPriority(Priority.High);
        var tagResult = task.AddTag(ValidTag);
        var moveResult = task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Todo);

        Assert.IsTrue(titleResult.IsFailure);
        Assert.IsTrue(priorityResult.IsFailure);
        Assert.IsTrue(tagResult.IsFailure);
        Assert.IsTrue(moveResult.IsFailure);

        Assert.AreEqual("task.deleted", titleResult.Error.Code);
    }

    // --- OwnerId immutability ---

    [TestMethod]
    public void Should_PreserveOwnerId_AfterMutations()
    {
        var task = CreateValidTask();
        var originalOwner = task.OwnerId;

        task.UpdateTitle(TaskTitle.Create("Changed").Value);
        task.SetPriority(Priority.Critical);
        task.MoveTo(ColumnId.New(), 0, BoardTaskStatus.Done);

        Assert.AreEqual(originalOwner, task.OwnerId);
    }
}
