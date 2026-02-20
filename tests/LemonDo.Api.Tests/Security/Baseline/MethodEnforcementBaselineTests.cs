namespace LemonDo.Api.Tests.Security.Baseline;

using System.Net;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Baseline HTTP method enforcement tests.
/// Verifies that denied HTTP methods return 405 Method Not Allowed (or 404 for non-routable).
/// </summary>
[TestClass]
public sealed class MethodEnforcementBaselineTests
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
    [DynamicData(nameof(EndpointRegistry.MethodEnforcementData), typeof(EndpointRegistry))]
    public async Task Should_Return405_When_DeniedMethod(string path, HttpMethod deniedMethod)
    {
        var resolvedPath = path
            .Replace("{id}", Guid.NewGuid().ToString())
            .Replace("{colId}", Guid.NewGuid().ToString())
            .Replace("{tag}", "sometag")
            .Replace("{roleName}", "Admin");

        var response = await _authenticatedClient.SendMethodAsync(deniedMethod, resolvedPath,
            deniedMethod == HttpMethod.Put ? new { } : null);

        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.MethodNotAllowed or HttpStatusCode.NotFound,
            $"{deniedMethod.Method} {path} should return 405 or 404, got {(int)response.StatusCode}");
    }
}
