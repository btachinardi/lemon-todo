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

    // Test helper: creates an EncryptedField for email with a fake hash
    private static EncryptedField TestEmailField(string redacted = "t***@example.com", string hash = "test-email-hash")
        => new("encrypted-email", redacted, hash);

    // Test helper: creates an EncryptedField for display name
    private static EncryptedField TestDisplayNameField(string redacted = "T***r")
        => new("encrypted-displayname", redacted);

    // Test helper: creates a ProtectedValue for password
    private static ProtectedValue TestPassword(string raw = "ValidPass123!")
        => new(raw);

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
        var command = new RegisterUserCommand(TestEmailField(), TestPassword(), TestDisplayNameField());

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("t***@example.com", result.Value.RedactedEmail);
        Assert.AreEqual("T***r", result.Value.RedactedDisplayName);

        // Verify credentials were created with hash and raw password
        await _authService.Received(1).CreateCredentialsAsync(
            Arg.Any<UserId>(), "test-email-hash", "ValidPass123!", Arg.Any<CancellationToken>());

        // Verify domain User was persisted with EncryptedFields
        await _userRepository.Received(1).AddAsync(
            Arg.Any<LemonDo.Domain.Identity.Entities.User>(),
            Arg.Any<EncryptedField>(), Arg.Any<EncryptedField>(), Arg.Any<CancellationToken>());

        // Verify unit of work was committed (events dispatched)
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // Note: Validation tests for invalid email, empty display name, and short display name
    // have been removed. Validation now happens at the JSON converter level (API boundary)
    // during deserialization into EncryptedField. The handler receives pre-validated data.

    [TestMethod]
    public async Task Should_PropagateAuthServiceError_When_CredentialCreationFails()
    {
        _authService.CreateCredentialsAsync(
            Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result<DomainError>.Failure(
                DomainError.Conflict("auth", "Email already registered.")));

        var command = new RegisterUserCommand(TestEmailField(), TestPassword(), TestDisplayNameField());

        var result = await _handler.HandleAsync(command);

        Assert.IsTrue(result.IsFailure);
        Assert.AreEqual("auth.conflict", result.Error.Code);

        // Domain User should NOT be persisted when credentials fail
        await _userRepository.DidNotReceive().AddAsync(
            Arg.Any<LemonDo.Domain.Identity.Entities.User>(),
            Arg.Any<EncryptedField>(), Arg.Any<EncryptedField>(), Arg.Any<CancellationToken>());
    }
}
