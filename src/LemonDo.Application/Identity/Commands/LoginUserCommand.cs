namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>Command to authenticate a user with email and password.</summary>
public sealed record LoginUserCommand(EncryptedField Email, ProtectedValue Password);

/// <summary>
/// Orchestrates login: authenticates via Identity ACL using email hash,
/// loads domain User for profile data, generates auth tokens, returns AuthResult with redacted protected data.
/// </summary>
public sealed class LoginUserCommandHandler(
    IAuthService authService,
    IUserRepository userRepository,
    ILogger<LoginUserCommandHandler> logger)
{
    /// <summary>Handles user login.</summary>
    public async Task<Result<AuthResult, DomainError>> HandleAsync(LoginUserCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Login attempt for {EmailRedacted}", command.Email.Redacted);

        // Authenticate via Identity (hash-based lookup + password check)
        var emailHash = command.Email.Hash
            ?? throw new InvalidOperationException("EncryptedField for email must have a hash.");
        var authResult = await authService.AuthenticateByHashAsync(emailHash, command.Password.Value, ct);
        if (authResult.IsFailure)
        {
            logger.LogWarning("Login failed for {EmailRedacted}: {ErrorCode}",
                command.Email.Redacted, authResult.Error.Code);
            return Result<AuthResult, DomainError>.Failure(authResult.Error);
        }

        var userId = authResult.Value;

        // Load domain User for redacted profile data
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<AuthResult, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid email or password."));

        // Generate auth tokens (JWT + refresh)
        var tokens = await authService.GenerateTokensAsync(userId, ct);

        logger.LogInformation("User {UserId} logged in successfully", userId);
        return Result<AuthResult, DomainError>.Success(
            new AuthResult(userId.Value, user.RedactedEmail, user.RedactedDisplayName,
                tokens.Roles, tokens.AccessToken, tokens.RefreshToken));
    }
}
