namespace LemonDo.Application.Common;

using System.Diagnostics.Metrics;

/// <summary>
/// Application-level business metrics exposed via OpenTelemetry.
/// Tracks task operations, domain errors, and board activity.
/// </summary>
public sealed class ApplicationMetrics
{
    /// <summary>The meter name used for OpenTelemetry registration.</summary>
    public const string MeterName = "LemonDo.Application";

    private readonly Counter<long> _tasksCreated;
    private readonly Counter<long> _tasksCompleted;
    private readonly Counter<long> _tasksDeleted;
    private readonly Counter<long> _tasksMoved;
    private readonly Counter<long> _domainErrors;

    /// <summary>Initializes application metrics instruments on the shared meter.</summary>
    public ApplicationMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _tasksCreated = meter.CreateCounter<long>(
            "lemondo.tasks.created",
            description: "Number of tasks created");

        _tasksCompleted = meter.CreateCounter<long>(
            "lemondo.tasks.completed",
            description: "Number of tasks completed");

        _tasksDeleted = meter.CreateCounter<long>(
            "lemondo.tasks.deleted",
            description: "Number of tasks deleted");

        _tasksMoved = meter.CreateCounter<long>(
            "lemondo.tasks.moved",
            description: "Number of tasks moved between columns");

        _domainErrors = meter.CreateCounter<long>(
            "lemondo.domain.errors",
            description: "Number of domain-level errors by error type");
    }

    /// <summary>Records a task creation event.</summary>
    public void TaskCreated() => _tasksCreated.Add(1);

    /// <summary>Records a task completion event.</summary>
    public void TaskCompleted() => _tasksCompleted.Add(1);

    /// <summary>Records a task deletion event.</summary>
    public void TaskDeleted() => _tasksDeleted.Add(1);

    /// <summary>Records a task move event.</summary>
    public void TaskMoved() => _tasksMoved.Add(1);

    /// <summary>Records a domain error with the error type as a tag.</summary>
    public void DomainError(string errorType) =>
        _domainErrors.Add(1, new KeyValuePair<string, object?>("error.type", errorType));
}
