namespace LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Task lifecycle status. Each board column maps to exactly one target status,
/// so moving a card between columns triggers a corresponding status transition.
/// </summary>
public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    Done = 2,
}
