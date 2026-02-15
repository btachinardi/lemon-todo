namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using Microsoft.Extensions.Logging;

/// <summary>Command to authenticate a user with email and password.</summary>
public sealed record LoginUserCommand(string Email, string Password);

/// <summary>Delegates authentication to <see cref="IAuthService"/>.</summary>
public sealed class LoginUserCommandHandler(
    IAuthService authService,
    ILogger<LoginUserCommandHandler> logger)
{
    /// <summary>Handles user login.</summary>
    public async Task<Result<AuthResult, DomainError>> HandleAsync(LoginUserCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Login attempt for {EmailHash}", LogHelpers.MaskEmail(command.Email));

        var result = await authService.LoginAsync(command.Email, command.Password, ct);

        if (result.IsFailure)
            logger.LogWarning("Login failed for {EmailHash}: {ErrorCode}", LogHelpers.MaskEmail(command.Email), result.Error.Code);
        else
            logger.LogInformation("User {UserId} logged in successfully", result.Value.UserId);

        return result;
    }
}
