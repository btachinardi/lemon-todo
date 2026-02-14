namespace LemonDo.Domain.Tasks.Entities;

using LemonDo.Domain.Common;
using LemonDo.Domain.Tasks.ValueObjects;

public sealed class Column : Entity<ColumnId>
{
    public ColumnName Name { get; private set; }
    public int Position { get; internal set; }
    public int? WipLimit { get; private set; }

    private Column(ColumnId id, ColumnName name, int position, int? wipLimit = null) : base(id)
    {
        Name = name;
        Position = position;
        WipLimit = wipLimit;
    }

    internal static Column Create(ColumnName name, int position, int? wipLimit = null)
    {
        return new Column(ColumnId.New(), name, position, wipLimit);
    }

    internal void Rename(ColumnName newName)
    {
        Name = newName;
    }

    // EF Core constructor
    private Column() : base(default!) { Name = default!; }
}
