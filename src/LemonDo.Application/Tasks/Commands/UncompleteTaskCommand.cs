namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to revert a completed task back to Todo and move its card to the initial column.</summary>
public sealed record UncompleteTaskCommand(Guid TaskId);

/// <summary>
/// Reverts a completed task back to Todo status and moves its card to the initial (Todo) column.
/// Also clears <c>CompletedAt</c> and resets <c>IsArchived</c> via the domain entity.
/// </summary>
public sealed class UncompleteTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<UncompleteTaskCommandHandler> logger)
{
    /// <inheritdoc/>
    public async Task<Result<DomainError>> HandleAsync(UncompleteTaskCommand command, CancellationToken ct = default)
    {
        logger.LogInformation("Uncompleting task {TaskId}", command.TaskId);

        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
        {
            var error = DomainError.NotFound("Task", command.TaskId.ToString());
            logger.LogWarning("Failed to uncomplete task: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var uncompleteResult = task.Uncomplete();
        if (uncompleteResult.IsFailure)
            return uncompleteResult;

        // Move card to initial (Todo) column on board
        var board = await boardRepository.GetDefaultForUserAsync(currentUser.UserId, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var todoColumn = board.GetInitialColumn();

        var moveResult = board.MoveCard(task.Id, todoColumn.Id, previousTaskId: null, nextTaskId: null);
        if (moveResult.IsFailure)
            return Result<DomainError>.Failure(moveResult.Error);

        await taskRepository.UpdateAsync(task, ct: ct);
        await boardRepository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        logger.LogInformation("Task {TaskId} uncompleted successfully", command.TaskId);
        return Result<DomainError>.Success();
    }
}
