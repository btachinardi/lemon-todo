namespace LemonDo.Api.Tests.Security.Baseline;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Baseline information leakage tests.
/// Verifies that error responses do not contain stack traces, internal namespaces,
/// or database internals, and that security headers are present.
/// </summary>
[TestClass]
public sealed class InfoLeakageBaselineTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _authenticatedClient = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _authenticatedClient.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    [DynamicData(nameof(EndpointRegistry.AuthenticatedEndpointData), typeof(EndpointRegistry))]
    public async Task Should_HaveSecurityHeaders(EndpointDescriptor endpoint)
    {
        var response = await _authenticatedClient.SendEndpointAsync(endpoint);
        SecurityAssertions.AssertSecurityHeaders(response, endpoint.DisplayName);
    }

    [TestMethod]
    public async Task Should_NotLeakInternals_When_TaskNotFound()
    {
        var response = await _authenticatedClient.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        await SecurityAssertions.AssertNoInfoLeakageAsync(response, "GET /api/tasks/{id} 404");
    }

    [TestMethod]
    public async Task Should_NotLeakInternals_When_BoardNotFound()
    {
        var response = await _authenticatedClient.GetAsync($"/api/boards/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        await SecurityAssertions.AssertNoInfoLeakageAsync(response, "GET /api/boards/{id} 404");
    }

    [TestMethod]
    public async Task Should_NotLeakInternals_When_ValidationFails()
    {
        var response = await _authenticatedClient.PostAsJsonAsync("/api/tasks",
            new { Title = "" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        await SecurityAssertions.AssertNoInfoLeakageAsync(response, "POST /api/tasks validation error");
    }

    [TestMethod]
    public async Task Should_NotLeakInternals_When_Unauthenticated()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        await SecurityAssertions.AssertNoInfoLeakageAsync(response, "GET /api/tasks 401");
    }
}
