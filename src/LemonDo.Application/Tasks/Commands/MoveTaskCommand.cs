namespace LemonDo.Application.Tasks.Commands;

using System.Diagnostics;
using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to move a task card to a column at a position between two neighbor cards.</summary>
public sealed record MoveTaskCommand(Guid TaskId, Guid ColumnId, Guid? PreviousTaskId, Guid? NextTaskId);

/// <summary>
/// Moves a task card to a different column/position on the board using neighbor-based ranking,
/// then syncs the task's status to match the target column's <see cref="LemonDo.Domain.Boards.Entities.Column.TargetStatus"/>.
/// </summary>
public sealed class MoveTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserService currentUser,
    ILogger<MoveTaskCommandHandler> logger,
    ApplicationMetrics metrics)
{
    /// <inheritdoc/>
    public async Task<Result<DomainError>> HandleAsync(MoveTaskCommand command, CancellationToken ct = default)
    {
        using var activity = ApplicationActivitySource.Source.StartActivity("MoveTask");
        activity?.SetTag("task.id", command.TaskId.ToString());
        activity?.SetTag("task.target_column", command.ColumnId.ToString());

        logger.LogInformation("Moving task {TaskId} to column {ColumnId}", command.TaskId, command.ColumnId);

        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
        {
            var error = DomainError.NotFound("Task", command.TaskId.ToString());
            logger.LogWarning("Failed to move task: {ErrorCode} - {ErrorMessage}", error.Code, error.Message);
            return Result<DomainError>.Failure(error);
        }

        var board = await boardRepository.GetDefaultForUserAsync(currentUser.UserId, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var targetColumnId = ColumnId.From(command.ColumnId);
        var previousTaskId = command.PreviousTaskId is not null ? TaskId.From(command.PreviousTaskId.Value) : null;
        var nextTaskId = command.NextTaskId is not null ? TaskId.From(command.NextTaskId.Value) : null;

        var moveResult = board.MoveCard(task.Id, targetColumnId, previousTaskId, nextTaskId);
        if (moveResult.IsFailure)
            return Result<DomainError>.Failure(moveResult.Error);

        // Sync task status to match the target column
        var statusResult = task.SetStatus(moveResult.Value);
        if (statusResult.IsFailure)
            return statusResult;

        await taskRepository.UpdateAsync(task, ct: ct);
        await boardRepository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);
        metrics.TaskMoved();

        logger.LogInformation("Task {TaskId} moved to column {ColumnId} successfully", command.TaskId, command.ColumnId);
        return Result<DomainError>.Success();
    }
}
