namespace LemonDo.Application.Tests.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

[TestClass]
public sealed class CompleteTaskCommandHandlerTests
{
    private ITaskItemRepository _repository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CompleteTaskCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<ITaskItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CompleteTaskCommandHandler(_repository, _unitOfWork);
    }

    [TestMethod]
    public async Task Should_CompleteTask_When_TaskExists()
    {
        var task = TaskItem.Create(UserId.Default, TaskTitle.Create("Test").Value, null, Priority.None).Value;
        _repository.GetByIdAsync(Arg.Any<TaskItemId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(task.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskItemStatus.Done, task.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TaskItemId>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(Guid.NewGuid()));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_Fail_When_AlreadyCompleted()
    {
        var task = TaskItem.Create(UserId.Default, TaskTitle.Create("Test").Value, null, Priority.None).Value;
        task.Complete();
        _repository.GetByIdAsync(Arg.Any<TaskItemId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(task.Id.Value));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("task.already_completed", result.Error.Code);
    }
}
