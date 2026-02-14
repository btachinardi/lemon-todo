namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record MoveTaskCommand(Guid TaskId, Guid ColumnId, int Position);

public sealed class MoveTaskCommandHandler(IBoardTaskRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(MoveTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(BoardTaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("BoardTask", command.TaskId.ToString()));

        var result = task.MoveTo(ColumnId.From(command.ColumnId), command.Position);
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
