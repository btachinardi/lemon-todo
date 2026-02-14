namespace LemonDo.Application.Tasks.Commands;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    string? Title = null,
    string? Description = null,
    Priority? Priority = null,
    DateTimeOffset? DueDate = null,
    bool ClearDueDate = false);

public sealed class UpdateTaskCommandHandler(ITaskItemRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<Result<TaskItemDto, DomainError>> HandleAsync(UpdateTaskCommand command, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(TaskItemId.From(command.TaskId), ct);
        if (task is null)
            return Result<TaskItemDto, DomainError>.Failure(
                DomainError.NotFound("TaskItem", command.TaskId.ToString()));

        if (command.Title is not null)
        {
            var titleResult = TaskTitle.Create(command.Title);
            if (titleResult.IsFailure)
                return Result<TaskItemDto, DomainError>.Failure(titleResult.Error);

            var updateResult = task.UpdateTitle(titleResult.Value);
            if (updateResult.IsFailure)
                return Result<TaskItemDto, DomainError>.Failure(updateResult.Error);
        }

        if (command.Description is not null)
        {
            var descResult = TaskDescription.Create(command.Description);
            if (descResult.IsFailure)
                return Result<TaskItemDto, DomainError>.Failure(descResult.Error);

            var updateResult = task.UpdateDescription(descResult.Value);
            if (updateResult.IsFailure)
                return Result<TaskItemDto, DomainError>.Failure(updateResult.Error);
        }

        if (command.Priority.HasValue)
        {
            var priorityResult = task.SetPriority(command.Priority.Value);
            if (priorityResult.IsFailure)
                return Result<TaskItemDto, DomainError>.Failure(priorityResult.Error);
        }

        if (command.ClearDueDate)
        {
            var dueDateResult = task.SetDueDate(null);
            if (dueDateResult.IsFailure)
                return Result<TaskItemDto, DomainError>.Failure(dueDateResult.Error);
        }
        else if (command.DueDate.HasValue)
        {
            var dueDateResult = task.SetDueDate(command.DueDate.Value);
            if (dueDateResult.IsFailure)
                return Result<TaskItemDto, DomainError>.Failure(dueDateResult.Error);
        }

        await repository.UpdateAsync(task, ct);
        await unitOfWork.SaveChangesAsync(ct);

        return Result<TaskItemDto, DomainError>.Success(TaskItemDtoMapper.ToDto(task));
    }
}
