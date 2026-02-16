namespace LemonDo.Api.Contracts;

/// <summary>Adds a new column to a board with task status mapping.</summary>
/// <remarks>
/// TargetStatus maps task status to this column (e.g., "Todo", "InProgress", "Done").
/// Position is 0-based; null appends to the end of the column list.
/// Column names must be 1-100 characters after trimming.
/// </remarks>
public sealed record AddColumnRequest(string Name, string TargetStatus, int? Position = null);
