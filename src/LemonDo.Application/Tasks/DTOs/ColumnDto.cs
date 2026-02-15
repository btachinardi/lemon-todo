namespace LemonDo.Application.Tasks.DTOs;

/// <summary>Read model for a board column, including its target status mapping.</summary>
public sealed record ColumnDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required string TargetStatus { get; init; }
    public required int Position { get; init; }
    public int? MaxTasks { get; init; }
}
