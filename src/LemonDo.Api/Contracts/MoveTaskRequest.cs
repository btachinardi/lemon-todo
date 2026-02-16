namespace LemonDo.Api.Contracts;

/// <summary>Moves a task to a new position using neighbor-based ranking.</summary>
/// <remarks>
/// PreviousTaskId and NextTaskId define the insertion position:
/// - Both null: place at the top of the column.
/// - Only PreviousTaskId: place immediately after that task.
/// - Only NextTaskId: place immediately before that task.
/// - Both set: place between the two tasks.
/// Task's status is automatically updated to match the column's TargetStatus.
/// </remarks>
public sealed record MoveTaskRequest(Guid ColumnId, Guid? PreviousTaskId, Guid? NextTaskId);
