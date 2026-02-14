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
    private IBoardTaskRepository _repository = null!;
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CompleteTaskCommandHandler _handler = null!;
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
        _repository.GetByColumnAsync(Arg.Any<ColumnId>(), Arg.Any<CancellationToken>())
            .Returns(new List<BoardTask>());

        _handler = new CompleteTaskCommandHandler(_repository, _boardRepository, _unitOfWork);
    }

    [TestMethod]
    public async Task Should_CompleteTask_When_TaskExists()
    {
        var initialColumn = _board.GetInitialColumn();
        var task = BoardTask.Create(
            UserId.Default, initialColumn.Id, 0, BoardTaskStatus.Todo,
            TaskTitle.Create("Test").Value).Value;
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(task.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(BoardTaskStatus.Done, task.Status);
        Assert.AreEqual(_board.GetDoneColumn().Id, task.ColumnId);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns((BoardTask?)null);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(Guid.NewGuid()));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_PositionAtEndOfDoneColumn_When_DoneColumnHasTasks()
    {
        var initialColumn = _board.GetInitialColumn();
        var doneColumn = _board.GetDoneColumn();

        var task = BoardTask.Create(
            UserId.Default, initialColumn.Id, 0, BoardTaskStatus.Todo,
            TaskTitle.Create("Test").Value).Value;
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        // 2 tasks already in done column
        var existingDoneTasks = new List<BoardTask>
        {
            BoardTask.Create(UserId.Default, doneColumn.Id, 0, BoardTaskStatus.Done, TaskTitle.Create("Done 1").Value).Value,
            BoardTask.Create(UserId.Default, doneColumn.Id, 1, BoardTaskStatus.Done, TaskTitle.Create("Done 2").Value).Value,
        };
        _repository.GetByColumnAsync(doneColumn.Id, Arg.Any<CancellationToken>())
            .Returns(existingDoneTasks);

        var result = await _handler.HandleAsync(new CompleteTaskCommand(task.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, task.Position);
    }
}
