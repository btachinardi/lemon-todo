namespace LemonDo.Api.Endpoints;

using LemonDo.Application.Common;

/// <summary>Minimal API endpoint for receiving batched frontend analytics events.</summary>
public static class AnalyticsEndpoints
{
    /// <summary>Maps the <c>POST /api/analytics/events</c> endpoint for client-side event tracking.</summary>
    public static RouteGroupBuilder MapAnalyticsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/analytics").WithTags("Analytics").RequireAuthorization();

        group.MapPost("/events", TrackEvents);

        return group;
    }

    private static async Task<IResult> TrackEvents(
        IAnalyticsService analyticsService,
        TrackEventsRequest request,
        CancellationToken ct)
    {
        if (request.Events is null || request.Events.Count == 0)
            return Results.Ok();

        foreach (var evt in request.Events)
        {
            await analyticsService.TrackAsync(
                evt.EventName,
                properties: evt.Properties,
                ct: ct);
        }

        return Results.Ok();
    }
}

/// <summary>Batch of analytics events sent from the frontend.</summary>
public sealed record TrackEventsRequest
{
    /// <summary>The list of analytics events to record.</summary>
    public List<AnalyticsEvent>? Events { get; init; }
}

/// <summary>A single analytics event from the frontend.</summary>
public sealed record AnalyticsEvent
{
    /// <summary>The name of the event (e.g. <c>task_created</c>).</summary>
    public required string EventName { get; init; }

    /// <summary>Optional key-value properties attached to the event.</summary>
    public Dictionary<string, string>? Properties { get; init; }
}
