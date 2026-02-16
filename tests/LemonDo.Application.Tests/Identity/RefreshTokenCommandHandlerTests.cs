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
public sealed class RefreshTokenCommandHandlerTests
{
    private IAuthService _authService = null!;
    private IUserRepository _userRepository = null!;
    private RefreshTokenCommandHandler _handler = null!;

    private static readonly UserId TestUserId = UserId.New();

    [TestInitialize]
    public void Setup()
    {
        _authService = Substitute.For<IAuthService>();
        _userRepository = Substitute.For<IUserRepository>();

        _handler = new RefreshTokenCommandHandler(
            _authService,
            _userRepository,
            Substitute.For<ILogger<RefreshTokenCommandHandler>>());
    }

    [TestMethod]
    public async Task Should_RefreshTokens_When_ValidRefreshToken()
    {
        _authService.RefreshTokenAsync("valid-refresh-token", Arg.Any<CancellationToken>())
            .Returns(Result<(UserId, AuthTokens), DomainError>.Success(
                (TestUserId, new AuthTokens("new-access", "new-refresh", new List<string> { "User" }.AsReadOnly()))));

        _userRepository.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(User.Reconstitute(TestUserId, "t***@example.com", "T***r", false));

        var command = new RefreshTokenCommand("valid-refresh-token");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("new-access", result.Value.AccessToken);
        Assert.AreEqual("new-refresh", result.Value.RefreshToken);
        Assert.AreEqual("t***@example.com", result.Value.RedactedEmail);
        Assert.AreEqual("T***r", result.Value.RedactedDisplayName);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_InvalidRefreshToken()
    {
        _authService.RefreshTokenAsync("invalid-token", Arg.Any<CancellationToken>())
            .Returns(Result<(UserId, AuthTokens), DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid or expired refresh token.")));

        var command = new RefreshTokenCommand("invalid-token");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.unauthorized", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_UserNotFoundInDomain()
    {
        _authService.RefreshTokenAsync("orphan-token", Arg.Any<CancellationToken>())
            .Returns(Result<(UserId, AuthTokens), DomainError>.Success(
                (TestUserId, new AuthTokens("access", "refresh", new List<string> { "User" }.AsReadOnly()))));

        _userRepository.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new RefreshTokenCommand("orphan-token");
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.unauthorized", result.Error.Code);
    }
}
