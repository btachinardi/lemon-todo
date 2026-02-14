namespace LemonDo.Api.Contracts;

public sealed record UpdateTaskRequest(
    string? Title = null,
    string? Description = null,
    string? Priority = null,
    DateTimeOffset? DueDate = null,
    bool ClearDueDate = false);
