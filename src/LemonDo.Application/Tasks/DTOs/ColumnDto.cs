namespace LemonDo.Application.Tasks.DTOs;

/// <summary>Read model for a board column, including its target status mapping.</summary>
public sealed record ColumnDto
{
    /// <summary>Unique identifier for the column.</summary>
    public required Guid Id { get; init; }
    /// <summary>Display name of the column.</summary>
    public required string Name { get; init; }
    /// <summary>Task status that cards in this column represent (Todo, InProgress, or Done).</summary>
    public required string TargetStatus { get; init; }
    /// <summary>Zero-based position of the column on the board (left to right ordering).</summary>
    public required int Position { get; init; }
    /// <summary>Maximum number of tasks allowed in this column for WIP limits, or null for unlimited.</summary>
    public int? MaxTasks { get; init; }
}
