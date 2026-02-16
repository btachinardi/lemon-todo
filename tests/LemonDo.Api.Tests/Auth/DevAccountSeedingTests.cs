namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;

[TestClass]
public sealed class DevAccountSeedingTests
{
    [TestMethod]
    public async Task Should_SeedDevAccounts_When_EnvironmentIsDevelopment()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = factory.CreateClient();

        // All three dev accounts should be loginable in Development mode
        (string Email, string Password)[] devAccounts =
        [
            ("dev.user@lemondo.dev", "User1234"),
            ("dev.admin@lemondo.dev", "Admin1234"),
            ("dev.sysadmin@lemondo.dev", "SysAdmin1234"),
        ];

        foreach (var (email, password) in devAccounts)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest(email, password));

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
                $"Dev account {email} should be loginable in Development environment");
        }
    }

    [TestMethod]
    public async Task Should_NotSeedDevAccounts_When_EnvironmentIsProduction()
    {
        using var factory = new CustomWebApplicationFactory()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Production");
            });
        using var client = factory.CreateClient();

        // None of the dev accounts should exist in Production mode
        (string Email, string Password)[] devAccounts =
        [
            ("dev.user@lemondo.dev", "User1234"),
            ("dev.admin@lemondo.dev", "Admin1234"),
            ("dev.sysadmin@lemondo.dev", "SysAdmin1234"),
        ];

        foreach (var (email, password) in devAccounts)
        {
            var response = await client.PostAsJsonAsync(
                "/api/auth/login", new LoginRequest(email, password));

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
                $"Dev account {email} should NOT exist in Production environment");
        }
    }
}
