namespace LemonDo.Api.Contracts;

public sealed record MoveTaskRequest(Guid ColumnId, Guid? PreviousTaskId, Guid? NextTaskId);
