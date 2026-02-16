namespace LemonDo.Application.Tasks.Commands;

using System.Diagnostics;
using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to soft-delete a task and remove its card from the board.</summary>
public sealed record DeleteTaskCommand(Guid TaskId);

/// <summary>Soft-deletes the task and removes its board card in a single unit of work.</summary>
public sealed class DeleteTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<DeleteTaskCommandHandler> logger,
    ApplicationMetrics metrics)
{
    /// <summary>Loads the task, soft-deletes it (marks as deleted), removes its card from the board, and persists both changes atomically.</summary>
    public async Task<Result<DomainError>> HandleAsync(DeleteTaskCommand command, CancellationToken ct = default)
    {
        using var activity = ApplicationActivitySource.Source.StartActivity("DeleteTask");
        activity?.SetTag("task.id", command.TaskId.ToString());

        logger.LogInformation("Deleting task {TaskId}", command.TaskId);

        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
        {
            var error = DomainError.NotFound("Task", command.TaskId.ToString());
            logger.LogWarning("Failed to delete task: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var result = task.Delete();
        if (result.IsFailure)
            return result;

        // Remove the card from the board
        var board = await boardRepository.GetDefaultForUserAsync(currentUser.UserId, ct);
        if (board is not null)
        {
            board.RemoveCard(task.Id);
            await boardRepository.UpdateAsync(board, ct);
        }

        await taskRepository.UpdateAsync(task, ct: ct);
        await unitOfWork.SaveChangesAsync(ct);
        metrics.TaskDeleted();

        logger.LogInformation("Task {TaskId} deleted successfully", command.TaskId);
        return Result<DomainError>.Success();
    }
}
