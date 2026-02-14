namespace LemonDo.Application.Tests.Tasks.Queries;

using LemonDo.Application.Tasks.Queries;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using NSubstitute;

[TestClass]
public sealed class GetDefaultBoardQueryHandlerTests
{
    private IBoardRepository _repository = null!;
    private GetDefaultBoardQueryHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _repository = Substitute.For<IBoardRepository>();
        _handler = new GetDefaultBoardQueryHandler(_repository);
    }

    [TestMethod]
    public async Task Should_ReturnBoard_When_DefaultExists()
    {
        var board = Board.CreateDefault(UserId.Default).Value;
        _repository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(board);

        var result = await _handler.HandleAsync();

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("My Board", result.Value.Name);
        Assert.HasCount(3, result.Value.Columns);
    }

    [TestMethod]
    public async Task Should_ReturnNotFound_When_NoDefaultBoard()
    {
        _repository.GetDefaultForUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns((Board?)null);

        var result = await _handler.HandleAsync();

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("not_found", result.Error.Code);
    }
}
