namespace LemonDo.Application.Tasks.DTOs;

public sealed record ColumnDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required int Position { get; init; }
    public int? WipLimit { get; init; }
}
