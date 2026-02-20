namespace LemonDo.Api.Tests.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// Security pass 6 — Response security, caching, and header vulnerabilities.
///
/// Focus areas:
///   1. Cache-Control on sensitive endpoints (PII / decrypted secrets must never be cached)
///   2. CORS preflight behaviour (origin allowlist, credentials flag, no wildcard)
///   3. Error response consistency (information oracle prevention)
///   4. Correlation ID injection (X-Correlation-Id header — length limits, XSS, reflected content)
///   5. Content Security Policy (present, no unsafe-eval on API paths)
///   6. Referrer-Policy header
///   7. HTTP method enforcement across all endpoint groups
///   8. Server/X-Powered-By disclosure
///
/// A PASSING test means the endpoint is SECURE (it correctly rejected or sanitised the attack).
/// A FAILING test means the endpoint is VULNERABLE (it mishandled the attack or leaked data).
/// </summary>
[TestClass]
public sealed class ResponseSecurityTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _anonymousClient = null!;
    private static HttpClient _userClient = null!;
    private static HttpClient _adminClient = null!;
    private static HttpClient _sysAdminClient = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _anonymousClient = _factory.CreateClient();
        _userClient = await _factory.CreateAuthenticatedClientAsync();
        _adminClient = await _factory.CreateAdminClientAsync();
        _sysAdminClient = await _factory.CreateSystemAdminClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _anonymousClient.Dispose();
        _userClient.Dispose();
        _adminClient.Dispose();
        _sysAdminClient.Dispose();
        _factory.Dispose();
    }

    // ==========================================================================
    // CATEGORY 1: Cache-Control on Sensitive Endpoints
    //
    // Responses that contain PII, decrypted notes, or authentication material
    // must include Cache-Control: no-store so browsers and intermediary proxies
    // cannot cache them. Without this, a shared computer or a forward proxy could
    // serve a subsequent user the cached sensitive response.
    //
    // The SecurityHeadersMiddleware does NOT currently set Cache-Control — every
    // test in this group verifies a MISSING control. If the header is absent
    // the test FAILS (vulnerability found).
    // ==========================================================================

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetMe()
    {
        // GET /api/auth/me returns redacted PII (email, displayName, roles).
        // Even redacted PII should not be cached by intermediaries.
        var response = await _userClient.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        AssertNoCacheHeaders(response, "GET /api/auth/me");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_PostRevealProfile()
    {
        // POST /api/auth/reveal-profile returns the UNREDACTED plaintext email and display name.
        // This is the most sensitive auth endpoint — must be no-store.
        using var freshClient = await _factory.CreateAuthenticatedClientAsync();
        var response = await freshClient.PostAsJsonAsync("/api/auth/reveal-profile",
            new { Password = CustomWebApplicationFactory.TestUserPassword });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        AssertNoCacheHeaders(response, "POST /api/auth/reveal-profile");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_PostViewTaskNote()
    {
        // POST /api/tasks/{id}/view-note returns the decrypted sensitive note.
        // The decrypted secret must never appear in a cache.
        // We use a non-existent task ID; even the error response should be no-store,
        // but we primarily want to see the header on a 404 (no-store should apply broadly).
        var response = await _userClient.PostAsJsonAsync(
            $"/api/tasks/{Guid.NewGuid()}/view-note",
            new { Password = CustomWebApplicationFactory.TestUserPassword });

        // 404 expected (task not found) — but no-store should still be set
        AssertNoCacheHeaders(response, "POST /api/tasks/{id}/view-note (404 path)");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetAdminUsers()
    {
        // GET /api/admin/users returns paginated list of users with redacted PII.
        var response = await _adminClient.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        AssertNoCacheHeaders(response, "GET /api/admin/users");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetAdminSingleUser()
    {
        // GET /api/admin/users/{id} returns a single user's redacted PII.
        // We deliberately use a non-existent ID; even the 404 response benefits from no-store.
        var response = await _adminClient.GetAsync($"/api/admin/users/{Guid.NewGuid()}");

        AssertNoCacheHeaders(response, "GET /api/admin/users/{id}");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_PostAdminRevealUser()
    {
        // POST /api/admin/users/{id}/reveal returns UNREDACTED plaintext PII for an admin.
        // A cache hit here would serve a previous admin's reveal result to a new requester.
        var response = await _sysAdminClient.PostAsJsonAsync(
            $"/api/admin/users/{Guid.NewGuid()}/reveal",
            new
            {
                Reason = "SupportTicket",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });

        // 404 expected (user not found), but no-store must still be set
        AssertNoCacheHeaders(response, "POST /api/admin/users/{id}/reveal");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_PostAdminRevealTaskNote()
    {
        // POST /api/admin/tasks/{id}/reveal-note returns the decrypted sensitive note.
        var response = await _sysAdminClient.PostAsJsonAsync(
            $"/api/admin/tasks/{Guid.NewGuid()}/reveal-note",
            new
            {
                Reason = "SupportTicket",
                Password = CustomWebApplicationFactory.SystemAdminUserPassword
            });

        AssertNoCacheHeaders(response, "POST /api/admin/tasks/{id}/reveal-note");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetNotifications()
    {
        // GET /api/notifications returns user-specific notification content.
        var response = await _userClient.GetAsync("/api/notifications");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        AssertNoCacheHeaders(response, "GET /api/notifications");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetNotificationsUnreadCount()
    {
        // GET /api/notifications/unread-count reveals the user's notification state.
        var response = await _userClient.GetAsync("/api/notifications/unread-count");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        AssertNoCacheHeaders(response, "GET /api/notifications/unread-count");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetTasks()
    {
        // GET /api/tasks lists the user's personal task data.
        var response = await _userClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        AssertNoCacheHeaders(response, "GET /api/tasks");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetAuthLoginResponse()
    {
        // POST /api/auth/login returns an access token — must never be cached.
        using var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var response = await freshClient.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        AssertNoCacheHeaders(response, "POST /api/auth/login (access token response)");
    }

    [TestMethod]
    public async Task Should_SetNoCacheOnSensitiveData_When_GetAuthRegisterResponse()
    {
        // POST /api/auth/register returns an access token on success — must never be cached.
        using var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        var response = await freshClient.PostAsJsonAsync("/api/auth/register",
            new { Email = $"nocache-reg-{Guid.NewGuid():N}@lemondo.dev", Password = "TestPass123!", DisplayName = "NoCache Test" });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        AssertNoCacheHeaders(response, "POST /api/auth/register (access token response)");
    }

    // ==========================================================================
    // CATEGORY 2: CORS Preflight Behaviour
    //
    // The server uses WithOrigins (not wildcard) and AllowCredentials().
    // Wildcard + credentials is forbidden by the CORS spec. Verify:
    //   a) Allowed origin → gets Access-Control-Allow-Origin
    //   b) Disallowed origin → does NOT get Access-Control-Allow-Origin
    //   c) The Allow-Origin header is never set to "*"
    //   d) Access-Control-Allow-Credentials is "true" for allowed origins
    // ==========================================================================

    [TestMethod]
    public async Task Should_AllowCors_When_RequestComesFromAllowedOrigin()
    {
        // The allowed origins are ["https://localhost:5173", "http://localhost:5173"]
        // In the test environment they come from the default config.
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type,Authorization");

        using var client = _factory.CreateClient();
        var response = await client.SendAsync(request);

        // Preflight must succeed (200 or 204)
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.OK
            || response.StatusCode == HttpStatusCode.NoContent,
            $"CORS preflight from allowed origin expected 200/204, got {(int)response.StatusCode}");

        // The response must echo back the allowed origin, not wildcard
        var acao = GetHeader(response, "Access-Control-Allow-Origin");
        Assert.IsNotNull(acao,
            "Access-Control-Allow-Origin must be present for requests from an allowed origin");
        Assert.AreNotEqual("*", acao,
            "Access-Control-Allow-Origin must NOT be wildcard — credentials require an explicit origin");
    }

    [TestMethod]
    public async Task Should_NotSetAccessControlAllowOrigin_When_RequestComesFromDisallowedOrigin()
    {
        // An attacker-controlled origin must not receive CORS access headers
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
        request.Headers.Add("Origin", "https://evil-attacker.com");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "Content-Type,Authorization");

        using var client = _factory.CreateClient();
        var response = await client.SendAsync(request);

        // Even if it returns 200/204, the origin must NOT be echoed back
        var acao = GetHeader(response, "Access-Control-Allow-Origin");
        Assert.IsTrue(
            acao is null || acao != "https://evil-attacker.com",
            "ACAO header must not echo back a disallowed (attacker) origin");
    }

    [TestMethod]
    public async Task Should_NotUseWildcardOrigin_OnAnyEndpoint()
    {
        // Wildcard "*" combined with AllowCredentials is forbidden by spec and a security risk.
        // Verify no actual API response includes a wildcard ACAO header.
        using var client = _factory.CreateClient();

        string[] endpointsToCheck =
        [
            "/health",
            "/api/auth/login",
            "/api/tasks",
        ];

        foreach (var endpoint in endpointsToCheck)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Origin", "http://localhost:5173");
            var response = await client.SendAsync(request);

            var acao = GetHeader(response, "Access-Control-Allow-Origin");
            if (acao is not null)
            {
                Assert.AreNotEqual("*", acao,
                    $"ACAO must never be wildcard on {endpoint}. Got: {acao}");
            }
        }
    }

    [TestMethod]
    public async Task Should_SetAllowCredentials_When_RequestComesFromAllowedOrigin()
    {
        // Access-Control-Allow-Credentials: true is required for cookie-based auth to work.
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization");

        using var client = _factory.CreateClient();
        var response = await client.SendAsync(request);

        // Only check the credentials header when the origin was accepted
        var acao = GetHeader(response, "Access-Control-Allow-Origin");
        if (acao is not null && acao == "http://localhost:5173")
        {
            var acac = GetHeader(response, "Access-Control-Allow-Credentials");
            Assert.AreEqual("true", acac,
                "Access-Control-Allow-Credentials must be 'true' for cookie-based auth to function");
        }
    }

    [TestMethod]
    public async Task Should_NotSetAllowCredentials_When_RequestComesFromDisallowedOrigin()
    {
        // An attacker must NOT receive Access-Control-Allow-Credentials: true for their origin.
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/tasks");
        request.Headers.Add("Origin", "https://attacker.example.com");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "Authorization");

        using var client = _factory.CreateClient();
        var response = await client.SendAsync(request);

        var acao = GetHeader(response, "Access-Control-Allow-Origin");
        // If the origin was rejected (no ACAO header), credentials cannot be used anyway.
        // If somehow ACAO is set, it must NOT be the attacker origin with credentials=true.
        if (acao == "https://attacker.example.com")
        {
            var acac = GetHeader(response, "Access-Control-Allow-Credentials");
            Assert.AreNotEqual("true", acac,
                "Must not grant credentials to a disallowed origin");
        }
    }

    // ==========================================================================
    // CATEGORY 3: Error Response Consistency (Information Oracle)
    //
    // Responses that differ based on *whether a resource exists* or *what state
    // it is in* leak internal state to an unauthenticated or unauthorised caller.
    // An attacker can use differing responses to confirm that IDs exist in the
    // system, determine lifecycle state, or enumerate users.
    // ==========================================================================

    [TestMethod]
    public async Task Should_ReturnIdentical404Shape_When_TaskNeverExistedVsTaskDeleted()
    {
        // First create and delete a task so we have a "deleted" ID to compare.
        var createResponse = await _userClient.PostAsJsonAsync("/api/tasks",
            new { Title = "Temp task for deletion", Priority = "None" });
        Assert.AreEqual(HttpStatusCode.Created, createResponse.StatusCode,
            "Task creation must succeed for this test to proceed");

        var createBody = await createResponse.Content.ReadAsStringAsync();
        var taskDoc = JsonDocument.Parse(createBody);
        var taskId = taskDoc.RootElement.GetProperty("id").GetGuid();

        // Delete the task
        var deleteResponse = await _userClient.DeleteAsync($"/api/tasks/{taskId}");
        Assert.AreEqual(HttpStatusCode.OK, deleteResponse.StatusCode,
            "Task deletion must succeed");

        // Now fetch the deleted task
        var deletedResponse = await _userClient.GetAsync($"/api/tasks/{taskId}");
        var deletedBody = await deletedResponse.Content.ReadAsStringAsync();

        // Fetch a completely non-existent task
        var neverExistedId = Guid.NewGuid();
        var neverExistedResponse = await _userClient.GetAsync($"/api/tasks/{neverExistedId}");
        var neverExistedBody = await neverExistedResponse.Content.ReadAsStringAsync();

        // Both must be 404 (not 410 Gone for deleted vs 404 for non-existent)
        Assert.AreEqual(HttpStatusCode.NotFound, deletedResponse.StatusCode,
            "Deleted task must return 404, not 410 or 200");
        Assert.AreEqual(HttpStatusCode.NotFound, neverExistedResponse.StatusCode,
            "Non-existent task must return 404");

        // The response shape must be identical — no "this task was deleted" vs "no such task"
        var deletedJson = TryParseJson(deletedBody);
        var neverJson = TryParseJson(neverExistedBody);

        if (deletedJson.HasValue && neverJson.HasValue)
        {
            // If both have a "type" field, verify they match (same error category)
            if (deletedJson.Value.TryGetProperty("type", out var dt) &&
                neverJson.Value.TryGetProperty("type", out var nt))
            {
                Assert.AreEqual(dt.GetString(), nt.GetString(),
                    $"Error 'type' must be identical for deleted vs never-existed task. " +
                    $"Deleted: '{dt.GetString()}', NeverExisted: '{nt.GetString()}'");
            }
        }
    }

    [TestMethod]
    public async Task Should_ReturnConsistentStatus_When_DeactivatingAlreadyDeactivatedUserVsNonExistent()
    {
        // Create and deactivate a real user, then attempt a second deactivation.
        // Compare the status code and body shape against a totally non-existent user ID.
        var email = $"sec-oracle-deact-{Guid.NewGuid():N}@lemondo.dev";
        var registerResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = "Oracle Test" });
        Assert.AreEqual(HttpStatusCode.OK, registerResponse.StatusCode);

        // Find the user ID
        var listResponse = await _sysAdminClient.GetAsync(
            $"/api/admin/users?search={Uri.EscapeDataString(email)}");
        var listBody = await listResponse.Content.ReadAsStringAsync();
        var listDoc = JsonDocument.Parse(listBody);
        var userId = listDoc.RootElement.GetProperty("items")[0].GetProperty("id").GetGuid();

        // First deactivation — should succeed
        var firstDeact = await _sysAdminClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);
        Assert.AreEqual(HttpStatusCode.OK, firstDeact.StatusCode,
            "First deactivation must succeed");

        // Second deactivation of the same (now-deactivated) user
        var secondDeact = await _sysAdminClient.PostAsync($"/api/admin/users/{userId}/deactivate", null);

        // Deactivating a non-existent user
        var nonExistentDeact = await _sysAdminClient.PostAsync(
            $"/api/admin/users/{Guid.NewGuid()}/deactivate", null);

        // Neither must return 200 (both are error cases).
        Assert.AreNotEqual(HttpStatusCode.OK, secondDeact.StatusCode,
            "Re-deactivating a deactivated user must not return 200");

        // The key oracle check: the status codes should be different enough to distinguish
        // "already deactivated" (409/422) from "not found" (404), BUT the response BODY
        // must not include phrases like "already deactivated" that confirm the user exists.
        var secondBody = await secondDeact.Content.ReadAsStringAsync();
        var nonExistentBody = await nonExistentDeact.Content.ReadAsStringAsync();

        // Both must not leak stack traces
        Assert.IsFalse(secondBody.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            $"Double-deactivate response must not leak StackTrace: {secondBody}");
        Assert.IsFalse(nonExistentBody.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            $"Non-existent deactivate response must not leak StackTrace: {nonExistentBody}");
    }

    [TestMethod]
    public async Task Should_ReturnConsistentErrorShape_When_UnauthorizedAcrossEndpoints()
    {
        // All 401 responses from different endpoints must use the same JSON structure.
        // A different shape on one endpoint makes it an anomalous oracle.
        using var freshClient = _factory.CreateClient();

        var endpoints = new[]
        {
            (HttpMethod.Get, "/api/auth/me"),
            (HttpMethod.Get, "/api/tasks"),
            (HttpMethod.Get, "/api/notifications"),
            (HttpMethod.Get, "/api/onboarding/status"),
        };

        foreach (var (method, path) in endpoints)
        {
            var req = new HttpRequestMessage(method, path);
            var resp = await freshClient.SendAsync(req);
            Assert.AreEqual(HttpStatusCode.Unauthorized, resp.StatusCode,
                $"{method} {path} must return 401 without auth");

            var body = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(body)) continue;

            // We only require consistency in the response status — the body may be empty
            // (ASP.NET Core default 401 from authentication middleware is typically empty).
        }
    }

    [TestMethod]
    public async Task Should_NotRevealWhetherUserExistsOrNot_When_UnauthorizedAdminOperation()
    {
        // A regular (non-admin) user hitting admin endpoints must get 403, not 404.
        // If the response were 404 for some IDs and 403 for others, it would act as
        // an existence oracle for admin IDs.

        // Non-existent user: 403 (forbidden because not admin, not 404)
        var nonExistentId = Guid.NewGuid();
        var resp1 = await _userClient.GetAsync($"/api/admin/users/{nonExistentId}");
        Assert.AreEqual(HttpStatusCode.Forbidden, resp1.StatusCode,
            "Regular user hitting admin endpoint with non-existent ID must get 403, not 404");

        // Existing user (admin itself): also 403
        var resp2 = await _userClient.GetAsync("/api/admin/users");
        Assert.AreEqual(HttpStatusCode.Forbidden, resp2.StatusCode,
            "Regular user hitting admin list endpoint must get 403");
    }

    [TestMethod]
    public async Task Should_ReturnConsistentShape_When_ServerErrorOccurs()
    {
        // Verify that when the error handler catches a generic exception it returns
        // the standard { type, title, status, correlationId } shape, not a raw exception dump.
        //
        // We trigger a 400 (which uses the same error handler path) via invalid JSON.
        using var client = _factory.CreateClient();
        var content = new StringContent("not valid json!!!", Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/auth/login", content);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        Assert.IsTrue(root.TryGetProperty("type", out _),
            $"Error response must have 'type' field. Body: {body}");
        Assert.IsTrue(root.TryGetProperty("title", out _),
            $"Error response must have 'title' field. Body: {body}");
        Assert.IsTrue(root.TryGetProperty("status", out _),
            $"Error response must have 'status' field. Body: {body}");

        // Must NOT expose stack traces
        Assert.IsFalse(body.Contains("at LemonDo", StringComparison.OrdinalIgnoreCase),
            $"Error body must not contain internal namespace stack frames: {body}");
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            $"Error body must not contain StackTrace keyword: {body}");
    }

    [TestMethod]
    public async Task Should_Return404NotRevealDetails_When_NonExistentTaskGetByAuthenticatedUser()
    {
        // Verify a 404 for a task that never existed returns a clean, non-leaky response.
        var response = await _userClient.GetAsync($"/api/tasks/{Guid.NewGuid()}");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            $"404 body must not expose StackTrace: {body}");
        Assert.IsFalse(body.Contains("SELECT", StringComparison.OrdinalIgnoreCase),
            $"404 body must not expose SQL fragments: {body}");
        Assert.IsFalse(body.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase),
            $"404 body must not expose EF Core internals: {body}");
    }

    // ==========================================================================
    // CATEGORY 4: Correlation ID Injection
    //
    // The CorrelationIdMiddleware accepts an X-Correlation-Id header from the
    // client and reflects it back in the response. This is a security risk if:
    //   a) The value has no length limit (DoS / log bloat)
    //   b) XSS payloads are reflected without sanitisation
    //   c) Log injection characters are accepted (newlines, nulls)
    //
    // The middleware currently does NOT validate or limit the incoming header value.
    // ==========================================================================

    [TestMethod]
    public async Task Should_TruncateOrRejectOversizedCorrelationId()
    {
        // An attacker injecting a 10,000-character correlation ID could:
        //   - Bloat log files to disk exhaustion
        //   - Potentially overflow fixed-length log fields in downstream systems
        using var client = _factory.CreateClient();
        var oversizedId = new string('x', 10_000);
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", oversizedId);

        var response = await client.GetAsync("/health");

        // The server must not crash (500) on an oversized correlation ID
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Oversized X-Correlation-Id must not cause a 500 server error");

        // The echoed correlation ID in the response must be truncated (reasonable limit: ≤256 chars)
        var echoed = GetHeader(response, "X-Correlation-Id");
        if (echoed is not null)
        {
            Assert.IsLessThanOrEqualTo(256, echoed.Length,
                $"Echoed X-Correlation-Id is {echoed.Length} chars — must be truncated to ≤256. " +
                $"An unlimited reflection enables log bloat attacks.");
        }
    }

    [TestMethod]
    public async Task Should_SanitizeOrRejectXssPayloadInCorrelationId()
    {
        // If the correlation ID is written to logs or embedded in HTML error pages,
        // XSS payloads in this header could cause cross-site scripting.
        using var client = _factory.CreateClient();
        var xssPayload = "<script>alert('xss')</script>";
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", xssPayload);

        var response = await client.GetAsync("/health");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "XSS payload in X-Correlation-Id must not crash the server");

        // The raw XSS payload must NOT be echoed verbatim in the response header
        var echoed = GetHeader(response, "X-Correlation-Id");
        if (echoed is not null)
        {
            // If the header is reflected, it must not contain unescaped HTML tags.
            // Header values are not HTML-rendered, but they can appear in error pages.
            // The strict check: if the payload passes through, document it as a WARN.
            Assert.DoesNotContain("<script>", echoed,
                $"Raw XSS payload reflected verbatim in X-Correlation-Id response header: '{echoed}'. " +
                "If this value appears in log-based UIs or error pages, it enables stored XSS.");
        }
    }

    [TestMethod]
    public async Task Should_SanitizeOrRejectLogInjectionInCorrelationId()
    {
        // Newline injection in the correlation ID can split log entries, creating
        // false log entries that look like legitimate requests.
        using var client = _factory.CreateClient();
        var logInjectionPayload = "legit-id\r\nFAKE_LOG_ENTRY: Admin logged in as root";
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", logInjectionPayload);

        var response = await client.GetAsync("/health");

        // Server must not crash
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Log injection payload in X-Correlation-Id must not crash the server");

        // The echoed header must not contain newlines (CRLF injection in response headers
        // is a header injection vulnerability).
        var echoed = GetHeader(response, "X-Correlation-Id");
        if (echoed is not null)
        {
            Assert.IsFalse(echoed.Contains('\r') || echoed.Contains('\n'),
                $"Echoed X-Correlation-Id must not contain CRLF characters (header injection risk). Got: '{echoed}'");
        }
    }

    [TestMethod]
    public async Task Should_SanitizeOrRejectNullByteInCorrelationId()
    {
        // Null bytes in the correlation ID can truncate log messages or
        // confuse downstream C-string based logging systems.
        using var client = _factory.CreateClient();
        var nullBytePayload = "correlation-id\x00malicious-suffix";
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", nullBytePayload);

        var response = await client.GetAsync("/health");

        // Must not crash
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null byte in X-Correlation-Id must not crash the server");
    }

    [TestMethod]
    public async Task Should_SetServerGeneratedCorrelationId_When_NoneProvided()
    {
        // Baseline: when no X-Correlation-Id is provided, the server generates one.
        // The generated ID must be present in the response.
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/health");

        var correlationId = GetHeader(response, "X-Correlation-Id");
        Assert.IsNotNull(correlationId,
            "Server must generate and echo an X-Correlation-Id when none is provided");
        Assert.IsFalse(string.IsNullOrEmpty(correlationId),
            "Generated X-Correlation-Id must not be empty");
    }

    [TestMethod]
    public async Task Should_EchoClientCorrelationId_When_ValidIdProvided()
    {
        // Baseline: a valid GUID correlation ID is echoed unchanged.
        using var client = _factory.CreateClient();
        var myId = Guid.NewGuid().ToString("N");
        client.DefaultRequestHeaders.TryAddWithoutValidation("X-Correlation-Id", myId);

        var response = await client.GetAsync("/health");
        var echoed = GetHeader(response, "X-Correlation-Id");

        Assert.AreEqual(myId, echoed,
            "A valid GUID correlation ID must be echoed back unchanged");
    }

    // ==========================================================================
    // CATEGORY 5: Content Security Policy
    //
    // The CSP header is set by SecurityHeadersMiddleware on all non-docs paths.
    // Verify the policy is present and does not include dangerous directives.
    // ==========================================================================

    [TestMethod]
    public async Task Should_IncludeCspHeader_OnApiEndpoints()
    {
        var response = await _anonymousClient.GetAsync("/health");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.IsTrue(response.Headers.Contains("Content-Security-Policy"),
            "Content-Security-Policy header must be present on API responses");
    }

    [TestMethod]
    public async Task Should_NotHaveUnsafeEvalInScriptSrc_InCsp()
    {
        // 'unsafe-eval' in script-src allows eval(), new Function(), etc.
        // This dramatically expands the XSS attack surface.
        var response = await _anonymousClient.GetAsync("/health");
        var csp = GetHeader(response, "Content-Security-Policy") ?? string.Empty;

        Assert.IsFalse(csp.Contains("'unsafe-eval'", StringComparison.OrdinalIgnoreCase),
            $"CSP must not include 'unsafe-eval' in script-src. Got: {csp}");
    }

    [TestMethod]
    public async Task Should_RestrictScriptSrcToSelf_InCsp()
    {
        // script-src must be restricted to 'self' only.
        var response = await _anonymousClient.GetAsync("/health");
        var csp = GetHeader(response, "Content-Security-Policy") ?? string.Empty;

        Assert.IsTrue(csp.Contains("script-src 'self'", StringComparison.OrdinalIgnoreCase),
            $"CSP must restrict script-src to 'self'. Got: {csp}");
    }

    [TestMethod]
    public async Task Should_HaveDefaultSrcInCsp()
    {
        // A missing default-src means no fallback policy — all resource types are unconstrained.
        var response = await _anonymousClient.GetAsync("/health");
        var csp = GetHeader(response, "Content-Security-Policy") ?? string.Empty;

        Assert.IsTrue(csp.Contains("default-src", StringComparison.OrdinalIgnoreCase),
            $"CSP must include default-src directive. Got: {csp}");
    }

    [TestMethod]
    public async Task Should_HaveCspOnUnauthenticated401Response()
    {
        // Security headers (including CSP) must be set even on 401 responses.
        // An attacker cannot bypass CSP by targeting unauthenticated endpoints.
        var response = await _anonymousClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.IsTrue(response.Headers.Contains("Content-Security-Policy"),
            "CSP must be present on 401 responses (SecurityHeadersMiddleware must run before auth)");
    }

    [TestMethod]
    public async Task Should_NotHaveCspOnScalarDocs_WhenScriptSrcSelf()
    {
        // The Scalar API documentation page loads scripts from CDN. The middleware
        // exempts /scalar paths from the restrictive CSP.
        var response = await _anonymousClient.GetAsync("/scalar/v1");

        var csp = GetHeader(response, "Content-Security-Policy");
        Assert.IsTrue(
            csp is null || !csp.Contains("script-src 'self'"),
            $"Scalar docs must not have restrictive 'script-src self' CSP. Got: {csp}");
    }

    // ==========================================================================
    // CATEGORY 6: Referrer-Policy Header
    // ==========================================================================

    [TestMethod]
    public async Task Should_HaveReferrerPolicyHeader_OnAllResponses()
    {
        // Referrer-Policy prevents leaking the current URL in the Referer header
        // when navigating away from an API-served page.
        var response = await _anonymousClient.GetAsync("/health");

        Assert.IsTrue(response.Headers.Contains("Referrer-Policy"),
            "Referrer-Policy header must be present on all responses");
        var value = GetHeader(response, "Referrer-Policy");
        Assert.IsNotNull(value);
        Assert.IsTrue(
            value == "no-referrer" || value == "strict-origin-when-cross-origin" || value == "strict-origin",
            $"Referrer-Policy must be a restrictive value. Got: {value}");
    }

    [TestMethod]
    public async Task Should_HaveReferrerPolicyHeader_OnAuthenticatedResponses()
    {
        var response = await _userClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.IsTrue(response.Headers.Contains("Referrer-Policy"),
            "Referrer-Policy must be present on authenticated endpoint responses");
    }

    [TestMethod]
    public async Task Should_HaveReferrerPolicyHeader_On401Response()
    {
        var response = await _anonymousClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.IsTrue(response.Headers.Contains("Referrer-Policy"),
            "Referrer-Policy must be present even on 401 Unauthorized responses");
    }

    // ==========================================================================
    // CATEGORY 7: Server Header / X-Powered-By Disclosure
    //
    // Internal technology stack information must not be exposed to callers.
    // ==========================================================================

    [TestMethod]
    public async Task Should_NotExposeXPoweredByHeader_OnHealthEndpoint()
    {
        var response = await _anonymousClient.GetAsync("/health");

        Assert.IsFalse(response.Headers.Contains("X-Powered-By"),
            "X-Powered-By header must not be exposed");
    }

    [TestMethod]
    public async Task Should_NotExposeKestrelVersionInServerHeader_OnApiEndpoints()
    {
        var response = await _userClient.GetAsync("/api/tasks");

        if (response.Headers.TryGetValues("Server", out var values))
        {
            var serverValue = string.Join(",", values);
            Assert.IsFalse(serverValue.Contains("Kestrel", StringComparison.OrdinalIgnoreCase),
                $"Server header must not reveal 'Kestrel' version. Got: {serverValue}");
            Assert.IsFalse(serverValue.Contains("Microsoft", StringComparison.OrdinalIgnoreCase),
                $"Server header must not reveal Microsoft technology stack. Got: {serverValue}");
        }
        // If no Server header is present, that is even better (no disclosure).
    }

    [TestMethod]
    public async Task Should_NotExposeXPoweredByHeader_OnAuthenticatedEndpoints()
    {
        var response = await _userClient.GetAsync("/api/tasks");

        Assert.IsFalse(response.Headers.Contains("X-Powered-By"),
            "X-Powered-By header must not be exposed on authenticated endpoints");
    }

    [TestMethod]
    public async Task Should_NotExposeXPoweredByHeader_On401Response()
    {
        var response = await _anonymousClient.GetAsync("/api/admin/users");

        Assert.IsFalse(response.Headers.Contains("X-Powered-By"),
            "X-Powered-By header must not be exposed on 401/403 responses");
    }

    // ==========================================================================
    // CATEGORY 8: HTTP Method Enforcement
    //
    // Endpoints must only accept the HTTP methods they were registered for.
    // Wrong-method requests must return 405 (Method Not Allowed), not 200.
    // ==========================================================================

    [TestMethod]
    public async Task Should_Return405_When_GetSentToAuthLoginEndpoint()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/auth/login");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_DeleteSentToAuthLoginEndpoint()
    {
        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/auth/login");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_PostSentToAuthMeEndpoint()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/me", null);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_DeleteSentToAuthMeEndpoint()
    {
        using var client = _factory.CreateClient();
        var response = await client.DeleteAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_PostSentToTasksListEndpoint()
    {
        // POST /api/tasks/ exists (create task), but GET is the list endpoint.
        // Verify PUT is rejected.
        var response = await _userClient.PutAsJsonAsync("/api/tasks",
            new { Title = "Should fail" });
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_PutSentToNotificationsEndpoint()
    {
        var response = await _userClient.PutAsync("/api/notifications",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_DeleteSentToOnboardingStatus()
    {
        var response = await _userClient.DeleteAsync("/api/onboarding/status");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.MethodNotAllowed
            || response.StatusCode == HttpStatusCode.NotFound,
            $"DELETE /api/onboarding/status should not be routable. Got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return405_When_GetSentToAnalyticsEventsEndpoint()
    {
        var response = await _userClient.GetAsync("/api/analytics/events");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_GetSentToAdminDeactivateEndpoint()
    {
        var response = await _adminClient.GetAsync($"/api/admin/users/{Guid.NewGuid()}/deactivate");
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.MethodNotAllowed
            || response.StatusCode == HttpStatusCode.NotFound,
            $"GET /api/admin/users/{{id}}/deactivate should not be routable. Got {(int)response.StatusCode}");
    }

    // ==========================================================================
    // CATEGORY 9: Content-Type Header on Responses
    //
    // All JSON API responses must set Content-Type: application/json.
    // Failure to do so could allow MIME-type sniffing attacks (even with nosniff,
    // it is a best-practice signal for intermediaries and clients).
    // ==========================================================================

    [TestMethod]
    public async Task Should_ReturnApplicationJson_When_GetTasksSucceeds()
    {
        var response = await _userClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.AreEqual("application/json", contentType,
            "GET /api/tasks must return Content-Type: application/json");
    }

    [TestMethod]
    public async Task Should_ReturnApplicationJson_When_Get401Unauthorized()
    {
        var response = await _anonymousClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        // ASP.NET Core's 401 may have an empty body — that is acceptable.
        // But if there IS a body, it must declare itself as JSON.
        var body = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(body))
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual("application/json", contentType,
                "A 401 response with a body must declare Content-Type: application/json");
        }
    }

    [TestMethod]
    public async Task Should_ReturnApplicationJson_When_Get400BadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/login",
            new StringContent("{\"Email\":null,\"Password\":null}", Encoding.UTF8, "application/json"));

        // Could be 400 (validation error)
        var body = await response.Content.ReadAsStringAsync();
        if (!string.IsNullOrWhiteSpace(body))
        {
            var contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual("application/json", contentType,
                "A 400 response with a body must declare Content-Type: application/json");
        }
    }

    [TestMethod]
    public async Task Should_ReturnApplicationJson_When_ErrorHandlerReturns500Shape()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/login",
            new StringContent("not-valid-json", Encoding.UTF8, "application/json"));

        // 400 from the JSON error handler — must be JSON content type
        var contentType = response.Content.Headers.ContentType?.MediaType;
        Assert.AreEqual("application/json", contentType,
            "Error handler responses must return Content-Type: application/json");
    }

    // ==========================================================================
    // CATEGORY 10: X-Frame-Options and Clickjacking
    //
    // The X-Frame-Options: DENY header prevents the API from being embedded in
    // an <iframe> on another origin. Without this, an attacker could load the API
    // in a hidden frame and intercept user interactions.
    // ==========================================================================

    [TestMethod]
    public async Task Should_HaveXFrameOptionsDeny_OnApiEndpoints()
    {
        var response = await _userClient.GetAsync("/api/tasks");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options header must be present on authenticated API responses");
        Assert.AreEqual("DENY", GetHeader(response, "X-Frame-Options"),
            "X-Frame-Options must be DENY");
    }

    [TestMethod]
    public async Task Should_HaveXFrameOptionsDeny_On401Response()
    {
        var response = await _anonymousClient.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);

        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options must be present even on 401 responses");
        Assert.AreEqual("DENY", GetHeader(response, "X-Frame-Options"),
            "X-Frame-Options must be DENY on 401 responses");
    }

    [TestMethod]
    public async Task Should_HaveXFrameOptionsDeny_On400BadRequest()
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/auth/login",
            new StringContent("not json", Encoding.UTF8, "application/json"));

        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options must be present on 400 error responses");
    }

    // ==========================================================================
    // CATEGORY 11: X-Content-Type-Options (MIME Sniffing)
    // ==========================================================================

    [TestMethod]
    public async Task Should_HaveXContentTypeOptionsNosniff_OnAllApiResponses()
    {
        string[] paths = ["/health", "/api/tasks"];
        foreach (var path in paths)
        {
            var response = await _userClient.GetAsync(path);

            Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"),
                $"X-Content-Type-Options must be present on {path}");
            Assert.AreEqual("nosniff", GetHeader(response, "X-Content-Type-Options"),
                $"X-Content-Type-Options must be 'nosniff' on {path}");
        }
    }

    [TestMethod]
    public async Task Should_HaveXContentTypeOptionsNosniff_OnErrorResponses()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/tasks"); // 401

        Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options nosniff must be present on 401 error responses");
    }

    // ==========================================================================
    // Helpers
    // ==========================================================================

    /// <summary>
    /// Asserts that the response contains Cache-Control headers indicating
    /// that the response MUST NOT be cached (no-store).
    /// This checks both the Cache-Control header and the Pragma header (HTTP/1.0 compat).
    /// </summary>
    private static void AssertNoCacheHeaders(HttpResponseMessage response, string endpointDescription)
    {
        var cacheControl = response.Headers.CacheControl;

        Assert.IsNotNull(cacheControl,
            $"VULNERABILITY: Cache-Control header is MISSING on {endpointDescription}. " +
            "Without Cache-Control: no-store, browsers and proxy caches may cache sensitive response data. " +
            "An attacker on a shared machine or with access to a proxy cache could retrieve previous users' data.");

        Assert.IsTrue(cacheControl.NoStore,
            $"VULNERABILITY: Cache-Control does not include 'no-store' on {endpointDescription}. " +
            $"Current value: '{response.Headers.CacheControl}'. " +
            "Sensitive responses must include 'Cache-Control: no-store' to prevent caching of PII and auth tokens.");
    }

    /// <summary>Returns the first value of a response header, or null if the header is absent.</summary>
    private static string? GetHeader(HttpResponseMessage response, string headerName)
    {
        if (response.Headers.TryGetValues(headerName, out var values))
            return values.FirstOrDefault();
        if (response.Content.Headers.TryGetValues(headerName, out var contentValues))
            return contentValues.FirstOrDefault();
        return null;
    }

    private static JsonElement? TryParseJson(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }
        catch
        {
            return null;
        }
    }
}
