namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Represents a task's spatial placement on a board: which column it's in and its rank within that column.
/// Immutable — moving a card creates a new instance (remove + add pattern).
/// </summary>
public sealed class TaskCard
{
    public TaskId TaskId { get; }
    public ColumnId ColumnId { get; }
    public decimal Rank { get; }

    internal TaskCard(TaskId taskId, ColumnId columnId, decimal rank)
    {
        TaskId = taskId;
        ColumnId = columnId;
        Rank = rank;
    }

    // EF Core constructor
    private TaskCard() { TaskId = default!; ColumnId = default!; }
}
