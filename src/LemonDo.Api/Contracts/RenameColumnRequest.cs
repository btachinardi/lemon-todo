namespace LemonDo.Api.Contracts;

/// <summary>Request body for <c>PUT /api/boards/{id}/columns/{colId}</c>.</summary>
public sealed record RenameColumnRequest(string Name);
