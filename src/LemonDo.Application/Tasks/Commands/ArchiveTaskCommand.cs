namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Hides a completed task from active board views without deleting it.</summary>
public sealed record ArchiveTaskCommand(Guid TaskId);

/// <summary>Archives the task via <see cref="LemonDo.Domain.Tasks.Entities.Task.Archive"/>.</summary>
public sealed class ArchiveTaskCommandHandler(ITaskRepository repository, IUnitOfWork unitOfWork, ILogger<ArchiveTaskCommandHandler> logger)
{
    /// <summary>Loads the task, marks it as archived (enforces completion precondition), and persists the change.</summary>
    public async Task<Result<DomainError>> HandleAsync(ArchiveTaskCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Archiving task {TaskId}", command.TaskId);

        var task = await repository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
        {
            var error = DomainError.NotFound("Task", command.TaskId.ToString());
            logger.LogWarning("Failed to archive task: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var result = task.Archive();
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct: ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Task {TaskId} archived successfully", command.TaskId);
        return Result<DomainError>.Success();
    }
}
