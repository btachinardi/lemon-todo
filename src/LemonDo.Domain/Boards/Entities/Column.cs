namespace LemonDo.Domain.Boards.Entities;

using LemonDo.Domain.Boards.ValueObjects;
using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed class Column : Entity<ColumnId>
{
    public ColumnName Name { get; private set; }
    public TaskStatus TargetStatus { get; }
    public int Position { get; internal set; }
    public int? MaxTasks { get; private set; }

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
