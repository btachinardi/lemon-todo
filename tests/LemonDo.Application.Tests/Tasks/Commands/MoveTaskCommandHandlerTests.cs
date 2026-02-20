namespace LemonDo.Application.Tests.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

[TestClass]
public sealed class MoveTaskCommandHandlerTests
{
    private static readonly ApplicationMetrics Metrics = new(new TestMeterFactory());
    private ITaskRepository _taskRepository = null!;
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private MoveTaskCommandHandler _handler = null!;
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

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(UserId.Default);
        _handler = new MoveTaskCommandHandler(_taskRepository, _boardRepository, _unitOfWork, currentUser, NullLogger<MoveTaskCommandHandler>.Instance, Metrics);
    }

    [TestMethod]
    public async Task Should_MoveTask_When_ValidCommand()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id);

        var doneColumn = _board.GetDoneColumn();
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, doneColumn.Id.Value,
                PreviousTaskId: null, NextTaskId: null));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.Done, task.Status);
        var card = _board.FindCardByTaskId(task.Id);
        Assert.IsNotNull(card);
        Assert.AreEqual(doneColumn.Id, card.ColumnId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_DeriveStatusFromColumn_When_MovedToInProgressColumn()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id);

        var inProgressColumn = _board.Columns.First(c => c.TargetStatus == TaskStatus.InProgress);
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, inProgressColumn.Id.Value,
                PreviousTaskId: null, NextTaskId: null));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.InProgress, task.Status);
    }

    [TestMethod]
    public async Task Should_ComputeRankBetweenNeighbors_When_MovedBetweenCards()
    {
        var taskA = TaskEntity.Create(UserId.Default, TaskTitle.Create("A").Value).Value;
        var taskB = TaskEntity.Create(UserId.Default, TaskTitle.Create("B").Value).Value;
        var taskC = TaskEntity.Create(UserId.Default, TaskTitle.Create("C").Value).Value;
        var columnId = _board.GetInitialColumn().Id;
        _board.PlaceTask(taskA.Id, columnId);
        _board.PlaceTask(taskB.Id, columnId);
        _board.PlaceTask(taskC.Id, columnId);

        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(taskC);

        // Move C between A and B (same column reorder)
        var result = await _handler.HandleAsync(
            new MoveTaskCommand(taskC.Id.Value, columnId.Value,
                PreviousTaskId: taskA.Id.Value, NextTaskId: taskB.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        var rankA = _board.FindCardByTaskId(taskA.Id)!.Rank;
        var rankC = _board.FindCardByTaskId(taskC.Id)!.Rank;
        var rankB = _board.FindCardByTaskId(taskB.Id)!.Rank;
        Assert.IsGreaterThan(rankA, rankC, "C should be ranked after A");
        Assert.IsLessThan(rankB, rankC, "C should be ranked before B");
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((TaskEntity?)null);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(),
                PreviousTaskId: null, NextTaskId: null));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_Fail_When_ColumnNotFoundOnBoard()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id);

        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, Guid.NewGuid(),
                PreviousTaskId: null, NextTaskId: null));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.column_not_found", result.Error.Code);
    }
}
