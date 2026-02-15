namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>POST /api/tasks/{id}/tags</c>.</summary>
public sealed record AddTagRequest(string Tag);
