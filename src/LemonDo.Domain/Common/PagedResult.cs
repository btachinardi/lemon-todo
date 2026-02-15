namespace LemonDo.Domain.Common;

/// <summary>
/// Represents a paginated result set.
/// </summary>
/// <typeparam name="T">The type of items in the result set.</typeparam>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    /// <summary>Total number of pages available based on <see cref="TotalCount"/> and <see cref="PageSize"/>.</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>True if there are more pages after the current <see cref="Page"/>.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>True if there are pages before the current <see cref="Page"/>. False when on the first page.</summary>
    public bool HasPreviousPage => Page > 1;
}
