namespace LemonDo.Api.Contracts;

public sealed record ReorderColumnRequest(Guid ColumnId, int NewPosition);
