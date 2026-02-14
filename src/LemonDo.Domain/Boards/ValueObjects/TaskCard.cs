namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

public sealed class TaskCard
{
    public TaskId TaskId { get; }
    public ColumnId ColumnId { get; internal set; }
    public int Position { get; internal set; }

    internal TaskCard(TaskId taskId, ColumnId columnId, int position)
    {
        TaskId = taskId;
        ColumnId = columnId;
        Position = position;
    }

    // EF Core constructor
    private TaskCard() { TaskId = default!; ColumnId = default!; }
}
