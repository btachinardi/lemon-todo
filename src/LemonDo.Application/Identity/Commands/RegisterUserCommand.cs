namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.Repositories;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>Command to register a new user account.</summary>
public sealed record RegisterUserCommand(EncryptedField Email, ProtectedValue Password, EncryptedField DisplayName);

/// <summary>
/// Orchestrates registration: creates domain User from pre-validated EncryptedFields,
/// creates Identity credentials, persists User (with encrypted protected data), generates auth tokens.
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
        logger.LogInformation("Registering user {EmailRedacted}", command.Email.Redacted);

        // EncryptedField already contains validated + encrypted data from JSON deserialization.
        // Reconstruct domain VOs from the redacted form for the domain User entity.
        var email = Email.Reconstruct(command.Email.Redacted);
        var displayName = DisplayName.Reconstruct(command.DisplayName.Redacted);

        // Create domain User (stores redacted strings, raises UserRegisteredEvent)
        var userResult = User.Create(email, displayName);
        if (userResult.IsFailure)
            return Result<AuthResult, DomainError>.Failure(userResult.Error);

        var user = userResult.Value;

        // Create Identity credentials (password hash, "User" role — no profile data)
        var emailHash = command.Email.Hash
            ?? throw new InvalidOperationException("EncryptedField for email must have a hash.");
        var credResult = await authService.CreateCredentialsAsync(user.Id, emailHash, command.Password.Value, ct);
        if (credResult.IsFailure)
            return Result<AuthResult, DomainError>.Failure(credResult.Error);

        // Persist domain User (repository stores encrypted fields into shadow columns)
        await userRepository.AddAsync(user, command.Email, command.DisplayName, ct);

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
