namespace LemonDo.Api.Tests.Analytics;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Endpoints;
using LemonDo.Api.Tests.Infrastructure;

[TestClass]
public sealed class AnalyticsEndpointTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;
    private static HttpClient _anonymousClient = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = await _factory.CreateAuthenticatedClientAsync();
        _anonymousClient = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _anonymousClient.Dispose();
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Should_Return401_When_TrackEventsUnauthenticated()
    {
        var request = new TrackEventsRequest
        {
            Events = [new AnalyticsEvent { EventName = "test_event" }]
        };

        var response = await _anonymousClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return200_When_EmptyEventsList()
    {
        var request = new TrackEventsRequest { Events = [] };

        var response = await _client.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return200_When_BatchOfEvents()
    {
        var request = new TrackEventsRequest
        {
            Events =
            [
                new AnalyticsEvent
                {
                    EventName = "page_viewed",
                    Properties = new Dictionary<string, string>
                    {
                        ["page"] = "/tasks",
                        ["source"] = "sidebar"
                    }
                },
                new AnalyticsEvent
                {
                    EventName = "task_created",
                    Properties = new Dictionary<string, string>
                    {
                        ["priority"] = "High"
                    }
                },
                new AnalyticsEvent { EventName = "onboarding_completed" }
            ]
        };

        var response = await _client.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return200_When_NullEventsList()
    {
        var request = new TrackEventsRequest { Events = null };

        var response = await _client.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }
}
