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

    // Test helper: creates an EncryptedField for email with a fake hash
    private static EncryptedField TestEmailField(string redacted = "u***@example.com", string hash = "test-email-hash")
        => new("encrypted-email", redacted, hash);

    // Test helper: creates a ProtectedValue for password
    private static ProtectedValue TestPassword(string raw = "Pass123!")
        => new(raw);

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
        // Arrange: authenticate by hash returns userId, repo returns user, tokens generated
        _authService.AuthenticateByHashAsync("test-email-hash", "Pass123!", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Success(TestUserId));

        _userRepository.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(User.Reconstitute(TestUserId, "u***@example.com", "U***r", false, null));

        _authService.GenerateTokensAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns(new AuthTokens("access", "refresh", new List<string> { "User" }.AsReadOnly()));

        var command = new LoginUserCommand(TestEmailField(), TestPassword());

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
        _authService.AuthenticateByHashAsync("bad-email-hash", "wrong", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password.")));

        var command = new LoginUserCommand(
            new EncryptedField("encrypted-bad", "b***@example.com", "bad-email-hash"),
            new ProtectedValue("wrong"));
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.unauthorized", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_ReturnRateLimited_When_AccountLocked()
    {
        _authService.AuthenticateByHashAsync("locked-email-hash", "any", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Failure(
                DomainError.RateLimited("auth", "Account temporarily locked.")));

        var command = new LoginUserCommand(
            new EncryptedField("encrypted-locked", "l***@example.com", "locked-email-hash"),
            new ProtectedValue("any"));
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.rate_limited", result.Error.Code);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_UserNotFoundInDomain()
    {
        _authService.AuthenticateByHashAsync("orphan-email-hash", "Pass123!", Arg.Any<CancellationToken>())
            .Returns(Result<UserId, DomainError>.Success(TestUserId));

        _userRepository.GetByIdAsync(TestUserId, Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var command = new LoginUserCommand(
            new EncryptedField("encrypted-orphan", "o***@example.com", "orphan-email-hash"),
            new ProtectedValue("Pass123!"));
        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.unauthorized", result.Error.Code);
    }
}
