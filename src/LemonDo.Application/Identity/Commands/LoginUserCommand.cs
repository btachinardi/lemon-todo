namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>Command to authenticate a user with email and password.</summary>
public sealed record LoginUserCommand(string Email, string Password);

/// <summary>
/// Orchestrates login: authenticates via Identity ACL, loads domain User for profile data,
/// generates auth tokens, returns AuthResult with redacted PII.
/// </summary>
public sealed class LoginUserCommandHandler(
    IAuthService authService,
    IUserRepository userRepository,
    ILogger<LoginUserCommandHandler> logger)
{
    /// <summary>Handles user login.</summary>
    public async Task<Result<AuthResult, DomainError>> HandleAsync(LoginUserCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Login attempt for {EmailHash}", LogHelpers.MaskEmail(command.Email));

        // Authenticate via Identity (hash-based lookup + password check)
        var authResult = await authService.AuthenticateAsync(command.Email, command.Password, ct);
        if (authResult.IsFailure)
        {
            logger.LogWarning("Login failed for {EmailHash}: {ErrorCode}",
                LogHelpers.MaskEmail(command.Email), authResult.Error.Code);
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
