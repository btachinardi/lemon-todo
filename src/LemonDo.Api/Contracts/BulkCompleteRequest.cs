namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>POST /api/tasks/bulk/complete</c>.</summary>
public sealed record BulkCompleteRequest(List<Guid> TaskIds);
