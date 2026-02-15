namespace LemonDo.Application.Tests.Identity;

using LemonDo.Application.Identity;
using LemonDo.Application.Identity.Commands;
using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;

[TestClass]
public sealed class LoginUserCommandHandlerTests
{
    private IAuthService _authService = null!;
    private LoginUserCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _authService = Substitute.For<IAuthService>();
        _handler = new LoginUserCommandHandler(
            _authService,
            Substitute.For<ILogger<LoginUserCommandHandler>>());
    }

    [TestMethod]
    public async Task Should_Login_When_ValidCredentials()
    {
        _authService.LoginAsync("user@example.com", "Pass123!", Arg.Any<CancellationToken>())
            .Returns(Result<AuthResult, DomainError>.Success(
                new AuthResult(Guid.NewGuid(), "user@example.com", "User", "access", "refresh")));

        var command = new LoginUserCommand("user@example.com", "Pass123!");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("user@example.com", result.Value.Email);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_InvalidCredentials()
    {
        _authService.LoginAsync("bad@example.com", "wrong", Arg.Any<CancellationToken>())
            .Returns(Result<AuthResult, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password.")));

        var command = new LoginUserCommand("bad@example.com", "wrong");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.unauthorized", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_ReturnRateLimited_When_AccountLocked()
    {
        _authService.LoginAsync("locked@example.com", "any", Arg.Any<CancellationToken>())
            .Returns(Result<AuthResult, DomainError>.Failure(
                DomainError.RateLimited("auth", "Account temporarily locked.")));

        var command = new LoginUserCommand("locked@example.com", "any");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.rate_limited", result.Error.Code);
    }
}
