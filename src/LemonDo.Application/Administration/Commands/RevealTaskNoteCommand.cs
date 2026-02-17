namespace LemonDo.Application.Administration.Commands;

using System.Text.Json;
using LemonDo.Application.Common;
using LemonDo.Application.Identity;
using LemonDo.Domain.Administration;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Command for an admin to reveal a task's encrypted sensitive note via break-the-glass controls.
/// Requires justification, optional comments, and the admin's own password for re-authentication.
/// </summary>
public sealed record RevealTaskNoteCommand(
    Guid TaskId,
    ProtectedDataRevealReason Reason,
    string? ReasonDetails,
    string? Comments,
    ProtectedValue AdminPassword);

/// <summary>Structured audit details for task note reveal actions.</summary>
public sealed record TaskNoteRevealAuditDetails(
    string Reason,
    string? ReasonDetails,
    string? Comments,
    Guid TaskId);

/// <summary>
/// Handles <see cref="RevealTaskNoteCommand"/> with break-the-glass controls:
/// validates justification, re-authenticates the admin, returns encrypted note as RevealedField, and records audit.
/// </summary>
public sealed class RevealTaskNoteCommandHandler(
    ITaskRepository taskRepository,
    IAuthService authService,
    IAuditService auditService,
    IRequestContext requestContext)
{
    /// <summary>Reveals a task's sensitive note after validation and re-authentication.</summary>
    public async Task<Result<RevealedField, DomainError>> HandleAsync(
        RevealTaskNoteCommand command, CancellationToken ct = default)
    {
        // 1. Validate: "Other" reason requires details
        if (command.Reason == ProtectedDataRevealReason.Other
            && string.IsNullOrWhiteSpace(command.ReasonDetails))
        {
            return Result<RevealedField, DomainError>.Failure(
                DomainError.Validation("reasonDetails",
                    "Reason details are required when reason is 'Other'."));
        }

        // 2. Verify the task exists and has a sensitive note
        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<RevealedField, DomainError>.Failure(
                DomainError.NotFound("Task", command.TaskId.ToString()));

        if (task.RedactedSensitiveNote is null)
            return Result<RevealedField, DomainError>.Failure(
                DomainError.NotFound("SensitiveNote", command.TaskId.ToString()));

        // 3. Re-authenticate the acting admin
        var adminUserId = requestContext.UserId
            ?? throw new InvalidOperationException("No authenticated user in request context.");
        var passwordResult = await authService.VerifyPasswordAsync(
            adminUserId, command.AdminPassword.Value, ct);
        if (passwordResult.IsFailure)
            return Result<RevealedField, DomainError>.Failure(passwordResult.Error);

        // 4. Get encrypted note as RevealedField (decryption deferred to JSON serialization)
        var revealResult = await taskRepository.GetEncryptedSensitiveNoteAsync(
            TaskId.From(command.TaskId), ct);
        if (revealResult.IsFailure)
            return Result<RevealedField, DomainError>.Failure(revealResult.Error);

        // 5. Record structured audit entry
        var auditDetails = new TaskNoteRevealAuditDetails(
            command.Reason.ToString(),
            command.ReasonDetails,
            command.Comments,
            command.TaskId);

        await auditService.RecordAsync(
            AuditAction.SensitiveNoteRevealed,
            "Task",
            command.TaskId.ToString(),
            JsonSerializer.Serialize(auditDetails),
            cancellationToken: ct);

        return Result<RevealedField, DomainError>.Success(revealResult.Value);
    }
}
