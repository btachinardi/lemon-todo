namespace LemonDo.Application.Tasks.DTOs;

/// <summary>Read model for a task, flattening value objects to primitive types for API responses.</summary>
public sealed record TaskDto
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Priority { get; init; }
    public required string Status { get; init; }
    public DateTimeOffset? DueDate { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required bool IsArchived { get; init; }
    public required bool IsDeleted { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
