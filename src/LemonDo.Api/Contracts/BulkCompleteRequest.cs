namespace LemonDo.Api.Contracts;

/// <summary>Completes multiple tasks in a single operation.</summary>
/// <remarks>
/// Fail-fast behavior: stops on the first error encountered.
/// Previously completed tasks are NOT rolled back on failure.
/// </remarks>
public sealed record BulkCompleteRequest(List<Guid> TaskIds);
