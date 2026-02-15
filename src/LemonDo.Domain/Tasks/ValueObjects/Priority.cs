namespace LemonDo.Domain.Tasks.ValueObjects;

/// <summary>
/// Urgency level for a task, used for sorting and visual cues on the board.
/// </summary>
public enum Priority
{
    /// <summary>No priority assigned (default).</summary>
    None = 0,

    /// <summary>Low urgency — address when convenient.</summary>
    Low = 1,

    /// <summary>Normal urgency — address in regular flow.</summary>
    Medium = 2,

    /// <summary>High urgency — address soon.</summary>
    High = 3,

    /// <summary>Blocking urgency — address immediately.</summary>
    Critical = 4,
}
