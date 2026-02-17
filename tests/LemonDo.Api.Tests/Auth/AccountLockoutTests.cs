namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Json;
using LemonDo.Api.Tests.Infrastructure;

[TestClass]
public sealed class AccountLockoutTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    /// <summary>Unique email for lockout tests to avoid cross-test interference.</summary>
    private const string LockoutEmail = "lockout@lemondo.dev";
    private const string LockoutPassword = "LockTest123!";

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();

        // Register a dedicated user for lockout testing
        await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = LockoutEmail, Password = LockoutPassword, DisplayName = "Lockout Tester" });
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Should_LockAccount_When_TooManyFailedAttempts()
    {
        // Attempt 5 logins with wrong password
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login",
                new { Email = LockoutEmail, Password = "WrongPassword!" });
        }

        // 6th attempt should be locked out (429)
        var lockedResponse = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = LockoutEmail, Password = "WrongPassword!" });

        Assert.AreEqual(HttpStatusCode.TooManyRequests, lockedResponse.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnTooManyRequests_When_CorrectPasswordButLocked()
    {
        // Register a fresh user so lockout state is clean
        const string freshEmail = "lockout-fresh@lemondo.dev";
        await _client.PostAsJsonAsync("/api/auth/register",
            new { Email = freshEmail, Password = "FreshPass123!", DisplayName = "Fresh Lockout" });

        // Lock the account
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/auth/login",
                new { Email = freshEmail, Password = "WrongPassword!" });
        }

        // Even correct password returns 429 while locked
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { Email = freshEmail, Password = "FreshPass123!" });

        Assert.AreEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
    }
}
