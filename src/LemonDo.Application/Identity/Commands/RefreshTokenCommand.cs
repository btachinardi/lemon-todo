namespace LemonDo.Application.Identity.Commands;

using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>Command to exchange a refresh token for a new token pair.</summary>
public sealed record RefreshTokenCommand(string RefreshToken);

/// <summary>Delegates token refresh to <see cref="IAuthService"/>.</summary>
public sealed class RefreshTokenCommandHandler(
    IAuthService authService,
    ILogger<RefreshTokenCommandHandler> logger)
{
    /// <summary>Handles token refresh.</summary>
    public async Task<Result<AuthResult, DomainError>> HandleAsync(RefreshTokenCommand command, CancellationToken ct = default)
    {
        var result = await authService.RefreshTokenAsync(command.RefreshToken, ct);

        if (result.IsSuccess)
            logger.LogInformation("Token refreshed for user {UserId}", result.Value.UserId);
        else
            logger.LogWarning("Token refresh failed: {ErrorCode}", result.Error.Code);

        return result;
    }
}
