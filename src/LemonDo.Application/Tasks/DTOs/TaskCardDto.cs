namespace LemonDo.Application.Tasks.DTOs;

public sealed record TaskCardDto
{
    public required Guid TaskId { get; init; }
    public required Guid ColumnId { get; init; }
    public required int Position { get; init; }
}
