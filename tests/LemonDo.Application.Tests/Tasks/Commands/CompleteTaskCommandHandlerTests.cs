namespace LemonDo.Application.Tests.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

[TestClass]
public sealed class CompleteTaskCommandHandlerTests
{
    private ITaskRepository _taskRepository = null!;
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CompleteTaskCommandHandler _handler = null!;
    private Board _board = null!;

    [TestInitialize]
    public void Setup()
    {
        _taskRepository = Substitute.For<ITaskRepository>();
        _boardRepository = Substitute.For<IBoardRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _board = Board.CreateDefault(UserId.Default).Value;
        _boardRepository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(_board);

        _handler = new CompleteTaskCommandHandler(_taskRepository, _boardRepository, _unitOfWork);
    }

    [TestMethod]
    public async Task Should_CompleteTask_When_TaskExists()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id, 0);

        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(task.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.Done, task.Status);
        Assert.IsNotNull(task.CompletedAt);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns((TaskEntity?)null);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(Guid.NewGuid()));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_MoveCardToDoneColumn_When_Completed()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id, 0);

        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(task.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        var card = _board.FindCardByTaskId(task.Id);
        Assert.IsNotNull(card);
        Assert.AreEqual(_board.GetDoneColumn().Id, card.ColumnId);
    }
}
