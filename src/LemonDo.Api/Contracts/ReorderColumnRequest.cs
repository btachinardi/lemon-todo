namespace LemonDo.Api.Contracts;

/// <summary>Reorders a column within the board's column list.</summary>
/// <remarks>
/// NewPosition is a 0-based index within the board's columns.
/// </remarks>
public sealed record ReorderColumnRequest(Guid ColumnId, int NewPosition);
