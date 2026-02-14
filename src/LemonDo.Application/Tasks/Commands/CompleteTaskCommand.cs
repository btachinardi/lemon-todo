namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record CompleteTaskCommand(Guid TaskId);

public sealed class CompleteTaskCommandHandler(
    IBoardTaskRepository repository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(CompleteTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(BoardTaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("BoardTask", command.TaskId.ToString()));

        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var doneColumn = board.GetDoneColumn();
        var existingTasks = await repository.GetByColumnAsync(doneColumn.Id, ct);

        var result = task.MoveTo(doneColumn.Id, existingTasks.Count, BoardTaskStatus.Done);
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
