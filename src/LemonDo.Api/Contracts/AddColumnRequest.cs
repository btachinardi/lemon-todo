namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>POST /api/boards/{id}/columns</c>.</summary>
public sealed record AddColumnRequest(string Name, string TargetStatus, int? Position = null);
