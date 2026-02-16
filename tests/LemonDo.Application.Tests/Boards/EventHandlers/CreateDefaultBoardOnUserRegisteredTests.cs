namespace LemonDo.Application.Tests.Boards.EventHandlers;

using LemonDo.Application.Boards.EventHandlers;
using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Identity.Events;
using LemonDo.Domain.Identity.ValueObjects;
using NSubstitute;

[TestClass]
public sealed class CreateDefaultBoardOnUserRegisteredTests
{
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private CreateDefaultBoardOnUserRegistered _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _boardRepository = Substitute.For<IBoardRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        _handler = new CreateDefaultBoardOnUserRegistered(_boardRepository, _unitOfWork);
    }

    [TestMethod]
    public async Task Should_CreateDefaultBoard_When_UserRegistered()
    {
        var userId = UserId.New();
        var domainEvent = new UserRegisteredEvent(userId);

        await _handler.HandleAsync(domainEvent);

        await _boardRepository.Received(1).AddAsync(
            Arg.Is<Board>(b => b.OwnerId == userId),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_CreateUniqueBoards_When_MultipleUsersRegistered()
    {
        var userId1 = UserId.New();
        var userId2 = UserId.New();

        await _handler.HandleAsync(new UserRegisteredEvent(userId1));
        await _handler.HandleAsync(new UserRegisteredEvent(userId2));

        await _boardRepository.Received(2).AddAsync(
            Arg.Any<Board>(),
            Arg.Any<CancellationToken>());
        await _unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
