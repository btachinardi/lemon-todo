namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Regression tests for the AddUserAdminColumns migration defect.
/// The original migration added CreatedAt with defaultValue: "" (empty string),
/// which causes FormatException when EF Core reads existing users.
/// The FixCreatedAtEmptyString migration backfills '' → valid DateTimeOffset.
/// </summary>
[TestClass]
public sealed class CreatedAtMigrationTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    [TestMethod]
    public async Task Should_LoginSuccessfully_After_FixMigrationBackfillsEmptyCreatedAt()
    {
        // Arrange: simulate the migration defect by setting CreatedAt to ''
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE AspNetUsers SET CreatedAt = '' WHERE Email = {0}",
                CustomWebApplicationFactory.TestUserEmail);
        }

        // Apply the same fix SQL from FixCreatedAtEmptyString migration
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE AspNetUsers SET CreatedAt = '2026-02-15T00:00:00.0000000+00:00' WHERE CreatedAt = ''");
        }

        // Act: login after fix migration has been applied
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(
                CustomWebApplicationFactory.TestUserEmail,
                CustomWebApplicationFactory.TestUserPassword));

        // Assert: login should succeed now that CreatedAt is a valid DateTimeOffset
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Login should succeed after FixCreatedAtEmptyString migration backfills empty CreatedAt.");

        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        Assert.IsNotNull(auth.AccessToken);
    }

    [TestMethod]
    public async Task Should_FailLogin_When_CreatedAtIsEmptyString()
    {
        // Arrange: simulate the migration defect — no fix applied
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<LemonDoDbContext>();
            await db.Database.ExecuteSqlRawAsync(
                "UPDATE AspNetUsers SET CreatedAt = '' WHERE Email = {0}",
                CustomWebApplicationFactory.AdminUserEmail);
        }

        // Act: login with corrupted CreatedAt
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(
                CustomWebApplicationFactory.AdminUserEmail,
                CustomWebApplicationFactory.AdminUserPassword));

        // Assert: returns 500 (the defect we're fixing)
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Empty CreatedAt string should cause 500 — this proves the defect exists.");

        // Cleanup: restore valid CreatedAt so other tests aren't affected
        using var scope2 = _factory.Services.CreateScope();
        var db2 = scope2.ServiceProvider.GetRequiredService<LemonDoDbContext>();
        await db2.Database.ExecuteSqlRawAsync(
            "UPDATE AspNetUsers SET CreatedAt = '2026-02-15T00:00:00.0000000+00:00' WHERE Email = {0}",
            CustomWebApplicationFactory.AdminUserEmail);
    }
}
