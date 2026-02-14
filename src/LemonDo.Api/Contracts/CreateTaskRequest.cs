namespace LemonDo.Api.Contracts;

public sealed record CreateTaskRequest(
    string Title,
    string? Description = null,
    string Priority = "None",
    DateTimeOffset? DueDate = null,
    List<string>? Tags = null);
