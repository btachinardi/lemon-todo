namespace LemonDo.Application.Tests.Identity;

using LemonDo.Application.Common;
using LemonDo.Application.Identity;
using LemonDo.Application.Identity.Commands;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

[TestClass]
public sealed class RegisterUserCommandHandlerTests
{
    private IAuthService _authService = null!;
    private IBoardRepository _boardRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private RegisterUserCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _authService = Substitute.For<IAuthService>();
        _boardRepository = Substitute.For<IBoardRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        _authService.RegisterAsync(
            Arg.Any<UserId>(), Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<DisplayName>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Result<AuthResult, DomainError>.Success(
                new AuthResult(callInfo.ArgAt<UserId>(0).Value, "test@example.com", "Test", ["User"], "access-token", "refresh-token")));

        _handler = new RegisterUserCommandHandler(
            _authService, _boardRepository, _unitOfWork,
            Substitute.For<ILogger<RegisterUserCommandHandler>>());
    }

    [TestMethod]
    public async Task Should_RegisterUser_When_ValidCommand()
    {
        var command = new RegisterUserCommand("test@example.com", "ValidPass123!", "Test User");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("test@example.com", result.Value.Email);
        await _authService.Received(1).RegisterAsync(
            Arg.Any<UserId>(), Arg.Any<Email>(), "ValidPass123!", Arg.Any<DisplayName>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_CreateDefaultBoard_When_RegistrationSucceeds()
    {
        var command = new RegisterUserCommand("board@example.com", "ValidPass123!", "Board User");

        await _handler.HandleAsync(command);

        await _boardRepository.Received(1).AddAsync(Arg.Any<Board>(), Arg.Any<CancellationToken>());
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_InvalidEmail()
    {
        var command = new RegisterUserCommand("not-an-email", "ValidPass123!", "Test User");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("email", result.Error.Code);
        await _authService.DidNotReceive().RegisterAsync(
            Arg.Any<UserId>(), Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<DisplayName>(), Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_EmptyDisplayName()
    {
        var command = new RegisterUserCommand("test@example.com", "ValidPass123!", "");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("displayName", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_Fail_When_DisplayNameTooShort()
    {
        var command = new RegisterUserCommand("test@example.com", "ValidPass123!", "A");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("displayName", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_PropagateAuthServiceError_When_RegistrationFails()
    {
        _authService.RegisterAsync(
            Arg.Any<UserId>(), Arg.Any<Email>(), Arg.Any<string>(), Arg.Any<DisplayName>(), Arg.Any<CancellationToken>())
            .Returns(Result<AuthResult, DomainError>.Failure(
                DomainError.Conflict("auth", "Email already registered.")));

        var command = new RegisterUserCommand("dupe@example.com", "ValidPass123!", "Test User");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.conflict", result.Error.Code);
    }
}
