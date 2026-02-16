namespace LemonDo.Application.Tests.Identity;

using LemonDo.Application.Identity;
using LemonDo.Application.Identity.Commands;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

[TestClass]
public sealed class LoginUserCommandHandlerTests
{
    private IAuthService _authService = null!;
    private IUserRepository _userRepository = null!;
    private LoginUserCommandHandler _handler = null!;

    private static readonly UserId TestUserId = UserId.New();

    [TestInitialize]
    public void Setup()
    {
        _authService = Substitute.For<IAuthService>();
        _userRepository = Substitute.For<IUserRepository>();

        _handler = new LoginUserCommandHandler(
            _authService,
            _userRepository,
            Substitute.For<ILogger<LoginUserCommandHandler>>());
    }

    [TestMethod]
    public async Task Should_Login_When_ValidCredentials()
    {
        // Arrange: authenticate returns userId, repo returns user, tokens generated
        _authService.AuthenticateAsync("user@example.com", "Pass123!", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Success(TestUserId));

        _userRepository.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(User.Reconstitute(TestUserId, "u***@example.com", "U***r", false));

        _authService.GenerateTokensAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new AuthTokens("access", "refresh", new List<string> { "User" }.AsReadOnly()));

        var command = new LoginUserCommand("user@example.com", "Pass123!");

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("u***@example.com", result.Value.RedactedEmail);
        Assert.AreEqual("U***r", result.Value.RedactedDisplayName);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_InvalidCredentials()
    {
        _authService.AuthenticateAsync("bad@example.com", "wrong", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password.")));

        var command = new LoginUserCommand("bad@example.com", "wrong");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.unauthorized", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_ReturnRateLimited_When_AccountLocked()
    {
        _authService.AuthenticateAsync("locked@example.com", "any", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Failure(
                DomainError.RateLimited("auth", "Account temporarily locked.")));

        var command = new LoginUserCommand("locked@example.com", "any");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.rate_limited", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_UserNotFoundInDomain()
    {
        _authService.AuthenticateAsync("orphan@example.com", "Pass123!", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Success(TestUserId));

        _userRepository.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new LoginUserCommand("orphan@example.com", "Pass123!");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.unauthorized", result.Error.Code);
    }
}
