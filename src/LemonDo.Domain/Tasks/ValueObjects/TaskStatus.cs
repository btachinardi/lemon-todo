namespace LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Task lifecycle status. Each board column maps to exactly one target status,
/// so moving a card between columns triggers a corresponding status transition.
/// </summary>
public enum TaskStatus
{
    /// <summary>Task is pending and has not been started.</summary>
    Todo = 0,

    /// <summary>Task is actively being worked on.</summary>
    InProgress = 1,

    /// <summary>Task has been finished. Sets <see cref="Entities.Task.CompletedAt"/>.</summary>
    Done = 2,
}
