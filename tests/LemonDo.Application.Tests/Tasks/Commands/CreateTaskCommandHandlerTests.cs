namespace LemonDo.Application.Tests.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.Commands;
using LemonDo.Domain.Tasks.Entities;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using NSubstitute;

[TestClass]
public sealed class CreateTaskCommandHandlerTests
{
    private ITaskItemRepository _repository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateTaskCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<ITaskItemRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateTaskCommandHandler(_repository, _unitOfWork);
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
        await _repository.Received(1).AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
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
        await _repository.DidNotReceive().AddAsync(Arg.Any<TaskItem>(), Arg.Any<CancellationToken>());
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
