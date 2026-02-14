namespace LemonDo.Api.Contracts;

public sealed record AddColumnRequest(string Name, string TargetStatus, int? Position = null);
