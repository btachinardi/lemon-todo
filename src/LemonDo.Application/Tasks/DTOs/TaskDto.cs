namespace LemonDo.Application.Tasks.DTOs;

/// <summary>Read model for a task, flattening value objects to primitive types for API responses.</summary>
public sealed record TaskDto
{
    /// <summary>Unique identifier for the task.</summary>
    public required Guid Id { get; init; }
    /// <summary>Title or summary of the task.</summary>
    public required string Title { get; init; }
    /// <summary>Detailed description or notes for the task.</summary>
    public string? Description { get; init; }
    /// <summary>Priority level (None, Low, Medium, High).</summary>
    public required string Priority { get; init; }
    /// <summary>Current lifecycle status (Todo, InProgress, Done).</summary>
    public required string Status { get; init; }
    /// <summary>Optional deadline for task completion.</summary>
    public DateTimeOffset? DueDate { get; init; }
    /// <summary>Collection of tags for categorizing or labeling the task.</summary>
    public required IReadOnlyList<string> Tags { get; init; }
    /// <summary>Indicates if the task is archived (hidden from active views but not deleted).</summary>
    public required bool IsArchived { get; init; }
    /// <summary>Indicates if the task has been soft-deleted.</summary>
    public required bool IsDeleted { get; init; }
    /// <summary>Timestamp when the task was marked as Done, or null if not completed.</summary>
    public DateTimeOffset? CompletedAt { get; init; }
    /// <summary>Timestamp when the task was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }
    /// <summary>Timestamp when the task was last modified.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }
}
