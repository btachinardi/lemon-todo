namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Administration;
using LemonDo.Application.Common;
using LemonDo.Application.Identity;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Command for a task owner to view their own encrypted sensitive note.
/// Requires password re-authentication for security.
/// </summary>
public sealed record ViewTaskNoteCommand(Guid TaskId, ProtectedValue Password);

/// <summary>
/// Handles <see cref="ViewTaskNoteCommand"/>: verifies ownership, re-authenticates the user,
/// decrypts the sensitive note, and records an audit entry.
/// </summary>
public sealed class ViewTaskNoteCommandHandler(
    ITaskRepository taskRepository,
    IAuthService authService,
    IAuditService auditService,
    ICurrentUserService currentUser)
{
    /// <summary>Decrypts and returns the task's sensitive note after ownership and password verification.</summary>
    public async Task<Result<RevealedField, DomainError>> HandleAsync(ViewTaskNoteCommand command, CancellationToken ct = default)
    {
        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<RevealedField, DomainError>.Failure(DomainError.NotFound("Task", command.TaskId.ToString()));

        // Verify ownership
        if (task.OwnerId != currentUser.UserId)
            return Result<RevealedField, DomainError>.Failure(
                DomainError.BusinessRule("task.not_owner", "You can only view notes on your own tasks."));

        // Verify the task has a sensitive note
        if (task.RedactedSensitiveNote is null)
            return Result<RevealedField, DomainError>.Failure(
                DomainError.NotFound("SensitiveNote", command.TaskId.ToString()));

        // Re-authenticate the user
        var passwordResult = await authService.VerifyPasswordAsync(
            currentUser.UserId.Value, command.Password.Value, ct);
        if (passwordResult.IsFailure)
            return Result<RevealedField, DomainError>.Failure(passwordResult.Error);

        // Get the encrypted note as a RevealedField (decryption happens at JSON serialization)
        var revealResult = await taskRepository.GetEncryptedSensitiveNoteAsync(
            TaskId.From(command.TaskId), ct);
        if (revealResult.IsFailure)
            return Result<RevealedField, DomainError>.Failure(revealResult.Error);

        // Audit the access
        await auditService.RecordAsync(
            AuditAction.SensitiveNoteRevealed,
            "Task",
            command.TaskId.ToString(),
            "Owner viewed their own sensitive note",
            cancellationToken: ct);

        return Result<RevealedField, DomainError>.Success(revealResult.Value);
    }
}
