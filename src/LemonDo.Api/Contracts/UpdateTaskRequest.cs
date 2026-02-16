namespace LemonDo.Api.Contracts;

/// <summary>Updates a task with partial field modifications.</summary>
/// <remarks>
/// All fields are optional; only provided fields are modified.
/// ClearDueDate=true removes the due date (DueDate value is ignored when true).
/// ClearSensitiveNote=true removes the sensitive note (SensitiveNote value is ignored when true).
/// </remarks>
public sealed record UpdateTaskRequest(
    string? Title = null,
    string? Description = null,
    string? Priority = null,
    DateTimeOffset? DueDate = null,
    bool ClearDueDate = false,
    string? SensitiveNote = null,
    bool ClearSensitiveNote = false);
