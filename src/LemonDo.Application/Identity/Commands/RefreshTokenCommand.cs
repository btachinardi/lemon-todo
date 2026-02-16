namespace LemonDo.Application.Identity.Commands;

using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Repositories;
using Microsoft.Extensions.Logging;

/// <summary>Command to exchange a refresh token for a new token pair.</summary>
public sealed record RefreshTokenCommand(string RefreshToken);

/// <summary>
/// Orchestrates token refresh: validates refresh token via Identity ACL,
/// loads domain User for profile data, returns AuthResult with redacted PII.
/// </summary>
public sealed class RefreshTokenCommandHandler(
    IAuthService authService,
    IUserRepository userRepository,
    ILogger<RefreshTokenCommandHandler> logger)
{
    /// <summary>Handles token refresh.</summary>
    public async Task<Result<AuthResult, DomainError>> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var result = await authService.RefreshTokenAsync(command.RefreshToken, ct);
        if (result.IsFailure)
        {
            logger.LogWarning("Token refresh failed: {ErrorCode}", result.Error.Code);
            return Result<AuthResult, DomainError>.Failure(result.Error);
        }

        var (userId, tokens) = result.Value;

        // Load domain User for redacted profile data
        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            return Result<AuthResult, DomainError>.Failure(
                DomainError.Unauthorized("auth", "Invalid or expired refresh token."));

        logger.LogInformation("Token refreshed for user {UserId}", userId);
        return Result<AuthResult, DomainError>.Success(
            new AuthResult(userId.Value, user.RedactedEmail, user.RedactedDisplayName,
                tokens.Roles, tokens.AccessToken, tokens.RefreshToken));
    }
}
