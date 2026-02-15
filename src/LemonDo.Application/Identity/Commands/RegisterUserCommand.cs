namespace LemonDo.Application.Identity.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.Entities;
using LemonDo.Domain.Identity.ValueObjects;
using Microsoft.Extensions.Logging;

/// <summary>Command to register a new user account.</summary>
public sealed record RegisterUserCommand(string Email, string Password, string DisplayName);

/// <summary>
/// Validates domain VOs, creates the domain User entity (raising UserRegisteredEvent),
/// delegates Identity registration to <see cref="IAuthService"/>, and creates a default board.
/// </summary>
public sealed class RegisterUserCommandHandler(
    IAuthService authService,
    IBoardRepository boardRepository,
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

        // Create domain entity (raises UserRegisteredEvent)
        var userResult = User.Create(emailResult.Value, displayNameResult.Value);
        if (userResult.IsFailure)
            return Result<AuthResult, DomainError>.Failure(userResult.Error);

        var user = userResult.Value;

        // Delegate to Identity via ACL
        var authResult = await authService.RegisterAsync(
            user.Id, emailResult.Value, command.Password, displayNameResult.Value, ct);

        if (authResult.IsFailure)
            return authResult;

        // Create default board for the new user
        var boardResult = Board.CreateDefault(user.Id);
        if (boardResult.IsSuccess)
        {
            await boardRepository.AddAsync(boardResult.Value, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        logger.LogInformation("User {UserId} registered successfully", user.Id);
        return authResult;
    }
}
