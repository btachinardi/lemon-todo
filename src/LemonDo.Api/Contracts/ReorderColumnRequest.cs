namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>POST /api/boards/{id}/columns/reorder</c>.</summary>
public sealed record ReorderColumnRequest(Guid ColumnId, int NewPosition);
