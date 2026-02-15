namespace LemonDo.Domain.Boards.ValueObjects;

using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Represents a task's spatial placement on a board: which column it's in and its rank within that column.
/// Immutable — moving a card creates a new instance (remove + add pattern).
/// </summary>
public sealed class TaskCard
{
    /// <summary>The task being placed on the board. References a Task aggregate from the Task bounded context.</summary>
    public TaskId TaskId { get; }

    /// <summary>The column on the board where this task is currently placed.</summary>
    public ColumnId ColumnId { get; }

    /// <summary>Decimal rank used for ordering cards within a column. Lower ranks sort first. Uses neighbor-based computation for sparse ordering.</summary>
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
