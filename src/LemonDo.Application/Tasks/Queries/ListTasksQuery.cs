namespace LemonDo.Application.Tasks.Queries;

using LemonDo.Application.Common;
using LemonDo.Application.Tasks.DTOs;
using LemonDo.Domain.Common;
using LemonDo.Domain.Identity.ValueObjects;
using LemonDo.Domain.Tasks.Repositories;

public sealed record ListTasksQuery(TaskListFilter Filter);

public sealed class ListTasksQueryHandler(IBoardTaskRepository repository)
{
    public async Task<PagedResult<BoardTaskDto>> HandleAsync(ListTasksQuery query, CancellationToken ct = default)
    {
        var filter = query.Filter;
        var pagedResult = await repository.ListAsync(
            UserId.Default,
            filter.ColumnId,
            filter.Priority,
            filter.Status,
            filter.SearchTerm,
            filter.Page,
            filter.PageSize,
            ct);

        var dtos = pagedResult.Items.Select(BoardTaskDtoMapper.ToDto).ToList();
        return new PagedResult<BoardTaskDto>(dtos, pagedResult.TotalCount, pagedResult.Page, pagedResult.PageSize);
    }
}
