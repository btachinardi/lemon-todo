namespace LemonDo.Application.Tasks.DTOs;

using LemonDo.Domain.Boards.Entities;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Maps <see cref="Board"/> aggregates to <see cref="BoardDto"/> read models.
/// Optionally filters cards to only include active (non-deleted, non-archived) tasks.
/// </summary>
public static class BoardDtoMapper
{
    /// <summary>Converts a Board entity to a DTO, optionally filtering cards to only include tasks in the provided set of active IDs.</summary>
    public static BoardDto ToDto(Board board, HashSet<TaskId>? activeTaskIds = null) => new()
    {
        Id = board.Id.Value,
        Name = board.Name.Value,
        Columns = board.Columns.OrderBy(c => c.Position).Select(c => new ColumnDto
        {
            Id = c.Id.Value,
            Name = c.Name.Value,
            TargetStatus = c.TargetStatus.ToString(),
            Position = c.Position,
            MaxTasks = c.MaxTasks
        }).ToList(),
        Cards = board.Cards
            .Where(c => activeTaskIds is null || activeTaskIds.Contains(c.TaskId))
            .Select(c => new TaskCardDto
            {
                TaskId = c.TaskId.Value,
                ColumnId = c.ColumnId.Value,
                Rank = c.Rank
            }).ToList(),
        CreatedAt = board.CreatedAt
    };
}
