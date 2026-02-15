namespace LemonDo.Application.Identity.Commands;

using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>Command to revoke a refresh token (used on logout).</summary>
public sealed record RevokeRefreshTokenCommand(string? RefreshToken);

/// <summary>Delegates token revocation to <see cref="IAuthService"/>. No-op when token is null.</summary>
public sealed class RevokeRefreshTokenCommandHandler(
    IAuthService authService,
    ILogger<RevokeRefreshTokenCommandHandler> logger)
{
    /// <summary>Handles refresh token revocation.</summary>
    public async Task<Result<DomainError>> HandleAsync(RevokeRefreshTokenCommand command, CancellationToken ct = default)
    {
        if (command.RefreshToken is null)
            return Result<DomainError>.Success();

        var result = await authService.RevokeRefreshTokenAsync(command.RefreshToken, ct);

        if (result.IsSuccess)
            logger.LogInformation("Refresh token revoked");
        else
            logger.LogWarning("Token revocation failed: {ErrorCode}", result.Error.Code);

        return result;
    }
}
