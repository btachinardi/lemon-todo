namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>POST /api/tasks</c>.</summary>
public sealed record CreateTaskRequest(
    string Title,
    string? Description = null,
    string Priority = "None",
    DateTimeOffset? DueDate = null,
    List<string>? Tags = null,
    string? SensitiveNote = null);
