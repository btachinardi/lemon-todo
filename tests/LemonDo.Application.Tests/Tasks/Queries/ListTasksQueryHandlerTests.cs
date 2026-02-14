namespace LemonDo.Application.Tests.Tasks.Queries;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

[TestClass]
public sealed class ListTasksQueryHandlerTests
{
    private IBoardTaskRepository _repository = null!;
    private ListTasksQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<IBoardTaskRepository>();
        _handler = new ListTasksQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Should_ReturnPagedTasks_When_Queried()
    {
        var task1 = BoardTask.Create(UserId.Default, TaskTitle.Create("Task 1").Value, null, Priority.None).Value;
        var task2 = BoardTask.Create(UserId.Default, TaskTitle.Create("Task 2").Value, null, Priority.High).Value;

        _repository.ListAsync(
            Arg.Any<UserId>(),
            Arg.Any<ColumnId?>(), Arg.Any<Priority?>(), Arg.Any<BoardTaskStatus?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BoardTask>([task1, task2], 2, 1, 50));

        var result = await _handler.HandleAsync(new ListTasksQuery(new TaskListFilter()));

        Assert.AreEqual(2, result.TotalCount);
        Assert.HasCount(2, result.Items);
        Assert.AreEqual("Task 1", result.Items[0].Title);
        Assert.AreEqual("Task 2", result.Items[1].Title);
    }

    [TestMethod]
    public async Task Should_ReturnEmpty_When_NoTasks()
    {
        _repository.ListAsync(
            Arg.Any<UserId>(),
            Arg.Any<ColumnId?>(), Arg.Any<Priority?>(), Arg.Any<BoardTaskStatus?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BoardTask>([], 0, 1, 50));

        var result = await _handler.HandleAsync(new ListTasksQuery(new TaskListFilter()));

        Assert.AreEqual(0, result.TotalCount);
        Assert.IsEmpty(result.Items);
    }

    [TestMethod]
    public async Task Should_PassFilterToRepository_When_Provided()
    {
        var columnId = ColumnId.New();
        var filter = new TaskListFilter
        {
            ColumnId = columnId,
            Priority = Priority.High,
            Status = BoardTaskStatus.Todo,
            SearchTerm = "groceries",
            Page = 2,
            PageSize = 10
        };

        _repository.ListAsync(
            Arg.Any<UserId>(),
            Arg.Any<ColumnId?>(), Arg.Any<Priority?>(), Arg.Any<BoardTaskStatus?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(new PagedResult<BoardTask>([], 0, 2, 10));

        await _handler.HandleAsync(new ListTasksQuery(filter));

        await _repository.Received(1).ListAsync(
            UserId.Default,
            columnId,
            Priority.High,
            BoardTaskStatus.Todo,
            "groceries",
            2,
            10,
            Arg.Any<CancellationToken>());
    }
}
