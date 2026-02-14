namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed record GetTaskByIdQuery(Guid TaskId);

public sealed class GetTaskByIdQueryHandler(IBoardTaskRepository repository)
{
    public async Task<Result<BoardTaskDto, DomainError>> HandleAsync(GetTaskByIdQuery query, CancellationToken ct = default)
    {
        var task = await repository.GetByIdAsync(BoardTaskId.From(query.TaskId), ct);
        if (task is null)
            return Result<BoardTaskDto, DomainError>.Failure(
                DomainError.NotFound("BoardTask", query.TaskId.ToString()));

        return Result<BoardTaskDto, DomainError>.Success(BoardTaskDtoMapper.ToDto(task));
    }
}
