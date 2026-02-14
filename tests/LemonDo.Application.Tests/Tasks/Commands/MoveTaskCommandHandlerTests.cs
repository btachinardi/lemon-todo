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
    private IBoardTaskRepository _repository = null!;
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private MoveTaskCommandHandler _handler = null!;
    private Board _board = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<IBoardTaskRepository>();
        _boardRepository = Substitute.For<IBoardRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _board = Board.CreateDefault(UserId.Default).Value;
        _boardRepository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(_board);

        _handler = new MoveTaskCommandHandler(_repository, _boardRepository, _unitOfWork);
    }

    [TestMethod]
    public async Task Should_MoveTask_When_ValidCommand()
    {
        var initialColumn = _board.GetInitialColumn();
        var task = BoardTask.Create(
            UserId.Default, initialColumn.Id, 0, BoardTaskStatus.Todo,
            TaskTitle.Create("Test").Value).Value;
        var doneColumn = _board.GetDoneColumn();

        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, doneColumn.Id.Value, 2));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(doneColumn.Id, task.ColumnId);
        Assert.AreEqual(2, task.Position);
        Assert.AreEqual(BoardTaskStatus.Done, task.Status);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_DeriveStatusFromColumn_When_MovedToInProgressColumn()
    {
        var initialColumn = _board.GetInitialColumn();
        var task = BoardTask.Create(
            UserId.Default, initialColumn.Id, 0, BoardTaskStatus.Todo,
            TaskTitle.Create("Test").Value).Value;

        var inProgressColumn = _board.Columns.First(c => c.TargetStatus == BoardTaskStatus.InProgress);
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, inProgressColumn.Id.Value, 0));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(BoardTaskStatus.InProgress, task.Status);
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns((BoardTask?)null);

        var result = await _handler.HandleAsync(new MoveTaskCommand(Guid.NewGuid(), Guid.NewGuid(), 0));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_Fail_When_ColumnNotFoundOnBoard()
    {
        var initialColumn = _board.GetInitialColumn();
        var task = BoardTask.Create(
            UserId.Default, initialColumn.Id, 0, BoardTaskStatus.Todo,
            TaskTitle.Create("Test").Value).Value;
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, Guid.NewGuid(), 0));

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("column.not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_Fail_When_NegativePosition()
    {
        var initialColumn = _board.GetInitialColumn();
        var task = BoardTask.Create(
            UserId.Default, initialColumn.Id, 0, BoardTaskStatus.Todo,
            TaskTitle.Create("Test").Value).Value;
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(
            new MoveTaskCommand(task.Id.Value, initialColumn.Id.Value, -1));

        Assert.IsTrue(result.IsFailure);
    }
}
