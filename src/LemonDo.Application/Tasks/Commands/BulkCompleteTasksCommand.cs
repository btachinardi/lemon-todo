namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record BulkCompleteTasksCommand(IReadOnlyList<Guid> TaskIds);

public sealed class BulkCompleteTasksCommandHandler(
    IBoardTaskRepository repository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(BulkCompleteTasksCommand command, CancellationToken ct = default)
    {
        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var doneColumn = board.GetDoneColumn();
        var existingTasks = await repository.GetByColumnAsync(doneColumn.Id, ct);
        var nextPosition = existingTasks.Count;

        foreach (var taskId in command.TaskIds)
        {
            var task = await repository.GetByIdAsync(BoardTaskId.From(taskId), ct);
            if (task is null)
                return Result<DomainError>.Failure(
                    DomainError.NotFound("BoardTask", taskId.ToString()));

            var result = task.MoveTo(doneColumn.Id, nextPosition, BoardTaskStatus.Done);
            if (result.IsFailure)
                return result;

            nextPosition++;
            await repository.UpdateAsync(task, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<DomainError>.Success();
    }
}
