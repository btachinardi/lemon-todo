namespace LemonDo.Application.Tests.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

[TestClass]
public sealed class MoveTaskCommandHandlerTests
{
    private ITaskItemRepository _repository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private MoveTaskCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<ITaskItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new MoveTaskCommandHandler(_repository, _unitOfWork);
    }

    [TestMethod]
    public async Task Should_MoveTask_When_ValidCommand()
    {
        var task = TaskItem.Create(UserId.Default, TaskTitle.Create("Test").Value, null, Priority.None).Value;
        var columnId = Guid.NewGuid();
        _repository.GetByIdAsync(Arg.Any<TaskItemId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new MoveTaskCommand(task.Id.Value, columnId, 2));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(columnId, task.ColumnId!.Value);
        Assert.AreEqual(2, task.Position);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<TaskItemId>(), Arg.Any<CancellationToken>())
            .Returns((TaskItem?)null);

        var result = await _handler.HandleAsync(new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public async Task Should_Fail_When_NegativePosition()
    {
        var task = TaskItem.Create(UserId.Default, TaskTitle.Create("Test").Value, null, Priority.None).Value;
        _repository.GetByIdAsync(Arg.Any<TaskItemId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new MoveTaskCommand(task.Id.Value, Guid.NewGuid(), -1));

        Assert.IsTrue(result.IsFailure);
    }
}
