namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>POST /api/tasks/{id}/view-note</c>. Requires password re-authentication.</summary>
public sealed record ViewTaskNoteRequest(string Password);
