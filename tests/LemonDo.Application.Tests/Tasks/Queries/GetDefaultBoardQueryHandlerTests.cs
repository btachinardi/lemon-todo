namespace LemonDo.Application.Tests.Tasks.Queries;

using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

[TestClass]
public sealed class GetDefaultBoardQueryHandlerTests
{
    private IBoardRepository _boardRepository = null!;
    private ITaskRepository _taskRepository = null!;
    private GetDefaultBoardQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _boardRepository = Substitute.For<IBoardRepository>();
        _taskRepository = Substitute.For<ITaskRepository>();
        _handler = new GetDefaultBoardQueryHandler(_boardRepository, _taskRepository, NullLogger<GetDefaultBoardQueryHandler>.Instance);
    }

    [TestMethod]
    public async Task Should_ReturnBoard_When_DefaultExists()
    {
        var board = Board.CreateDefault(UserId.Default).Value;
        _boardRepository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(board);

        var result = await _handler.HandleAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("My Board", result.Value.Name);
        Assert.HasCount(3, result.Value.Columns);
    }

    [TestMethod]
    public async Task Should_ReturnNotFound_When_NoDefaultBoard()
    {
        _boardRepository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Board?)null);

        var result = await _handler.HandleAsync();

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_ExcludeCardsForDeletedTasks_When_ReturningBoard()
    {
        var board = Board.CreateDefault(UserId.Default).Value;
        var columnId = board.GetInitialColumn().Id;

        // Place two tasks on the board
        var activeTask = TaskEntity.Create(UserId.Default, TaskTitle.Create("Active").Value).Value;
        var deletedTask = TaskEntity.Create(UserId.Default, TaskTitle.Create("Deleted").Value).Value;
        deletedTask.Delete();

        board.PlaceTask(activeTask.Id, columnId);
        board.PlaceTask(deletedTask.Id, columnId);

        _boardRepository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(board);

        // Task repository returns only the active task as non-deleted
        _taskRepository.GetActiveTaskIdsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<TaskId> { activeTask.Id });

        var result = await _handler.HandleAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Value.Cards!);
        Assert.AreEqual(activeTask.Id.Value, result.Value.Cards![0].TaskId);
    }

    [TestMethod]
    public async Task Should_ExcludeCardsForArchivedTasks_When_ReturningBoard()
    {
        var board = Board.CreateDefault(UserId.Default).Value;
        var columnId = board.GetInitialColumn().Id;

        var activeTask = TaskEntity.Create(UserId.Default, TaskTitle.Create("Active").Value).Value;
        var archivedTask = TaskEntity.Create(UserId.Default, TaskTitle.Create("Archived").Value).Value;
        archivedTask.Archive();

        board.PlaceTask(activeTask.Id, columnId);
        board.PlaceTask(archivedTask.Id, columnId);

        _boardRepository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(board);

        _taskRepository.GetActiveTaskIdsAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<TaskId> { activeTask.Id });

        var result = await _handler.HandleAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(1, result.Value.Cards!);
        Assert.AreEqual(activeTask.Id.Value, result.Value.Cards![0].TaskId);
    }
}
