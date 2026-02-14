namespace LemonDo.Api.Contracts;

public sealed record AddColumnRequest(string Name, int? Position = null);
