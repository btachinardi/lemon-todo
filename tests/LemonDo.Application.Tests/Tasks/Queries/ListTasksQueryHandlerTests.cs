namespace LemonDo.Application.Tests.Tasks.Queries;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

[TestClass]
public sealed class ListTasksQueryHandlerTests
{
    private ITaskRepository _repository = null!;
    private ListTasksQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<ITaskRepository>();
        _handler = new ListTasksQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Should_ReturnPagedTasks_When_Queried()
    {
        var task1 = TaskEntity.Create(
            UserId.Default, TaskTitle.Create("Task 1").Value).Value;
        var task2 = TaskEntity.Create(
            UserId.Default, TaskTitle.Create("Task 2").Value, priority: Priority.High).Value;

        _repository.ListAsync(
            Arg.Any<UserId>(),
            Arg.Any<Priority?>(), Arg.Any<TaskStatus?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TaskEntity>([task1, task2], 2, 1, 50));

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
            Arg.Any<Priority?>(), Arg.Any<TaskStatus?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TaskEntity>([], 0, 1, 50));

        var result = await _handler.HandleAsync(new ListTasksQuery(new TaskListFilter()));

        Assert.AreEqual(0, result.TotalCount);
        Assert.IsEmpty(result.Items);
    }

    [TestMethod]
    public async Task Should_PassFilterToRepository_When_Provided()
    {
        var filter = new TaskListFilter
        {
            Priority = Priority.High,
            Status = TaskStatus.Todo,
            SearchTerm = "groceries",
            Page = 2,
            PageSize = 10
        };

        _repository.ListAsync(
            Arg.Any<UserId>(),
            Arg.Any<Priority?>(), Arg.Any<TaskStatus?>(),
            Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>())
            .Returns(new PagedResult<TaskEntity>([], 0, 2, 10));

        await _handler.HandleAsync(new ListTasksQuery(filter));

        await _repository.Received(1).ListAsync(
            UserId.Default,
            Priority.High,
            TaskStatus.Todo,
            "groceries",
            2,
            10,
            Arg.Any<CancellationToken>());
    }
}
