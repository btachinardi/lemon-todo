namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>POST /api/tasks/{id}/move</c>. Neighbor IDs determine rank.</summary>
public sealed record MoveTaskRequest(Guid ColumnId, Guid? PreviousTaskId, Guid? NextTaskId);
