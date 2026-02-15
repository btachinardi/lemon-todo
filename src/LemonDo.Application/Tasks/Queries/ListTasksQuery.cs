namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Query to retrieve a paginated, filtered list of tasks.</summary>
public sealed record ListTasksQuery(TaskListFilter Filter);

/// <summary>Returns a paginated result of task DTOs matching the filter criteria.</summary>
public sealed class ListTasksQueryHandler(ITaskRepository repository)
{
    public async Task<PagedResult<TaskDto>> HandleAsync(ListTasksQuery query, CancellationToken ct = default)
    {
        var filter = query.Filter;
        var pagedResult = await repository.ListAsync(
            UserId.Default,
            filter.Priority,
            filter.Status,
            filter.SearchTerm,
            filter.Page,
            filter.PageSize,
            ct);

        var dtos = pagedResult.Items.Select(TaskDtoMapper.ToDto).ToList();
        return new PagedResult<TaskDto>(dtos, pagedResult.TotalCount, pagedResult.Page, pagedResult.PageSize);
    }
}
