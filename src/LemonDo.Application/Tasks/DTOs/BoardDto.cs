namespace LemonDo.Application.Tasks.DTOs;

public sealed record BoardDto
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required IReadOnlyList<ColumnDto> Columns { get; init; }
    public IReadOnlyList<TaskCardDto>? Cards { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
