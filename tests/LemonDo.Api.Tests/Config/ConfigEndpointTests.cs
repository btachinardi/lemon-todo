namespace LemonDo.Api.Tests.Config;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

[TestClass]
public sealed class ConfigEndpointTests
{
    [TestMethod]
    public async Task Should_ReturnEnableDemoAccountsTrue_When_FlagIsEnabled()
    {
        using var factory = new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:EnableDemoAccounts"] = "true",
                    });
                });
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/config");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConfigResponse>();
        Assert.IsNotNull(body);
        Assert.IsTrue(body.EnableDemoAccounts);
    }

    [TestMethod]
    public async Task Should_ReturnEnableDemoAccountsFalse_When_FlagIsNotSet()
    {
        using var factory = new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:EnableDemoAccounts"] = "false",
                    });
                });
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/config");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConfigResponse>();
        Assert.IsNotNull(body);
        Assert.IsFalse(body.EnableDemoAccounts);
    }

    [TestMethod]
    public async Task Should_ReturnEnableDemoAccountsFalse_When_FlagIsMissing()
    {
        // Default factory does not set EnableDemoAccounts, so it should default to false
        // unless Development environment auto-enables it
        using var factory = new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    // Explicitly ensure the flag is absent
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:EnableDemoAccounts"] = null,
                    });
                });
            });
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/config");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ConfigResponse>();
        Assert.IsNotNull(body);
        Assert.IsFalse(body.EnableDemoAccounts);
    }

    [TestMethod]
    public async Task Should_BeAccessibleWithoutAuthentication()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/config");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    private sealed record ConfigResponse(bool EnableDemoAccounts);
}
