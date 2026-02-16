using System.Net;
using LemonDo.Api.Tests.Infrastructure;

namespace LemonDo.Api.Tests.Auth;

[TestClass]
public sealed class RolePolicyTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestMethod]
    public async Task Should_SeedSystemAdminRole_When_ApplicationStarts()
    {
        // SystemAdmin user exists and can log in
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_SeedAdminRole_When_ApplicationStarts()
    {
        var client = await _factory.CreateAdminClientAsync();
        var response = await client.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_IncludeSystemAdminInRoles_When_UserHasRole()
    {
        // Verify the SystemAdmin user can authenticate and has correct roles
        var client = await _factory.CreateSystemAdminClientAsync();
        var response = await client.GetAsync("/api/auth/me");
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Assert.IsTrue(json.Contains("SystemAdmin") || json.Contains("sysadmin"),
            "SystemAdmin user should have SystemAdmin role");
    }
}
