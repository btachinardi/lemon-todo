namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.Repositories;
using LemonDo.Domain.Tasks.ValueObjects;
using Microsoft.Extensions.Logging;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Query to retrieve a single task by its ID.</summary>
public sealed record GetTaskByIdQuery(Guid TaskId);

/// <summary>Returns the task as a DTO, or a not-found error.</summary>
public sealed class GetTaskByIdQueryHandler(ITaskRepository repository, ICurrentUserService currentUser, ILogger<GetTaskByIdQueryHandler> logger)
{
    /// <summary>Loads the task and maps it to a DTO, or returns a not-found error if the task doesn't exist or isn't owned by the current user.</summary>
    public async Task<Result<TaskDto, DomainError>> HandleAsync(GetTaskByIdQuery query, CancellationToken ct = default)
    {
        logger.LogInformation("Fetching task {TaskId}", query.TaskId);

        var task = await repository.GetByIdAsync(TaskId.From(query.TaskId), currentUser.UserId, ct);
        if (task is null)
        {
            logger.LogWarning("Task {TaskId} not found", query.TaskId);
            return Result<TaskDto, DomainError>.Failure(
                DomainError.NotFound("Task", query.TaskId.ToString()));
        }

        logger.LogInformation("Task {TaskId} fetched successfully", query.TaskId);
        return Result<TaskDto, DomainError>.Success(TaskDtoMapper.ToDto(task));
    }
}
