namespace LemonDo.Application.Tasks.DTOs;

using LemonDo.Domain.Tasks.Entities;

public static class TaskItemDtoMapper
{
    public static TaskItemDto ToDto(TaskItem task) => new()
    {
        Id = task.Id.Value,
        Title = task.Title.Value,
        Description = task.Description?.Value,
        Priority = task.Priority.ToString(),
        Status = task.Status.ToString(),
        DueDate = task.DueDate,
        Tags = task.Tags.Select(t => t.Value).ToList(),
        ColumnId = task.ColumnId?.Value,
        Position = task.Position,
        IsArchived = task.IsArchived,
        IsDeleted = task.IsDeleted,
        CompletedAt = task.CompletedAt,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };

    public static BoardDto ToDto(Board board) => new()
    {
        Id = board.Id.Value,
        Name = board.Name.Value,
        Columns = board.Columns.Select(c => new ColumnDto
        {
            Id = c.Id.Value,
            Name = c.Name.Value,
            Position = c.Position,
            WipLimit = c.WipLimit
        }).ToList(),
        CreatedAt = board.CreatedAt
    };
}
