namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Boards.Repositories;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

public sealed record UncompleteTaskCommand(Guid TaskId);

public sealed class UncompleteTaskCommandHandler(
    ITaskRepository taskRepository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(UncompleteTaskCommand command, CancellationToken ct = default)
    {
        var task = await taskRepository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Task", command.TaskId.ToString()));

        var uncompleteResult = task.Uncomplete();
        if (uncompleteResult.IsFailure)
            return uncompleteResult;

        // Move card to initial (Todo) column on board
        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var todoColumn = board.GetInitialColumn();
        var position = board.GetCardCountInColumn(todoColumn.Id);

        var moveResult = board.MoveCard(task.Id, todoColumn.Id, position);
        if (moveResult.IsFailure)
            return Result<DomainError>.Failure(moveResult.Error);

        await taskRepository.UpdateAsync(task, ct);
        await boardRepository.UpdateAsync(board, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
