namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to partially update a task's scalar fields (title, description, priority, due date, sensitive note).</summary>
/// <remarks>
/// Only provided fields are modified. To explicitly remove the due date, set ClearDueDate to true (the DueDate value is ignored when true).
/// Same for ClearSensitiveNote: set to true to remove the sensitive note (the SensitiveNote value is ignored when true).
/// SensitiveNote arrives pre-validated and pre-encrypted from the JSON converter.
/// </remarks>
public sealed record UpdateTaskCommand(
    Guid TaskId,
    string? Title = null,
    string? Description = null,
    Priority? Priority = null,
    DateTimeOffset? DueDate = null,
    bool ClearDueDate = false,
    EncryptedField? SensitiveNote = null,
    bool ClearSensitiveNote = false);

/// <summary>Applies partial updates to a task. Only non-null fields are changed.</summary>
public sealed class UpdateTaskCommandHandler(ITaskRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<UpdateTaskCommandHandler> logger)
{
    /// <summary>Loads the task, applies partial updates to provided fields, validates all changes, and persists the modified task.</summary>
    public async Task<Result<TaskDto, DomainError>> HandleAsync(UpdateTaskCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Updating task {TaskId}", command.TaskId);

        var task = await repository.GetByIdAsync(TaskId.From(command.TaskId), currentUser.UserId, ct);
        if (task is null)
        {
            var error = DomainError.NotFound("Task", command.TaskId.ToString());
            logger.LogWarning("Failed to update task: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<TaskDto, DomainError>.Failure(error);
        }

        if (command.Title is not null)
        {
            var titleResult = TaskTitle.Create(command.Title);
            if (titleResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(titleResult.Error);

            var updateResult = task.UpdateTitle(titleResult.Value);
            if (updateResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(updateResult.Error);
        }

        if (command.Description is not null)
        {
            var descResult = TaskDescription.Create(command.Description);
            if (descResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(descResult.Error);

            var updateResult = task.UpdateDescription(descResult.Value);
            if (updateResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(updateResult.Error);
        }

        if (command.Priority.HasValue)
        {
            var priorityResult = task.SetPriority(command.Priority.Value);
            if (priorityResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(priorityResult.Error);
        }

        if (command.ClearDueDate)
        {
            var dueDateResult = task.SetDueDate(null);
            if (dueDateResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(dueDateResult.Error);
        }
        else if (command.DueDate.HasValue)
        {
            var dueDateResult = task.SetDueDate(command.DueDate.Value);
            if (dueDateResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(dueDateResult.Error);
        }

        // SensitiveNote is already validated + encrypted by the JSON converter.
        SensitiveNote? sensitiveNote = null;
        var clearNote = command.ClearSensitiveNote;
        if (clearNote)
        {
            var noteResult = task.UpdateSensitiveNote(null);
            if (noteResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(noteResult.Error);
        }
        else if (command.SensitiveNote is not null)
        {
            sensitiveNote = SensitiveNote.Reconstruct(command.SensitiveNote.Redacted);
            var updateNoteResult = task.UpdateSensitiveNote(sensitiveNote);
            if (updateNoteResult.IsFailure)
                return Result<TaskDto, DomainError>.Failure(updateNoteResult.Error);
        }

        await repository.UpdateAsync(task, command.SensitiveNote, clearNote, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Task {TaskId} updated successfully", command.TaskId);
        return Result<TaskDto, DomainError>.Success(TaskDtoMapper.ToDto(task));
    }
}
