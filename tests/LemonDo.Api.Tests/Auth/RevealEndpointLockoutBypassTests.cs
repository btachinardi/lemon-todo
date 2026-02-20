namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;

/// <summary>
/// Tests that failed password attempts on reveal endpoints (reveal-profile, view-note)
/// correctly increment the account lockout counter, preventing unlimited password guessing.
///
/// The vulnerability: VerifyPasswordAsync used CheckPasswordAsync() which does NOT
/// increment the lockout counter. The fix: use CheckPasswordSignInAsync(lockoutOnFailure: true).
/// </summary>
[TestClass]
public sealed class RevealEndpointLockoutBypassTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static readonly JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    [ClassInitialize]
    public static void ClassInit(TestContext _) => _factory = new CustomWebApplicationFactory();

    [ClassCleanup]
    public static void ClassCleanup() => _factory.Dispose();

    // ============================================================
    // TEST 1: reveal-profile triggers lockout
    // ============================================================

    [TestMethod]
    public async Task Should_TriggerLockout_When_MultipleFailedRevealProfileAttempts()
    {
        var (client, email) = await RegisterFreshUser("lockout-reveal");

        // ATTACK: Make 6 failed password attempts via reveal-profile.
        // ASP.NET Identity locks after 5 failed attempts (MaxFailedAccessAttempts=5).
        // The 5th attempt triggers lockout on the spot; attempt 6 confirms it persists.
        for (var i = 0; i < 6; i++)
        {
            await client.PostAsJsonAsync("/api/auth/reveal-profile",
                new { Password = $"WrongPassword{i}!" });
        }

        // VERIFY: Login with CORRECT password should be locked out (429)
        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = "TestPass123!" });

        Assert.AreEqual(
            HttpStatusCode.TooManyRequests,
            loginResponse.StatusCode,
            "Failed password attempts via /api/auth/reveal-profile should trigger " +
            "account lockout, blocking subsequent login attempts.");
    }

    // ============================================================
    // TEST 2: view-note triggers lockout
    // ============================================================

    [TestMethod]
    public async Task Should_TriggerLockout_When_MultipleFailedViewNoteAttempts()
    {
        var (client, email) = await RegisterFreshUser("lockout-viewnote");

        // Create a task with a sensitive note so view-note gets past the "not found" check
        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Lockout Test Task", SensitiveNote = "secret-data-for-testing" });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var task = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = task.GetProperty("id").GetString();

        // ATTACK: 6 failed password attempts via view-note
        for (var i = 0; i < 6; i++)
        {
            await client.PostAsJsonAsync(
                $"/api/tasks/{taskId}/view-note",
                new { Password = $"WrongPassword{i}!" });
        }

        // VERIFY: Login should be locked
        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = "TestPass123!" });

        Assert.AreEqual(
            HttpStatusCode.TooManyRequests,
            loginResponse.StatusCode,
            "Failed password attempts via /api/tasks/{id}/view-note should trigger " +
            "account lockout, blocking subsequent login attempts.");
    }

    // ============================================================
    // TEST 3: Lockout accumulates across reveal endpoints
    // ============================================================

    [TestMethod]
    public async Task Should_TriggerLockout_When_FailedAttemptsSpreadAcrossRevealEndpoints()
    {
        // The lockout counter should accumulate across ALL password verification
        // endpoints, not be tracked per-endpoint.

        var (client, email) = await RegisterFreshUser("lockout-combined");

        // Create a task with sensitive note
        var createResponse = await client.PostAsJsonAsync("/api/tasks",
            new { Title = "Combined Test Task", SensitiveNote = "combined-test-secret" });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode);
        var task = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var taskId = task.GetProperty("id").GetString();

        // ATTACK: Spread 6 failed attempts across two endpoints
        // 3 via reveal-profile + 3 via view-note = 6 total (exceeds threshold of 5)
        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync("/api/auth/reveal-profile",
                new { Password = $"WrongPassword{i}!" });
        }

        for (var i = 0; i < 3; i++)
        {
            await client.PostAsJsonAsync($"/api/tasks/{taskId}/view-note",
                new { Password = $"WrongPassword{i + 10}!" });
        }

        // VERIFY: Login should be locked
        using var loginClient = _factory.CreateClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login",
            new { Email = email, Password = "TestPass123!" });

        Assert.AreEqual(
            HttpStatusCode.TooManyRequests,
            loginResponse.StatusCode,
            "Failed password attempts spread across reveal-profile and view-note " +
            "should accumulate in the lockout counter, blocking subsequent login.");
    }

    // ============================================================
    // TEST 4: Reveal endpoint returns 429 when already locked
    // ============================================================

    [TestMethod]
    public async Task Should_Return429OnReveal_When_AccountAlreadyLocked()
    {
        // After lockout via login failures, reveal endpoints should also
        // refuse re-authentication with 429 (not silently accept wrong passwords).

        var (client, email) = await RegisterFreshUser("lockout-reveal-locked");

        // Lock the account via login failures (established behavior)
        using var loginClient = _factory.CreateClient();
        for (var i = 0; i < 6; i++)
        {
            await loginClient.PostAsJsonAsync("/api/auth/login",
                new { Email = email, Password = "WrongPassword!" });
        }

        // VERIFY: Reveal endpoint should return 429 (locked), not 401 (bad password).
        // Temporary lockout does NOT block access token usage (only password-based ops).
        // The reveal endpoint itself checks lockout via VerifyPasswordAsync.
        var revealResponse = await client.PostAsJsonAsync("/api/auth/reveal-profile",
            new { Password = "TestPass123!" });

        Assert.AreEqual(
            HttpStatusCode.TooManyRequests,
            revealResponse.StatusCode,
            "Reveal endpoint should return 429 when the account is locked, " +
            "not attempt password verification.");
    }

    // ============================================================
    // Helper
    // ============================================================

    /// <summary>Registers a fresh user and returns an authenticated client + email.</summary>
    private static async Task<(HttpClient Client, string Email)> RegisterFreshUser(string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@lemondo.dev";
        const string password = "TestPass123!";
        var client = _factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = password, DisplayName = $"Test {prefix}" });
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode, "Registration should succeed");
        var authResult = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOpts);
        Assert.IsNotNull(authResult);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResult.AccessToken);

        return (client, email);
    }
}
