namespace LemonDo.Application.Tasks.DTOs;

/// <summary>Read model for a task's spatial placement on a board (column + rank).</summary>
public sealed record TaskCardDto
{
    public required Guid TaskId { get; init; }
    public required Guid ColumnId { get; init; }
    public required decimal Rank { get; init; }
}
