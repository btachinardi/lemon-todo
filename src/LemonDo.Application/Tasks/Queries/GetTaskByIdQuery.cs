namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record GetTaskByIdQuery(Guid TaskId);

public sealed class GetTaskByIdQueryHandler(ITaskItemRepository repository)
{
    public async Task<Result<TaskItemDto, DomainError>> HandleAsync(GetTaskByIdQuery query, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(TaskItemId.From(query.TaskId), ct);
        if (task is null)
            return Result<TaskItemDto, DomainError>.Failure(
                DomainError.NotFound("TaskItem", query.TaskId.ToString()));

        return Result<TaskItemDto, DomainError>.Success(TaskItemDtoMapper.ToDto(task));
    }
}
