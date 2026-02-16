namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>PUT /api/tasks/{id}</c>. All fields are optional for partial updates.</summary>
public sealed record UpdateTaskRequest(
    string? Title = null,
    string? Description = null,
    string? Priority = null,
    DateTimeOffset? DueDate = null,
    bool ClearDueDate = false,
    string? SensitiveNote = null,
    bool ClearSensitiveNote = false);
