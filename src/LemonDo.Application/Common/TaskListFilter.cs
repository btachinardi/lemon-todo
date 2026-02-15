namespace LemonDo.Application.Common;

using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>Filter criteria for the task list query. All fields are optional and combined with AND logic.</summary>
public sealed record TaskListFilter
{
    /// <summary>Optional priority filter. When set, only tasks with matching priority are returned.</summary>
    public Priority? Priority { get; init; }
    /// <summary>Optional status filter. When set, only tasks with matching status are returned.</summary>
    public TaskStatus? Status { get; init; }
    /// <summary>Optional search term applied to task title and description using case-insensitive contains matching.</summary>
    public string? SearchTerm { get; init; }
    /// <summary>Optional tag filter. When set, only tasks that contain this tag are returned.</summary>
    public string? Tag { get; init; }
    /// <summary>Page number for pagination (1-based index).</summary>
    public int Page { get; init; } = 1;
    /// <summary>Number of items per page.</summary>
    public int PageSize { get; init; } = 50;
}
