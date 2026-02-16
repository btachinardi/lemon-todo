namespace LemonDo.Api.Contracts;

/// <summary>Adds a tag to a task with automatic normalization.</summary>
/// <remarks>
/// Tags are trimmed, converted to lowercase, and must not exceed 50 characters.
/// Duplicate tags are ignored.
/// </remarks>
public sealed record AddTagRequest(string Tag);
