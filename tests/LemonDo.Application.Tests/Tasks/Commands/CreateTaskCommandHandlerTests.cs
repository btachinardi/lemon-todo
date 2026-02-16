namespace LemonDo.Application.Tests.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

[TestClass]
public sealed class CreateTaskCommandHandlerTests
{
    private static readonly ApplicationMetrics Metrics = new(new TestMeterFactory());
    private ITaskRepository _taskRepository = null!;
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateTaskCommandHandler _handler = null!;
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
        _handler = new CreateTaskCommandHandler(_taskRepository, _boardRepository, _unitOfWork, currentUser, Substitute.For<ILogger<CreateTaskCommandHandler>>(), Metrics);
    }

    [TestMethod]
    public async Task Should_CreateTask_When_ValidCommand()
    {
        var command = new CreateTaskCommand("Buy groceries", "Milk and eggs", Priority.High);

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Buy groceries", result.Value.Title);
        Assert.AreEqual("Milk and eggs", result.Value.Description);
        Assert.AreEqual("High", result.Value.Priority);
        await _taskRepository.Received(1).AddAsync(Arg.Any<TaskEntity>(), Arg.Any<SensitiveNote?>(), Arg.Any<CancellationToken>());
        await _boardRepository.Received(1).UpdateAsync(Arg.Any<Board>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_AssignTaskToInitialColumn_When_Created()
    {
        var command = new CreateTaskCommand("New task", null);

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Todo", result.Value.Status);

        // Verify the board was updated (task was placed on it)
        await _boardRepository.Received(1).UpdateAsync(Arg.Any<Board>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_PlaceAtEndOfColumn_When_ColumnHasExistingTasks()
    {
        // Arrange: place 3 tasks on the board's initial column
        var initialColumn = _board.GetInitialColumn();
        var existingTaskIds = new List<TaskId>();
        for (var i = 0; i < 3; i++)
        {
            var t = TaskEntity.Create(UserId.Default, TaskTitle.Create($"Task {i}").Value).Value;
            _board.PlaceTask(t.Id, initialColumn.Id);
            existingTaskIds.Add(t.Id);
        }

        var command = new CreateTaskCommand("New task", null);

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        // The board should now have 4 cards (3 pre-placed + 1 new)
        Assert.HasCount(4, _board.Cards);

        // The new card's rank must be higher than all existing cards
        var existingMaxRank = existingTaskIds
            .Select(id => _board.FindCardByTaskId(id)!.Rank)
            .Max();
        var newTaskId = TaskId.From(result.Value.Id);
        var newCardRank = _board.FindCardByTaskId(newTaskId)!.Rank;
        Assert.IsGreaterThan(existingMaxRank, newCardRank,
            $"New card rank ({newCardRank}) should be > existing max ({existingMaxRank})");
    }

    [TestMethod]
    public async Task Should_CreateTaskWithTags_When_TagsProvided()
    {
        var command = new CreateTaskCommand("Buy groceries", null, Tags: ["shopping", "food"]);

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.HasCount(2, result.Value.Tags);
    }

    [TestMethod]
    public async Task Should_Fail_When_TitleIsEmpty()
    {
        var command = new CreateTaskCommand("", "Description");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        await _taskRepository.DidNotReceive().AddAsync(Arg.Any<TaskEntity>(), Arg.Any<SensitiveNote?>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_TitleExceedsMaxLength()
    {
        var command = new CreateTaskCommand(new string('x', 501), null);

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
    }

    [TestMethod]
    public async Task Should_Fail_When_InvalidTag()
    {
        var command = new CreateTaskCommand("Valid title", null, Tags: [""]);

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
    }
}
