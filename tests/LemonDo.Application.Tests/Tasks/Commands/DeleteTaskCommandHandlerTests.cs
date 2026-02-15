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
public sealed class DeleteTaskCommandHandlerTests
{
    private static readonly ApplicationMetrics Metrics = new(new TestMeterFactory());
    private ITaskRepository _taskRepository = null!;
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private DeleteTaskCommandHandler _handler = null!;
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

        _handler = new DeleteTaskCommandHandler(_taskRepository, _boardRepository, _unitOfWork, Substitute.For<ILogger<DeleteTaskCommandHandler>>(), Metrics);
    }

    [TestMethod]
    public async Task Should_DeleteTask_And_RemoveBoardCard()
    {
        var task = TaskEntity.Create(UserId.Default, TaskTitle.Create("To delete").Value).Value;
        var columnId = _board.GetInitialColumn().Id;
        _board.PlaceTask(task.Id, columnId);
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new DeleteTaskCommand(task.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        Assert.IsTrue(task.IsDeleted, "Task should be soft-deleted");
        Assert.IsNull(_board.FindCardByTaskId(task.Id), "Board card should be removed");
        await _boardRepository.Received(1).UpdateAsync(Arg.Any<Board>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_TaskNotFound()
    {
        _taskRepository.GetByIdAsync(Arg.Any<TaskId>(), Arg.Any<CancellationToken>())
            .Returns((TaskEntity?)null);

        var result = await _handler.HandleAsync(new DeleteTaskCommand(Guid.NewGuid()));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }
}
