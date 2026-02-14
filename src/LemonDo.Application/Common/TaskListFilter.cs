namespace LemonDo.Application.Common;

using LemonDo.Domain.Tasks.ValueObjects;

public sealed record TaskListFilter
{
    public Priority? Priority { get; init; }
    public TaskStatus? Status { get; init; }
    public string? SearchTerm { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
}
