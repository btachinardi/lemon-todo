namespace LemonDo.Api.Contracts;

public sealed record MoveTaskRequest(Guid ColumnId, int Position);
