namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Represents a task's spatial placement on a board: which column it's in and its position within that column.
/// Immutable — moving a card creates a new instance (remove + add pattern).
/// </summary>
public sealed class TaskCard
{
    public TaskId TaskId { get; }
    public ColumnId ColumnId { get; }
    public int Position { get; }

    internal TaskCard(TaskId taskId, ColumnId columnId, int position)
    {
        TaskId = taskId;
        ColumnId = columnId;
        Position = position;
    }

    // EF Core constructor
    private TaskCard() { TaskId = default!; ColumnId = default!; }
}
