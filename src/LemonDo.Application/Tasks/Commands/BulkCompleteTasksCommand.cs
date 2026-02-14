namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record BulkCompleteTasksCommand(IReadOnlyList<Guid> TaskIds);

public sealed class BulkCompleteTasksCommandHandler(IBoardTaskRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(BulkCompleteTasksCommand command, CancellationToken ct = default)
    {
        foreach (var taskId in command.TaskIds)
        {
            var task = await repository.GetByIdAsync(BoardTaskId.From(taskId), ct);
            if (task is null)
                return Result<DomainError>.Failure(
                    DomainError.NotFound("BoardTask", taskId.ToString()));

            var result = task.Complete();
            if (result.IsFailure)
                return result;

            await repository.UpdateAsync(task, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
        return Result<DomainError>.Success();
    }
}
