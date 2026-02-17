namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

[TestClass]
public sealed class DevAccountSeedingTests
{
    private static readonly (string Email, string Password)[] DevAccounts =
    [
        ("dev.user@lemondo.dev", "User1234"),
        ("dev.admin@lemondo.dev", "Admin1234"),
        ("dev.sysadmin@lemondo.dev", "SysAdmin1234"),
    ];

    [TestMethod]
    public async Task Should_SeedDevAccounts_When_FeatureFlagIsEnabled()
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

        foreach (var (email, password) in DevAccounts)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest(email, password));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                $"Dev account {email} should be loginable when EnableDemoAccounts=true");
        }
    }

    [TestMethod]
    public async Task Should_NotSeedDevAccounts_When_FeatureFlagIsDisabled()
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

        foreach (var (email, password) in DevAccounts)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest(email, password));

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
                $"Dev account {email} should NOT exist when EnableDemoAccounts=false");
        }
    }

    [TestMethod]
    public async Task Should_SeedDevAccounts_When_FeatureFlagIsEnabledInProduction()
    {
        using var factory = new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:EnableDemoAccounts"] = "true",
                    });
                });
            });
        using var client = factory.CreateClient();

        foreach (var (email, password) in DevAccounts)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest(email, password));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                $"Dev account {email} should be loginable when EnableDemoAccounts=true even in Production");
        }
    }

    [TestMethod]
    public async Task Should_NotSeedDevAccounts_When_FlagIsMissingAndNotDevelopment()
    {
        using var factory = new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });
        using var client = factory.CreateClient();

        foreach (var (email, password) in DevAccounts)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest(email, password));

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
                $"Dev account {email} should NOT exist when flag is missing in Production");
        }
    }
}
