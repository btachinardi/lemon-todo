namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

public sealed record MoveTaskCommand(Guid TaskId, Guid ColumnId, int Position);

/// <summary>
/// Moves a task card to a different column/position on the board, then syncs the task's status
/// to match the target column's <see cref="LemonDo.Domain.Boards.Entities.Column.TargetStatus"/>.
/// </summary>
public sealed class MoveTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(MoveTaskCommand command, CancellationToken ct = default)
    {
        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Task", command.TaskId.ToString()));

        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var targetColumnId = ColumnId.From(command.ColumnId);

        var moveResult = board.MoveCard(task.Id, targetColumnId, command.Position);
        if (moveResult.IsFailure)
            return Result<DomainError>.Failure(moveResult.Error);

        // Sync task status to match the target column
        var statusResult = task.SetStatus(moveResult.Value);
        if (statusResult.IsFailure)
            return statusResult;

        await taskRepository.UpdateAsync(task, ct);
        await boardRepository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
