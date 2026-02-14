namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record CompleteTaskCommand(Guid TaskId);

public sealed class CompleteTaskCommandHandler(ITaskItemRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(CompleteTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(TaskItemId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("TaskItem", command.TaskId.ToString()));

        var result = task.Complete();
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
