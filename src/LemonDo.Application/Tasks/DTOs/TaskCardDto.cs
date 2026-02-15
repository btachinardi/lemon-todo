namespace LemonDo.Application.Tasks.DTOs;

/// <summary>Read model for a task's spatial placement on a board (column + rank).</summary>
public sealed record TaskCardDto
{
    /// <summary>Unique identifier of the task this card represents.</summary>
    public required Guid TaskId { get; init; }
    /// <summary>Unique identifier of the column where this card is placed.</summary>
    public required Guid ColumnId { get; init; }
    /// <summary>Fractional rank for ordering cards within the column. Higher values appear later in the list.</summary>
    public required decimal Rank { get; init; }
}
