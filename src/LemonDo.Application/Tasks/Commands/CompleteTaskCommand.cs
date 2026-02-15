namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to mark a task as done and move its card to the Done column.</summary>
public sealed record CompleteTaskCommand(Guid TaskId);

/// <summary>
/// Completes a task and moves its card to the Done column on the default board.
/// Coordinates both the Task (status) and Board (spatial placement) contexts.
/// </summary>
public sealed class CompleteTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(CompleteTaskCommand command, CancellationToken ct = default)
    {
        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Task", command.TaskId.ToString()));

        var completeResult = task.Complete();
        if (completeResult.IsFailure)
            return completeResult;

        // Move card to Done column on board
        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var doneColumn = board.GetDoneColumn();

        var moveResult = board.MoveCard(task.Id, doneColumn.Id, previousTaskId: null, nextTaskId: null);
        if (moveResult.IsFailure)
            return Result<DomainError>.Failure(moveResult.Error);

        await taskRepository.UpdateAsync(task, ct);
        await boardRepository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
