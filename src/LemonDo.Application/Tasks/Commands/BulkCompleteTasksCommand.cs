namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to complete multiple tasks at once, moving all their cards to Done.</summary>
public sealed record BulkCompleteTasksCommand(IReadOnlyList<Guid> TaskIds);

/// <summary>
/// Atomically completes multiple tasks and moves all their cards to the Done column.
/// Fails fast on the first error (e.g., task not found or already deleted) and rolls back all changes.
/// </summary>
public sealed class BulkCompleteTasksCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<BulkCompleteTasksCommandHandler> logger)
{
    /// <inheritdoc/>
    public async Task<Result<DomainError>> HandleAsync(BulkCompleteTasksCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Bulk completing {TaskCount} tasks", command.TaskIds.Count);

        var board = await boardRepository.GetDefaultForUserAsync(currentUser.UserId, ct);
        if (board is null)
        {
            var error = DomainError.NotFound("Board", "default");
            logger.LogWarning("Failed to bulk complete tasks: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var doneColumn = board.GetDoneColumn();

        foreach (var taskId in command.TaskIds)
        {
            var task = await taskRepository.GetByIdAsync(TaskId.From(taskId), ct);
            if (task is null)
                return Result<DomainError>.Failure(
                    DomainError.NotFound("Task", taskId.ToString()));

            var completeResult = task.Complete();
            if (completeResult.IsFailure)
                return completeResult;

            var moveResult = board.MoveCard(task.Id, doneColumn.Id, previousTaskId: null, nextTaskId: null);
            if (moveResult.IsFailure)
                return Result<DomainError>.Failure(moveResult.Error);

            await taskRepository.UpdateAsync(task, ct);
        }

        await boardRepository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Bulk completed {TaskCount} tasks successfully", command.TaskIds.Count);
        return Result<DomainError>.Success();
    }
}
