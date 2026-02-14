namespace LemonDo.Application.Tasks.DTOs;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

public static class TaskDtoMapper
{
    public static TaskDto ToDto(TaskEntity task) => new()
    {
        Id = task.Id.Value,
        Title = task.Title.Value,
        Description = task.Description?.Value,
        Priority = task.Priority.ToString(),
        Status = task.Status.ToString(),
        DueDate = task.DueDate,
        Tags = task.Tags.Select(t => t.Value).ToList(),
        IsArchived = task.IsArchived,
        IsDeleted = task.IsDeleted,
        CompletedAt = task.CompletedAt,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt
    };
}
