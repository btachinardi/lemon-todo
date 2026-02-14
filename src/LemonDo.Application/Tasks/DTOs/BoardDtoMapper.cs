namespace LemonDo.Application.Tasks.DTOs;

using LemonDo.Domain.Boards.Entities;

public static class BoardDtoMapper
{
    public static BoardDto ToDto(Board board) => new()
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
        Cards = board.Cards.Select(c => new TaskCardDto
        {
            TaskId = c.TaskId.Value,
            ColumnId = c.ColumnId.Value,
            Position = c.Position
        }).ToList(),
        CreatedAt = board.CreatedAt
    };
}
