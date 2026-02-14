namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record MoveTaskCommand(Guid TaskId, Guid ColumnId, int Position);

public sealed class MoveTaskCommandHandler(
    IBoardTaskRepository repository,
    IBoardRepository boardRepository,
    IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(MoveTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(BoardTaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("BoardTask", command.TaskId.ToString()));

        var board = await boardRepository.GetDefaultForUserAsync(UserId.Default, ct);
        if (board is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Board", "default"));

        var targetColumnId = ColumnId.From(command.ColumnId);
        var targetColumn = board.FindColumnById(targetColumnId);
        if (targetColumn is null)
            return Result<DomainError>.Failure(
                new DomainError("column.not_found", $"Column '{command.ColumnId}' not found on board."));

        var result = task.MoveTo(targetColumnId, command.Position, targetColumn.TargetStatus);
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
