namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to remove a tag from an existing task.</summary>
public sealed record RemoveTagFromTaskCommand(Guid TaskId, string Tag);

/// <summary>Validates the tag and delegates to <see cref="LemonDo.Domain.Tasks.Entities.Task.RemoveTag"/>.</summary>
public sealed class RemoveTagFromTaskCommandHandler(ITaskRepository repository, IUnitOfWork unitOfWork, ICurrentUserService currentUser, ILogger<RemoveTagFromTaskCommandHandler> logger)
{
    /// <summary>Validates the tag, loads the task, removes the tag, and persists the change.</summary>
    public async Task<Result<DomainError>> HandleAsync(RemoveTagFromTaskCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Removing tag {Tag} from task {TaskId}", command.Tag, command.TaskId);

        var task = await repository.GetByIdAsync(TaskId.From(command.TaskId), currentUser.UserId, ct);
        if (task is null)
        {
            var error = DomainError.NotFound("Task", command.TaskId.ToString());
            logger.LogWarning("Failed to remove tag from task: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var tagResult = Tag.Create(command.Tag);
        if (tagResult.IsFailure)
            return Result<DomainError>.Failure(tagResult.Error);

        var result = task.RemoveTag(tagResult.Value);
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct: ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Tag {Tag} removed from task {TaskId} successfully", command.Tag, command.TaskId);
        return Result<DomainError>.Success();
    }
}
