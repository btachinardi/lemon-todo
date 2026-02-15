namespace LemonDo.Domain.Tasks.ValueObjects;

using LemonDo.Domain.Common;

/// <summary>
/// Strongly-typed identifier for a <see cref="Entities.Task"/> aggregate.
/// </summary>
public sealed class TaskId : ValueObject<Guid>, IReconstructable<TaskId, Guid>
{
    /// <summary>Creates a <see cref="TaskId"/> from an existing GUID.</summary>
    public TaskId(Guid value) : base(value) { }

    /// <summary>Generates a new random <see cref="TaskId"/>.</summary>
    public static TaskId New() => new(Guid.NewGuid());

    /// <summary>Wraps an existing GUID as a <see cref="TaskId"/>.</summary>
    public static TaskId From(Guid value) => new(value);

    /// <summary>Reconstructs a <see cref="TaskId"/> from a persistence value.</summary>
    public static TaskId Reconstruct(Guid value) => new(value);
}
