namespace LemonDo.Domain.Boards.Entities;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// A board column that maps to a specific <see cref="TaskStatus"/>.
/// When a task card is placed in this column, its status should be set to <see cref="TargetStatus"/>.
/// </summary>
public sealed class Column : Entity<ColumnId>
{
    public ColumnName Name { get; private set; }

    /// <summary>
    /// The <see cref="TaskStatus"/> that tasks in this column should have.
    /// </summary>
    public TaskStatus TargetStatus { get; }
    public int Position { get; internal set; }
    public int? MaxTasks { get; private set; }

    /// <summary>
    /// Monotonic counter for assigning ranks to new cards in this column.
    /// Starts at 1000 and increments by 1000 on each placement.
    /// </summary>
    public decimal NextRank { get; internal set; } = 1000m;

    private Column(ColumnId id, ColumnName name, TaskStatus targetStatus, int position, int? wipLimit = null) : base(id)
    {
        Name = name;
        TargetStatus = targetStatus;
        Position = position;
        MaxTasks = wipLimit;
    }

    internal static Column Create(ColumnName name, int position, TaskStatus targetStatus, int? wipLimit = null)
    {
        return new Column(ColumnId.New(), name, targetStatus, position, wipLimit);
    }

    internal void Rename(ColumnName newName)
    {
        Name = newName;
    }

    // EF Core constructor
    private Column() : base(default!) { Name = default!; }
}
