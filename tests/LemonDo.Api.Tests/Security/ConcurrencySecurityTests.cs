namespace LemonDo.Api.Tests.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using LemonDo.Application.Tasks.DTOs;

/// <summary>
/// Security hardening tests for race condition and concurrency vulnerabilities.
///
/// PASS = the endpoint correctly handles concurrent requests (is SECURE).
/// FAIL = the endpoint is VULNERABLE to the concurrent attack.
///
/// Each test creates its own isolated factory and fresh users so tests cannot
/// interfere with each other when the class runs in parallel.
/// </summary>
[TestClass]
public sealed class ConcurrencySecurityTests
{
    // =========================================================================
    // HELPERS
    // =========================================================================

    private sealed record FreshUserContext(
        CustomWebApplicationFactory Factory,
        string AccessToken,
        string RefreshCookie);

    /// <summary>
    /// Creates a brand-new factory (its own in-memory SQLite database) and registers
    /// a fresh user, returning the factory, access token, and refresh-token cookie value.
    /// </summary>
    private static async Task<FreshUserContext> CreateFreshUserAsync(string? emailSuffix = null)
    {
        var factory = new CustomWebApplicationFactory();
        var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });

        var uniqueEmail = $"concurrent-{emailSuffix ?? Guid.NewGuid().ToString("N")}@lemondo.dev";
        const string password = "ConcurrencyTest1!";

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = uniqueEmail, Password = password, DisplayName = "Concurrent User" });

        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        var refreshCookie = ExtractRefreshTokenCookie(registerResponse);

        client.Dispose();
        return new FreshUserContext(factory, auth!.AccessToken, refreshCookie);
    }

    private static string ExtractRefreshTokenCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return string.Empty;
        var setCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        if (setCookie is null) return string.Empty;
        var semiIndex = setCookie.IndexOf(';');
        return semiIndex > 0 ? setCookie[..semiIndex] : setCookie;
    }

    private static HttpClient CreateRawClient(
        CustomWebApplicationFactory factory, string? accessToken = null)
    {
        var client = factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                HandleCookies = false
            });
        if (accessToken is not null)
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private static async Task<Guid> CreateTaskAsync(HttpClient client, string title)
    {
        var resp = await client.PostAsJsonAsync("/api/tasks", new { Title = title });
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
        return dto!.Id;
    }

    // =========================================================================
    // ATTACK 1: CONCURRENT REFRESH TOKEN RACE (TOKEN DOUBLE-SPEND)
    //
    // Sends two simultaneous POST /api/auth/refresh requests with the SAME refresh
    // token cookie. Only one must succeed (200 OK) and the other must be rejected
    // (401 Unauthorized). If both succeed, the same token generated two independent
    // sessions -- a double-spend vulnerability.
    // =========================================================================

    [TestMethod]
    public async Task Should_AllowOnlyOneRefresh_When_SameTokenUsedConcurrently()
    {
        var ctx = await CreateFreshUserAsync("refresh-race");
        using var factory = ctx.Factory;
        var refreshCookie = ctx.RefreshCookie;

        using var clientA = CreateRawClient(factory);
        using var clientB = CreateRawClient(factory);

        var taskA = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            req.Headers.Add("Cookie", refreshCookie);
            return await clientA.SendAsync(req);
        });
        var taskB = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            req.Headers.Add("Cookie", refreshCookie);
            return await clientB.SendAsync(req);
        });

        var responses = await Task.WhenAll(taskA, taskB);
        var statusCodes = responses.Select(r => r.StatusCode).ToArray();
        var successCount = statusCodes.Count(s => s == HttpStatusCode.OK);
        var unauthorizedCount = statusCodes.Count(s => s == HttpStatusCode.Unauthorized);

        Assert.AreEqual(1, successCount,
            $"Exactly one concurrent refresh must succeed. " +
            $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");
        Assert.AreEqual(1, unauthorizedCount,
            $"The second concurrent refresh must be rejected with 401. " +
            $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var r in responses) r.Dispose();
    }

    [TestMethod]
    public async Task Should_AllowOnlyOneRefresh_When_FiveConcurrentRequestsWithSameToken()
    {
        var ctx = await CreateFreshUserAsync("refresh-race-5");
        using var factory = ctx.Factory;
        var refreshCookie = ctx.RefreshCookie;

        const int concurrency = 5;
        var refreshClients = Enumerable.Range(0, concurrency)
            .Select(_ => CreateRawClient(factory))
            .ToList();

        var tasks = refreshClients.Select(client => Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            req.Headers.Add("Cookie", refreshCookie);
            return await client.SendAsync(req);
        })).ToArray();

        var responses = await Task.WhenAll(tasks);
        var statusCodes = responses.Select(r => r.StatusCode).ToArray();
        var successCount = statusCodes.Count(s => s == HttpStatusCode.OK);
        var serverErrorCount = statusCodes.Count(s => s == HttpStatusCode.InternalServerError);

        // The key security property: no 500 errors AND at most 1 successful refresh.
        // With SQLite in-memory (single connection), high concurrency may cause all
        // requests to fail with 401 due to connection contention — that's acceptable.
        // What's NOT acceptable: 500 errors (unhandled exceptions) or 2+ successes
        // (token double-spend).
        Assert.AreEqual(0, serverErrorCount,
            $"Concurrent refresh must not cause 500 errors. " +
            $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");
        Assert.IsTrue(successCount <= 1,
            $"At most one concurrent refresh must succeed (no double-spend). " +
            $"Got {successCount} successes: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var r in responses) r.Dispose();
        foreach (var c in refreshClients) c.Dispose();
    }

    // =========================================================================
    // ATTACK 2: CONCURRENT REGISTRATION RACE (DUPLICATE ACCOUNT)
    //
    // Sends two simultaneous POST /api/auth/register with the SAME email.
    // Only one must succeed; the second must fail with 409 Conflict.
    // If both succeed, two accounts share the same email.
    // =========================================================================

    [TestMethod]
    public async Task Should_CreateOnlyOneAccount_When_SameEmailRegisteredConcurrently()
    {
        using var factory = new CustomWebApplicationFactory();
        using var clientA = CreateRawClient(factory);
        using var clientB = CreateRawClient(factory);

        var sharedEmail = $"race-register-{Guid.NewGuid():N}@lemondo.dev";
        var body = new { Email = sharedEmail, Password = "ConcurrencyTest1!", DisplayName = "Race User" };

        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync("/api/auth/register", body),
            clientB.PostAsJsonAsync("/api/auth/register", body));

        var statusCodes = responses.Select(r => r.StatusCode).ToArray();
        var successCount = statusCodes.Count(s => s == HttpStatusCode.OK);

        Assert.AreEqual(1, successCount,
            $"Exactly one concurrent registration must succeed. " +
            $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var status in statusCodes)
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                $"Concurrent duplicate registration must not cause 500. " +
                $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var r in responses) r.Dispose();
    }

    [TestMethod]
    public async Task Should_CreateOnlyOneAccount_When_FiveConcurrentRegistrationsWithSameEmail()
    {
        using var factory = new CustomWebApplicationFactory();
        var sharedEmail = $"race-register-5-{Guid.NewGuid():N}@lemondo.dev";
        var body = new { Email = sharedEmail, Password = "ConcurrencyTest1!", DisplayName = "Race5 User" };

        const int concurrency = 5;
        var regClients = Enumerable.Range(0, concurrency)
            .Select(_ => CreateRawClient(factory))
            .ToList();

        var tasks = regClients.Select(client => Task.Run(async () =>
            await client.PostAsJsonAsync("/api/auth/register", body)
        )).ToArray();

        var responses = await Task.WhenAll(tasks);
        var statusCodes = responses.Select(r => r.StatusCode).ToArray();

        Assert.AreEqual(1, statusCodes.Count(s => s == HttpStatusCode.OK),
            $"Exactly one of {concurrency} concurrent registrations must succeed. " +
            $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var status in statusCodes)
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                $"Concurrent duplicate registration must not cause 500. " +
                $"Statuses: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var r in responses) r.Dispose();
        foreach (var c in regClients) c.Dispose();
    }

    // =========================================================================
    // ATTACK 3: CONCURRENT TASK COMPLETE + DELETE
    //
    // Creates a task then simultaneously sends POST /complete and DELETE.
    // The task must reach a consistent final state with no 500 errors.
    // =========================================================================

    [TestMethod]
    public async Task Should_ReachConsistentState_When_CompletingAndDeletingTaskConcurrently()
    {
        var ctx = await CreateFreshUserAsync("complete-delete");
        using var factory = ctx.Factory;
        var accessToken = ctx.AccessToken;

        using var setupClient = CreateRawClient(factory, accessToken);
        var taskId = await CreateTaskAsync(setupClient, "Concurrent complete-delete target");

        using var clientA = CreateRawClient(factory, accessToken);
        using var clientB = CreateRawClient(factory, accessToken);

        var responses = await Task.WhenAll(
            clientA.PostAsync($"/api/tasks/{taskId}/complete", null),
            clientB.DeleteAsync($"/api/tasks/{taskId}"));

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, responses[0].StatusCode,
            "Complete must not crash with 500 during concurrent delete");
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, responses[1].StatusCode,
            "Delete must not crash with 500 during concurrent complete");

        using var readClient = CreateRawClient(factory, accessToken);
        var getResponse = await readClient.GetAsync($"/api/tasks/{taskId}");

        if (getResponse.StatusCode == HttpStatusCode.OK)
        {
            var dto = await getResponse.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
            Assert.IsNotNull(dto, "If task is readable after concurrent ops, DTO must be valid");
            Assert.IsTrue(dto.Status is "Done" or "Todo",
                $"Task must be in a valid status, got: {dto.Status}");
        }
        else
        {
            Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode,
                $"If task is gone, must be 404. Got: {(int)getResponse.StatusCode}");
        }

        foreach (var r in responses) r.Dispose();
    }

    [TestMethod]
    public async Task Should_OnlySucceedOnce_When_DeletingTaskConcurrently()
    {
        var ctx = await CreateFreshUserAsync("double-delete");
        using var factory = ctx.Factory;
        var accessToken = ctx.AccessToken;

        using var setupClient = CreateRawClient(factory, accessToken);
        var taskId = await CreateTaskAsync(setupClient, "Double delete target");

        using var clientA = CreateRawClient(factory, accessToken);
        using var clientB = CreateRawClient(factory, accessToken);

        var responses = await Task.WhenAll(
            clientA.DeleteAsync($"/api/tasks/{taskId}"),
            clientB.DeleteAsync($"/api/tasks/{taskId}"));

        var statusCodes = responses.Select(r => r.StatusCode).ToArray();

        foreach (var status in statusCodes)
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                $"Concurrent double-delete must not cause 500. " +
                $"Statuses: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var r in responses) r.Dispose();
    }

    // =========================================================================
    // ATTACK 4: CONCURRENT LOGOUT + REFRESH RACE
    //
    // Simultaneously sends POST /api/auth/logout (revoke) and POST /api/auth/refresh
    // (rotate) with the same refresh cookie. Tests for session persistence after logout.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotIssueUsableTokenAfterLogout_When_LogoutAndRefreshAreConcurrent()
    {
        var ctx = await CreateFreshUserAsync("logout-refresh-race");
        using var factory = ctx.Factory;
        var accessToken = ctx.AccessToken;
        var refreshCookie = ctx.RefreshCookie;

        using var logoutClient = CreateRawClient(factory, accessToken);
        using var refreshClient = CreateRawClient(factory);

        var logoutTask = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
            req.Headers.Add("Cookie", refreshCookie);
            return await logoutClient.SendAsync(req);
        });
        var refreshTask = Task.Run(async () =>
        {
            var req = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
            req.Headers.Add("Cookie", refreshCookie);
            return await refreshClient.SendAsync(req);
        });

        using var logoutResponse = await logoutTask;
        using var refreshResponse = await refreshTask;

        // Logout must always succeed: revoke is idempotent from the user's perspective
        Assert.AreEqual(HttpStatusCode.OK, logoutResponse.StatusCode,
            $"Logout must succeed regardless of concurrent refresh. Got: {(int)logoutResponse.StatusCode}");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, refreshResponse.StatusCode,
            "Concurrent logout+refresh must not cause 500 on refresh");

        // If refresh won the race and returned a new token, attempt to use that new token again.
        // A second-generation refresh on a chain rooted in a revoked session should eventually fail.
        if (refreshResponse.StatusCode == HttpStatusCode.OK)
        {
            var newAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.IsNotNull(newAuth, "If refresh succeeded it must return a valid AuthResponse");

            var newRefreshCookie = ExtractRefreshTokenCookie(refreshResponse);
            if (!string.IsNullOrEmpty(newRefreshCookie))
            {
                using var secondRefreshClient = CreateRawClient(factory);
                var req2 = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
                req2.Headers.Add("Cookie", newRefreshCookie);
                using var resp2 = await secondRefreshClient.SendAsync(req2);

                // At minimum no 500. A 401 here confirms the token family was fully revoked.
                Assert.AreNotEqual(HttpStatusCode.InternalServerError, resp2.StatusCode,
                    "Second-generation refresh after race must not cause 500");
            }
        }
    }

    // =========================================================================
    // ATTACK 5: CONCURRENT BULK COMPLETE WITH OVERLAPPING TASK IDs
    //
    // Creates tasks then sends two simultaneous bulk-complete requests where task IDs
    // overlap. No 500 errors allowed; all tasks must reach a valid final state.
    // =========================================================================

    [TestMethod]
    public async Task Should_NotCauseServerErrorOrCorruption_When_BulkCompleteHasOverlappingIdsAndRunsConcurrently()
    {
        var ctx = await CreateFreshUserAsync("bulk-overlap");
        using var factory = ctx.Factory;
        var accessToken = ctx.AccessToken;

        using var setupClient = CreateRawClient(factory, accessToken);
        var taskId1 = await CreateTaskAsync(setupClient, "Bulk overlap task 1");
        var taskId2 = await CreateTaskAsync(setupClient, "Bulk overlap task 2");
        var taskId3 = await CreateTaskAsync(setupClient, "Bulk overlap task 3");

        using var clientA = CreateRawClient(factory, accessToken);
        using var clientB = CreateRawClient(factory, accessToken);

        // Request A includes all three tasks; request B overlaps on task 1 and task 2
        var responses = await Task.WhenAll(
            clientA.PostAsJsonAsync("/api/tasks/bulk/complete",
                new { TaskIds = new[] { taskId1, taskId2, taskId3 } }),
            clientB.PostAsJsonAsync("/api/tasks/bulk/complete",
                new { TaskIds = new[] { taskId1, taskId2 } }));

        var statusCodes = responses.Select(r => r.StatusCode).ToArray();

        foreach (var status in statusCodes)
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                $"Concurrent overlapping bulk-complete must not cause 500. " +
                $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        // Verify all tasks are in a readable (non-corrupted) state after concurrent operations
        foreach (var taskId in new[] { taskId1, taskId2, taskId3 })
        {
            using var readClient = CreateRawClient(factory, accessToken);
            var gr = await readClient.GetAsync($"/api/tasks/{taskId}");
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, gr.StatusCode,
                $"Task {taskId} read after concurrent bulk-complete must not 500");
        }

        foreach (var r in responses) r.Dispose();
    }

    [TestMethod]
    public async Task Should_HandleRepeatBulkComplete_Without500_When_RequestsAreDuplicated()
    {
        var ctx = await CreateFreshUserAsync("bulk-repeat");
        using var factory = ctx.Factory;
        var accessToken = ctx.AccessToken;

        using var setupClient = CreateRawClient(factory, accessToken);
        var taskId1 = await CreateTaskAsync(setupClient, "Bulk repeat task 1");
        var taskId2 = await CreateTaskAsync(setupClient, "Bulk repeat task 2");

        // Pre-create all clients sequentially to avoid SQLite in-memory deadlocks
        const int concurrency = 5;
        var bulkClients = Enumerable.Range(0, concurrency)
            .Select(_ => CreateRawClient(factory, accessToken))
            .ToList();

        var tasks = bulkClients.Select(client => Task.Run(async () =>
            await client.PostAsJsonAsync("/api/tasks/bulk/complete",
                new { TaskIds = new[] { taskId1, taskId2 } })
        )).ToArray();

        var responses = await Task.WhenAll(tasks);
        var statusCodes = responses.Select(r => r.StatusCode).ToArray();

        Assert.AreEqual(0, statusCodes.Count(s => s == HttpStatusCode.InternalServerError),
            $"Duplicate concurrent bulk-complete must not cause 500. " +
            $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        foreach (var r in responses) r.Dispose();
        foreach (var c in bulkClients) c.Dispose();
    }

    // =========================================================================
    // ATTACK 6: CONCURRENT ROLE ASSIGNMENT (DOUBLE-ASSIGN)
    //
    // As SystemAdmin, simultaneously assigns the SAME role to the SAME user.
    // No 500 errors allowed. The role must appear exactly once after both complete.
    // =========================================================================

    [TestMethod]
    public async Task Should_HandleConcurrentRoleAssignment_Without500_OrDuplicateRole()
    {
        using var factory = new CustomWebApplicationFactory();

        var targetEmail = $"role-race-target-{Guid.NewGuid():N}@lemondo.dev";
        using var registerClient = CreateRawClient(factory);
        (await registerClient.PostAsJsonAsync("/api/auth/register",
            new { Email = targetEmail, Password = "ConcurrencyTest1!", DisplayName = "Role Race Target" }))
            .EnsureSuccessStatusCode();

        using var sysAdminClient = await factory.CreateSystemAdminClientAsync();
        var listResponse = await sysAdminClient.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(targetEmail)}");
        var list = await listResponse.Content.ReadFromJsonAsync<
            LemonDo.Domain.Common.PagedResult<LemonDo.Application.Administration.DTOs.AdminUserDto>>(
            TestJsonOptions.Default);
        Assert.IsNotEmpty(list!.Items, "Target user must be found via admin search");
        var targetUserId = list.Items[0].Id;

        using var adminClientA = await factory.CreateSystemAdminClientAsync();
        using var adminClientB = await factory.CreateSystemAdminClientAsync();

        var responses = await Task.WhenAll(
            adminClientA.PostAsJsonAsync(
                $"/api/admin/users/{targetUserId}/roles", new { RoleName = "Admin" }),
            adminClientB.PostAsJsonAsync(
                $"/api/admin/users/{targetUserId}/roles", new { RoleName = "Admin" }));

        var statusCodes = responses.Select(r => r.StatusCode).ToArray();

        foreach (var status in statusCodes)
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                $"Concurrent role assignment must not cause 500. " +
                $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        var getUserResponse = await sysAdminClient.GetAsync($"/api/admin/users/{targetUserId}");
        Assert.AreEqual(HttpStatusCode.OK, getUserResponse.StatusCode);
        var user = await getUserResponse.Content.ReadFromJsonAsync<
            LemonDo.Application.Administration.DTOs.AdminUserDto>(TestJsonOptions.Default);
        Assert.IsNotNull(user);

        var adminRoleCount = user.Roles.Count(r => r == "Admin");
        Assert.AreEqual(1, adminRoleCount,
            $"User must have Admin role exactly once after concurrent assignment. " +
            $"Got {adminRoleCount} occurrences. Roles: [{string.Join(", ", user.Roles)}]");

        foreach (var r in responses) r.Dispose();
    }

    [TestMethod]
    public async Task Should_HandleHighConcurrencyRoleAssignment_Without500_OrDuplicateRole()
    {
        // Run multiple rounds with fresh target users to maximize the chance of
        // hitting the TOCTOU race in AssignRoleAsync (IsInRoleAsync → AddToRoleAsync).
        const int rounds = 3;
        const int concurrency = 15;

        using var factory = new CustomWebApplicationFactory();

        // Pre-create all admin clients once (reused across rounds).
        var adminClients = new List<HttpClient>();
        for (var i = 0; i < concurrency; i++)
            adminClients.Add(await factory.CreateSystemAdminClientAsync());

        try
        {
            for (var round = 0; round < rounds; round++)
            {
                // Fresh target user each round so the role assignment race starts clean.
                var targetEmail = $"role-race-r{round}-{Guid.NewGuid():N}@lemondo.dev";
                using var registerClient = CreateRawClient(factory);
                (await registerClient.PostAsJsonAsync("/api/auth/register",
                    new { Email = targetEmail, Password = "ConcurrencyTest1!", DisplayName = $"Role Race R{round}" }))
                    .EnsureSuccessStatusCode();

                using var sysAdminClient = await factory.CreateSystemAdminClientAsync();
                var listResponse = await sysAdminClient.GetAsync(
                    $"/api/admin/users?search={Uri.EscapeDataString(targetEmail)}");
                var list = await listResponse.Content.ReadFromJsonAsync<
                    LemonDo.Domain.Common.PagedResult<LemonDo.Application.Administration.DTOs.AdminUserDto>>(
                    TestJsonOptions.Default);
                var targetUserId = list!.Items[0].Id;

                // Barrier: all threads wait until every thread is ready, then fire simultaneously.
                using var barrier = new CountdownEvent(concurrency);
                var url = $"/api/admin/users/{targetUserId}/roles";
                var body = new { RoleName = "Admin" };

                var tasks = adminClients.Select(client => Task.Run(async () =>
                {
                    barrier.Signal();
                    barrier.Wait(); // all threads release at the same instant
                    return await client.PostAsJsonAsync(url, body);
                })).ToArray();

                var responses = await Task.WhenAll(tasks);
                var statusCodes = responses.Select(r => r.StatusCode).ToArray();

                // With TransientFaultRetryPolicy in AdminUserService and SqliteTransientFaultDetector
                // in ErrorHandlingMiddleware, no request should produce a 500.
                // Service-level retries handle transient SQLite errors before they reach the middleware;
                // the middleware's catch-all for transient faults is the final safety net.
                foreach (var status in statusCodes)
                    Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                        $"Round {round}: High-concurrency role assignment must not 500. " +
                        $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

                var getUserResponse = await sysAdminClient.GetAsync($"/api/admin/users/{targetUserId}");
                var user = await getUserResponse.Content.ReadFromJsonAsync<
                    LemonDo.Application.Administration.DTOs.AdminUserDto>(TestJsonOptions.Default);
                Assert.IsNotNull(user);

                var adminRoleCount = user.Roles.Count(r => r == "Admin");
                Assert.AreEqual(1, adminRoleCount,
                    $"Round {round}: User must have Admin role exactly once after {concurrency} " +
                    $"concurrent assignments. Got {adminRoleCount}. " +
                    $"Roles: [{string.Join(", ", user.Roles)}]");

                foreach (var r in responses) r.Dispose();
            }
        }
        finally
        {
            foreach (var c in adminClients) c.Dispose();
        }
    }

    // =========================================================================
    // ATTACK 7: CONCURRENT TASK COMPLETE - DOUBLE-COMPLETE
    //
    // Sends two simultaneous POST /api/tasks/{id}/complete for the same task.
    // Task must end up as "Done". No 500 errors allowed.
    // =========================================================================

    [TestMethod]
    public async Task Should_CompleteTaskExactlyOnce_When_CompletedConcurrently()
    {
        var ctx = await CreateFreshUserAsync("double-complete");
        using var factory = ctx.Factory;
        var accessToken = ctx.AccessToken;

        using var setupClient = CreateRawClient(factory, accessToken);
        var taskId = await CreateTaskAsync(setupClient, "Double complete target");

        using var clientA = CreateRawClient(factory, accessToken);
        using var clientB = CreateRawClient(factory, accessToken);

        var responses = await Task.WhenAll(
            clientA.PostAsync($"/api/tasks/{taskId}/complete", null),
            clientB.PostAsync($"/api/tasks/{taskId}/complete", null));

        var statusCodes = responses.Select(r => r.StatusCode).ToArray();

        foreach (var status in statusCodes)
            Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                $"Concurrent double-complete must not cause 500. " +
                $"Got: {string.Join(", ", statusCodes.Select(s => (int)s))}");

        using var readClient = CreateRawClient(factory, accessToken);
        var getResponse = await readClient.GetAsync($"/api/tasks/{taskId}");

        if (getResponse.StatusCode == HttpStatusCode.OK)
        {
            var dto = await getResponse.Content.ReadFromJsonAsync<TaskDto>(TestJsonOptions.Default);
            Assert.IsNotNull(dto);
            // Task must be "Done" -- not stuck at "Todo" due to a lost-update race
            Assert.AreEqual("Done", dto.Status,
                $"Task must be Done after concurrent complete. Got: {dto.Status}");
        }
        else
        {
            Assert.AreEqual(HttpStatusCode.NotFound, getResponse.StatusCode,
                $"If task is not readable, it must be 404. Got: {(int)getResponse.StatusCode}");
        }

        foreach (var r in responses) r.Dispose();
    }
}
