namespace LemonDo.Application.Tasks.DTOs;

using TaskEntity = LemonDo.Domain.Tasks.Entities.Task;

/// <summary>Maps <see cref="LemonDo.Domain.Tasks.Entities.Task"/> aggregates to <see cref="TaskDto"/> read models.</summary>
public static class TaskDtoMapper
{
    /// <summary>Converts a Task entity to a DTO, flattening value objects to primitive types.</summary>
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
