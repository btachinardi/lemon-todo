namespace LemonDo.Application.Tasks.DTOs;

/// <summary>Read model for a board, including its columns and active task cards.</summary>
public sealed record BoardDto
{
    /// <summary>Unique identifier for the board.</summary>
    public required Guid Id { get; init; }
    /// <summary>Display name of the board.</summary>
    public required string Name { get; init; }
    /// <summary>Ordered list of columns on the board.</summary>
    public required IReadOnlyList<ColumnDto> Columns { get; init; }
    /// <summary>Task cards positioned on the board, optionally filtered to exclude deleted/archived tasks.</summary>
    public IReadOnlyList<TaskCardDto>? Cards { get; init; }
    /// <summary>Timestamp when the board was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
}
