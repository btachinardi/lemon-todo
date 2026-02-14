namespace LemonDo.Application.Tests.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

[TestClass]
public sealed class MoveTaskCommandHandlerTests
{
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

        _handler = new MoveTaskCommandHandler(_taskRepository, _boardRepository, _unitOfWork);
    }

    [TestMethod]
    public async Task Should_MoveTask_When_ValidCommand()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id, 0);

        var doneColumn = _board.GetDoneColumn();
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, doneColumn.Id.Value, 0));

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
        _board.PlaceTask(task.Id, initialColumn.Id, 0);

        var inProgressColumn = _board.Columns.First(c => c.TargetStatus == TaskStatus.InProgress);
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, inProgressColumn.Id.Value, 0));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(TaskStatus.InProgress, task.Status);
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns((TaskEntity?)null);

        var result = await _handler.HandleAsync(new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_Fail_When_ColumnNotFoundOnBoard()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id, 0);

        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, Guid.NewGuid(), 0));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("board.column_not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_Fail_When_NegativePosition()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("Test").Value).Value;
        var initialColumn = _board.GetInitialColumn();
        _board.PlaceTask(task.Id, initialColumn.Id, 0);

        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, initialColumn.Id.Value, -1));

        Assert.IsTrue(result.IsFailure);
    }
}
