namespace LemonDo.Domain.Tests.Tasks.Entities;

using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Events;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;
using TaskStatus = LemonDo.Domain.Tasks.ValueObjects.TaskStatus;

[TestClass]
public sealed class TaskTests
{
    private static TaskTitle ValidTitle => TaskTitle.Create("Buy groceries").Value;
    private static TaskDescription ValidDescription => TaskDescription.Create("Milk, eggs, bread").Value;
    private static Tag ValidTag => Tag.Create("shopping").Value;
    private static UserId DefaultOwner => UserId.Default;

    private static TaskEntity CreateValidTask(
        Priority priority = Priority.None,
        DateTimeOffset? dueDate = null,
        IEnumerable<Tag>? tags = null)
    {
        var result = TaskEntity.Create(DefaultOwner, ValidTitle, ValidDescription, priority, dueDate, tags);
        Assert.IsTrue(result.IsSuccess);
        return result.Value;
    }

    // --- Creation ---

    [TestMethod]
    public void Should_CreateTask_When_ValidInputs()
    {
        var task = CreateValidTask(Priority.High);

        Assert.AreEqual("Buy groceries", task.Title.Value);
        Assert.AreEqual("Milk, eggs, bread", task.Description!.Value);
        Assert.AreEqual(Priority.High, task.Priority);
        Assert.AreEqual(TaskStatus.Todo, task.Status);
        Assert.AreEqual(DefaultOwner, task.OwnerId);
        Assert.IsFalse(task.IsArchived);
        Assert.IsFalse(task.IsDeleted);
        Assert.IsNull(task.CompletedAt);
    }

    [TestMethod]
    public void Should_DefaultToTodo_When_Created()
    {
        var task = CreateValidTask();

        Assert.AreEqual(TaskStatus.Todo, task.Status);
    }

    [TestMethod]
    public void Should_RaiseTaskCreatedEvent_When_Created()
    {
        var task = CreateValidTask(Priority.Medium);

        Assert.HasCount(1, task.DomainEvents);
        var evt = task.DomainEvents[0] as TaskCreatedEvent;
        Assert.IsNotNull(evt);
        Assert.AreEqual(task.Id, evt.TaskId);
        Assert.AreEqual(Priority.Medium, evt.Priority);
    }

    [TestMethod]
    public void Should_CreateTaskWithTags_When_TagsProvided()
    {
        var tags = new[] { ValidTag, Tag.Create("food").Value };
        var task = CreateValidTask(tags: tags);

        Assert.HasCount(2, task.Tags);
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

    // --- SetStatus / Complete / Uncomplete ---

    [TestMethod]
    public void Should_SetStatus_When_ValidTransition()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();

        var result = task.SetStatus(TaskStatus.InProgress);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.InProgress, task.Status);
    }

    [TestMethod]
    public void Should_SetStatusToDone_When_Complete()
    {
        var task = CreateValidTask();

        var result = task.Complete();

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.Done, task.Status);
        Assert.IsNotNull(task.CompletedAt);
    }

    [TestMethod]
    public void Should_SetStatusToTodo_When_Uncomplete()
    {
        var task = CreateValidTask();
        task.Complete();

        var result = task.Uncomplete();

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.Todo, task.Status);
        Assert.IsNull(task.CompletedAt);
    }

    [TestMethod]
    public void Should_SetCompletedAt_When_SetStatusToDone()
    {
        var task = CreateValidTask();

        task.SetStatus(TaskStatus.Done);

        Assert.IsNotNull(task.CompletedAt);
    }

    [TestMethod]
    public void Should_ClearCompletedAt_When_SetStatusFromDone()
    {
        var task = CreateValidTask();
        task.SetStatus(TaskStatus.Done);
        Assert.IsNotNull(task.CompletedAt);

        task.SetStatus(TaskStatus.Todo);

        Assert.IsNull(task.CompletedAt);
        Assert.AreEqual(TaskStatus.Todo, task.Status);
    }

    [TestMethod]
    public void Should_PreserveIsArchived_When_SetStatusChanges()
    {
        var task = CreateValidTask();
        task.Archive();
        Assert.IsTrue(task.IsArchived);

        task.SetStatus(TaskStatus.InProgress);

        Assert.IsTrue(task.IsArchived); // Archive state is independent of lifecycle
    }

    [TestMethod]
    public void Should_NoOp_When_SetStatusToSameStatus()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();

        var result = task.SetStatus(TaskStatus.Todo);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsEmpty(task.DomainEvents);
    }

    [TestMethod]
    public void Should_RaiseStatusChangedEvent_When_StatusChanges()
    {
        var task = CreateValidTask();
        task.ClearDomainEvents();

        task.SetStatus(TaskStatus.Done);

        var statusEvt = task.DomainEvents.OfType<TaskStatusChangedEvent>().Single();
        Assert.AreEqual(TaskStatus.Todo, statusEvt.OldStatus);
        Assert.AreEqual(TaskStatus.Done, statusEvt.NewStatus);
    }

    [TestMethod]
    public void Should_FailSetStatus_When_Deleted()
    {
        var task = CreateValidTask();
        task.Delete();

        var result = task.SetStatus(TaskStatus.Done);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("task.deleted", result.Error.Code);
    }

    // --- Archive / Unarchive ---

    [TestMethod]
    public void Should_Archive_When_Done()
    {
        var task = CreateValidTask();
        task.SetStatus(TaskStatus.Done);

        var result = task.Archive();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(task.IsArchived);
        Assert.AreEqual(TaskStatus.Done, task.Status);
    }

    [TestMethod]
    public void Should_Archive_When_Todo()
    {
        var task = CreateValidTask();

        var result = task.Archive();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(task.IsArchived);
        Assert.AreEqual(TaskStatus.Todo, task.Status); // Status unchanged
    }

    [TestMethod]
    public void Should_Archive_When_InProgress()
    {
        var task = CreateValidTask();
        task.SetStatus(TaskStatus.InProgress);

        var result = task.Archive();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(task.IsArchived);
        Assert.AreEqual(TaskStatus.InProgress, task.Status); // Status unchanged
    }

    [TestMethod]
    public void Should_Unarchive_When_Archived()
    {
        var task = CreateValidTask();
        task.Archive();

        var result = task.Unarchive();

        Assert.IsTrue(result.IsSuccess);
        Assert.IsFalse(task.IsArchived);
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
        var statusResult = task.SetStatus(TaskStatus.Done);

        Assert.IsTrue(titleResult.IsFailure);
        Assert.IsTrue(priorityResult.IsFailure);
        Assert.IsTrue(tagResult.IsFailure);
        Assert.IsTrue(statusResult.IsFailure);

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
        task.SetStatus(TaskStatus.Done);

        Assert.AreEqual(originalOwner, task.OwnerId);
    }

    // --- Sensitive Note ---

    [TestMethod]
    public void Should_CreateWithSensitiveNote_When_Provided()
    {
        var note = SensitiveNote.Create("Secret content").Value;
        var result = TaskEntity.Create(DefaultOwner, ValidTitle, sensitiveNote: note);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("[PROTECTED]", result.Value.RedactedSensitiveNote);
    }

    [TestMethod]
    public void Should_CreateWithoutSensitiveNote_When_NotProvided()
    {
        var result = TaskEntity.Create(DefaultOwner, ValidTitle);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(result.Value.RedactedSensitiveNote);
    }

    [TestMethod]
    public void Should_UpdateSensitiveNote_When_Valid()
    {
        var task = CreateValidTask();
        Assert.IsNull(task.RedactedSensitiveNote);

        var note = SensitiveNote.Create("New secret").Value;
        var result = task.UpdateSensitiveNote(note);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("[PROTECTED]", task.RedactedSensitiveNote);
    }

    [TestMethod]
    public void Should_ClearSensitiveNote_When_NullPassed()
    {
        var note = SensitiveNote.Create("Secret").Value;
        var task = TaskEntity.Create(DefaultOwner, ValidTitle, sensitiveNote: note).Value;
        Assert.AreEqual("[PROTECTED]", task.RedactedSensitiveNote);

        var result = task.UpdateSensitiveNote(null);

        Assert.IsTrue(result.IsSuccess);
        Assert.IsNull(task.RedactedSensitiveNote);
    }

    [TestMethod]
    public void Should_FailUpdateSensitiveNote_When_TaskDeleted()
    {
        var task = CreateValidTask();
        task.Delete();

        var note = SensitiveNote.Create("Secret").Value;
        var result = task.UpdateSensitiveNote(note);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("task.deleted", result.Error.Code);
    }
}
