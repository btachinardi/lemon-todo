namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Query to retrieve a single task by its ID.</summary>
public sealed record GetTaskByIdQuery(Guid TaskId);

/// <summary>Returns the task as a DTO, or a not-found error.</summary>
public sealed class GetTaskByIdQueryHandler(ITaskRepository repository)
{
    public async Task<Result<TaskDto, DomainError>> HandleAsync(GetTaskByIdQuery query, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(TaskId.From(query.TaskId), ct);
        if (task is null)
            return Result<TaskDto, DomainError>.Failure(
                DomainError.NotFound("Task", query.TaskId.ToString()));

        return Result<TaskDto, DomainError>.Success(TaskDtoMapper.ToDto(task));
    }
}
