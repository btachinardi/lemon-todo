namespace LemonDo.Application.Common;

/// <summary>
/// Port for recording analytics events. Implementations must never log raw user IDs or task content.
/// </summary>
public interface IAnalyticsService
{
    /// <summary>Records an analytics event. User IDs must be hashed by implementations.</summary>
    Task TrackAsync(
        string eventName,
        Guid? userId = null,
        Dictionary<string, string>? properties = null,
        CancellationToken ct = default);
}
