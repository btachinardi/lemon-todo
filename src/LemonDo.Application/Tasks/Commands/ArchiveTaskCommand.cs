namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Command to archive a task, hiding it from active views.</summary>
public sealed record ArchiveTaskCommand(Guid TaskId);

/// <summary>Archives the task via <see cref="LemonDo.Domain.Tasks.Entities.Task.Archive"/>.</summary>
public sealed class ArchiveTaskCommandHandler(ITaskRepository repository, IUnitOfWork unitOfWork)
{
    /// <inheritdoc/>
    public async Task<Result<DomainError>> HandleAsync(ArchiveTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(TaskId.From(command.TaskId), ct);
        if (task is null)
            return Result<DomainError>.Failure(
                DomainError.NotFound("Task", command.TaskId.ToString()));

        var result = task.Archive();
        if (result.IsFailure)
            return result;

        await repository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<DomainError>.Success();
    }
}
