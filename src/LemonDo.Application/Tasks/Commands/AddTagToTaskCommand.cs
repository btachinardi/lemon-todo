namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

public sealed record AddTagToTaskCommand(Guid TaskId, string Tag);

public sealed class AddTagToTaskCommandHandler(ITaskRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<DomainError>> HandleAsync(AddTagToTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Task", command.TaskId.ToString()));

        var tagResult = Tag.Create(command.Tag);
        if (tagResult.IsFailure)
            return Result<DomainError>.Failure(tagResult.Error);

        var result = task.AddTag(tagResult.Value);
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
