namespace LemonDo.Application.Tests.Identity;

using LemonDo.Application.Common;
using LemonDo.Application.Identity;
using LemonDo.Application.Identity.Commands;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;
using NSubstitute;

[TestClass]
public sealed class RegisterUserCommandHandlerTests
{
    private IAuthService _authService = null!;
    private IUserRepository _userRepository = null!;
    private IUnitOfWork _unitOfWork = null!;
    private RegisterUserCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _authService = Substitute.For<IAuthService>();
        _userRepository = Substitute.For<IUserRepository>();
        _unitOfWork = Substitute.For<IUnitOfWork>();

        // Default: credentials creation succeeds
        _authService.CreateCredentialsAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainError>.Success());

        // Default: token generation returns valid tokens
        _authService.GenerateTokensAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(new AuthTokens("access-token", "refresh-token", new List<string> { "User" }.AsReadOnly()));

        _handler = new RegisterUserCommandHandler(
            _authService, _userRepository, _unitOfWork,
            Substitute.For<ILogger<RegisterUserCommandHandler>>());
    }

    [TestMethod]
    public async Task Should_RegisterUser_When_ValidCommand()
    {
        var command = new RegisterUserCommand("test@example.com", "ValidPass123!", "Test User");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("t***@example.com", result.Value.RedactedEmail);
        Assert.AreEqual("T***r", result.Value.RedactedDisplayName);

        // Verify credentials were created
        await _authService.Received(1).CreateCredentialsAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), "ValidPass123!", Arg.Any<CancellationToken>());

        // Verify domain User was persisted
        await _userRepository.Received(1).AddAsync(
            Arg.Any<LemonDo.Domain.Identity.Entities.User>(),
            Arg.Any<Email>(), Arg.Any<DisplayName>(), Arg.Any<CancellationToken>());

        // Verify unit of work was committed (events dispatched)
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [TestMethod]
    public async Task Should_Fail_When_InvalidEmail()
    {
        var command = new RegisterUserCommand("not-an-email", "ValidPass123!", "Test User");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.Contains("email", result.Error.Code);

        // Auth service should not be called
        await _authService.DidNotReceive().CreateCredentialsAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
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
    public async Task Should_PropagateAuthServiceError_When_CredentialCreationFails()
    {
        _authService.CreateCredentialsAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainError>.Failure(
                DomainError.Conflict("auth", "Email already registered.")));

        var command = new RegisterUserCommand("dupe@example.com", "ValidPass123!", "Test User");

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.conflict", result.Error.Code);

        // Domain User should NOT be persisted when credentials fail
        await _userRepository.DidNotReceive().AddAsync(
            Arg.Any<LemonDo.Domain.Identity.Entities.User>(),
            Arg.Any<Email>(), Arg.Any<DisplayName>(), Arg.Any<CancellationToken>());
    }
}
