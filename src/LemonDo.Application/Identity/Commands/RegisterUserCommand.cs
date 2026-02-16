namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>Command to register a new user account.</summary>
public sealed record RegisterUserCommand(string Email, string Password, string DisplayName);

/// <summary>
/// Orchestrates registration: validates VOs, creates domain User, creates Identity credentials,
/// persists User (with encrypted protected data), generates auth tokens.
/// Default board creation is handled by <see cref="LemonDo.Application.Boards.EventHandlers.CreateDefaultBoardOnUserRegistered"/>
/// reacting to the <see cref="LemonDo.Domain.Identity.Events.UserRegisteredEvent"/>.
/// </summary>
public sealed class RegisterUserCommandHandler(
    IAuthService authService,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork,
    ILogger<RegisterUserCommandHandler> logger)
{
    /// <summary>Handles user registration.</summary>
    public async Task<Result<AuthResult, DomainError>> HandleAsync(RegisterUserCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Registering user {EmailHash}", LogHelpers.MaskEmail(command.Email));

        var emailResult = Email.Create(command.Email);
        if (emailResult.IsFailure)
            return Result<AuthResult, DomainError>.Failure(emailResult.Error);

        var displayNameResult = DisplayName.Create(command.DisplayName);
        if (displayNameResult.IsFailure)
            return Result<AuthResult, DomainError>.Failure(displayNameResult.Error);

        var email = emailResult.Value;
        var displayName = displayNameResult.Value;

        // Create domain User (stores redacted strings, raises UserRegisteredEvent)
        var userResult = User.Create(email, displayName);
        if (userResult.IsFailure)
            return Result<AuthResult, DomainError>.Failure(userResult.Error);

        var user = userResult.Value;

        // Create Identity credentials (password hash, "User" role — no profile data)
        var emailHash = ProtectedDataHasher.HashEmail(email.Value);
        var credResult = await authService.CreateCredentialsAsync(user.Id, emailHash, command.Password, ct);
        if (credResult.IsFailure)
            return Result<AuthResult, DomainError>.Failure(credResult.Error);

        // Persist domain User (repository encrypts protected data into shadow columns)
        await userRepository.AddAsync(user, email, displayName, ct);

        // Save User + dispatch UserRegisteredEvent (board creation handled by event handler)
        await unitOfWork.SaveChangesAsync(ct);

        // Generate auth tokens
        var tokens = await authService.GenerateTokensAsync(user.Id, ct);

        logger.LogInformation("User {UserId} registered successfully", user.Id);
        return Result<AuthResult, DomainError>.Success(
            new AuthResult(user.Id.Value, user.RedactedEmail, user.RedactedDisplayName,
                tokens.Roles, tokens.AccessToken, tokens.RefreshToken));
    }
}
