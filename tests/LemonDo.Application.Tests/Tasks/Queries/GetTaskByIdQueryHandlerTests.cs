namespace LemonDo.Application.Tests.Tasks.Queries;

using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

[TestClass]
public sealed class GetTaskByIdQueryHandlerTests
{
    private IBoardTaskRepository _repository = null!;
    private GetTaskByIdQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<IBoardTaskRepository>();
        _handler = new GetTaskByIdQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Should_ReturnTask_When_Exists()
    {
        var columnId = ColumnId.New();
        var task = BoardTask.Create(
            UserId.Default, columnId, 0, BoardTaskStatus.Todo,
            TaskTitle.Create("Test").Value, priority: Priority.High).Value;
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns(task);

        var result = await _handler.HandleAsync(new GetTaskByIdQuery(task.Id.Value));

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("Test", result.Value.Title);
        Assert.AreEqual("High", result.Value.Priority);
    }

    [TestMethod]
    public async Task Should_ReturnFailure_When_NotFound()
    {
        _repository.GetByIdAsync(Arg.Any<BoardTaskId>(), Arg.Any<CancellationToken>())
            .Returns((BoardTask?)null);

        var result = await _handler.HandleAsync(new GetTaskByIdQuery(Guid.NewGuid()));

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }
}
