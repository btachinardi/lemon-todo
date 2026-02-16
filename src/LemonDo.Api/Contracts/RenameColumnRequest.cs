namespace LemonDo.Api.Contracts;

/// <summary>Renames a board column.</summary>
/// <remarks>
/// Column name must be 1-100 characters after trimming.
/// </remarks>
public sealed record RenameColumnRequest(string Name);
