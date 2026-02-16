namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Categorizes a task with an additional tag. The tag is normalized (trimmed, lowercased).</summary>
public sealed record AddTagToTaskCommand(Guid TaskId, string Tag);

/// <summary>Validates the tag and delegates to <see cref="LemonDo.Domain.Tasks.Entities.Task.AddTag"/>.</summary>
public sealed class AddTagToTaskCommandHandler(ITaskRepository repository, IUnitOfWork unitOfWork, ILogger<AddTagToTaskCommandHandler> logger)
{
    /// <summary>Validates the tag, loads the task, adds the normalized tag, and persists the change.</summary>
    public async Task<Result<DomainError>> HandleAsync(AddTagToTaskCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Adding tag {Tag} to task {TaskId}", command.Tag, command.TaskId);

        var task = await repository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
        {
            var error = DomainError.NotFound("Task", command.TaskId.ToString());
            logger.LogWarning("Failed to add tag to task: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var tagResult = Tag.Create(command.Tag);
        if (tagResult.IsFailure)
            return Result<DomainError>.Failure(tagResult.Error);

        var result = task.AddTag(tagResult.Value);
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct: ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Tag {Tag} added to task {TaskId} successfully", command.Tag, command.TaskId);
        return Result<DomainError>.Success();
    }
}
