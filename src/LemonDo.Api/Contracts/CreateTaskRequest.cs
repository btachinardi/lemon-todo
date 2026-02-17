namespace LemonDo.Api.Contracts;

using LemonDo.Domain.Common;

/// <summary>Creates a new task with optional metadata.</summary>
/// <remarks>
/// Title is required (1-500 characters).
/// Description, DueDate, and SensitiveNote are optional.
/// Priority defaults to "None" if not specified (valid values: "None", "Low", "Medium", "High").
/// Tags are automatically normalized (trimmed and lowercased).
/// SensitiveNote is validated and encrypted at JSON deserialization.
/// </remarks>
public sealed record CreateTaskRequest(
    string Title,
    string? Description = null,
    string Priority = "None",
    DateTimeOffset? DueDate = null,
    List<string>? Tags = null,
    EncryptedField? SensitiveNote = null);
