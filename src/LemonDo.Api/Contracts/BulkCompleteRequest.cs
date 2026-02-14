namespace LemonDo.Api.Contracts;

public sealed record BulkCompleteRequest(List<Guid> TaskIds);
