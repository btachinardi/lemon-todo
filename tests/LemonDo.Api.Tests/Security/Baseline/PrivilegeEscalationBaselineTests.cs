namespace LemonDo.Api.Tests.Security.Baseline;

using System.Net;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Api.Tests.Infrastructure.Security;

/// <summary>
/// Baseline privilege escalation tests.
/// Verifies that regular users cannot access admin endpoints (403),
/// and admins cannot access system-admin-only endpoints (403).
/// </summary>
[TestClass]
public sealed class PrivilegeEscalationBaselineTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _userClient = null!;
    private static HttpClient _adminClient = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _userClient = await _factory.CreateAuthenticatedClientAsync();
        _adminClient = await _factory.CreateAdminClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _userClient.Dispose();
        _adminClient.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    [DynamicData(nameof(EndpointRegistry.AdminOnlyEndpointData), typeof(EndpointRegistry))]
    public async Task Should_Return403_When_UserAccessesAdminEndpoint(EndpointDescriptor endpoint)
    {
        var response = await _userClient.SendEndpointAsync(endpoint);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode,
            $"Regular user must get 403 on {endpoint.DisplayName}");
    }

    [TestMethod]
    [DynamicData(nameof(EndpointRegistry.SystemAdminOnlyEndpointData), typeof(EndpointRegistry))]
    public async Task Should_Return403_When_AdminAccessesSystemAdminEndpoint(EndpointDescriptor endpoint)
    {
        var response = await _adminClient.SendEndpointAsync(endpoint);
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode,
            $"Admin must get 403 on SystemAdmin-only {endpoint.DisplayName}");
    }
}
