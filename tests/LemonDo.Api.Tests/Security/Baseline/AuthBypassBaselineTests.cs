namespace LemonDo.Api.Tests.Security.Baseline;

using System.Net;
using System.Net.Http.Headers;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Baseline auth bypass tests for ALL authenticated endpoints.
/// Uses DynamicData over the EndpointRegistry to generate one test per endpoint.
/// </summary>
[TestClass]
public sealed class AuthBypassBaselineTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestMethod]
    [DynamicData(nameof(EndpointRegistry.AuthenticatedEndpointData), typeof(EndpointRegistry))]
    public async Task Should_Return401_When_NoToken(EndpointDescriptor endpoint)
    {
        var response = await _factory.SendUnauthenticatedAsync(endpoint);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            $"{endpoint.DisplayName} must reject unauthenticated requests");
    }

    [TestMethod]
    [DynamicData(nameof(EndpointRegistry.AuthenticatedEndpointData), typeof(EndpointRegistry))]
    public async Task Should_Return401_When_EmptyBearer(EndpointDescriptor endpoint)
    {
        var response = await _factory.SendWithEmptyBearerAsync(endpoint);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            $"{endpoint.DisplayName} must reject empty Bearer token");
    }

    [TestMethod]
    [DynamicData(nameof(MalformedTokens.AllInvalidTokens), typeof(MalformedTokens))]
    public async Task Should_Return401_When_MalformedToken(string token, string label)
    {
        // Test against a representative authenticated endpoint
        using var client = _factory.CreateClient();
        if (token.Length > 0)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        else
            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");

        var response = await client.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            $"Malformed token [{label}] must be rejected on GET /api/tasks");
    }
}
