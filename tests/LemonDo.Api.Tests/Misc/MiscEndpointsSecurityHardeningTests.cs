namespace LemonDo.Api.Tests.Misc;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Endpoints;
using LemonDo.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

/// <summary>
/// Security hardening tests for NotificationEndpoints, OnboardingEndpoints,
/// AnalyticsEndpoints, and the unauthenticated /api/config endpoint.
///
/// A PASSING test means the endpoint is SECURE (it rejected the attack).
/// A FAILING test means the endpoint is VULNERABLE (it accepted the malicious request).
/// </summary>
[TestClass]
public sealed class MiscEndpointsSecurityHardeningTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _anonymousClient = null!;
    private static HttpClient _authenticatedClient = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _anonymousClient = _factory.CreateClient();
        _authenticatedClient = await _factory.CreateAuthenticatedClientAsync();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _anonymousClient.Dispose();
        _authenticatedClient.Dispose();
        _factory.Dispose();
    }

    // -------------------------------------------------------------------------
    // NOTIFICATIONS — Authentication Bypass
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_Return401_When_ListNotificationsWithNoToken()
    {
        var response = await _anonymousClient.GetAsync("/api/notifications");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_GetUnreadCountWithNoToken()
    {
        var response = await _anonymousClient.GetAsync("/api/notifications/unread-count");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_MarkAsReadWithNoToken()
    {
        var response = await _anonymousClient.PostAsync(
            $"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_MarkAllAsReadWithNoToken()
    {
        var response = await _anonymousClient.PostAsync("/api/notifications/read-all", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_ListNotificationsWithMalformedToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "this-is-not-a-valid-jwt-token");

        var response = await client.GetAsync("/api/notifications");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_MarkAsReadWithEmptyBearerToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");

        var response = await client.PostAsync(
            $"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_ListNotificationsWithWronglySignedToken()
    {
        // Craft a structurally valid JWT signed with a DIFFERENT key than the server expects
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            """{"alg":"HS256","typ":"JWT"}"""))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
            $$$"""{"sub":"{{{Guid.NewGuid()}}}","iss":"LemonDo","aud":"LemonDo","exp":9999999999}"""))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var fakeSignature = Convert.ToBase64String(
            Encoding.UTF8.GetBytes("wrongsignature"))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');

        var fakeToken = $"{header}.{payload}.{fakeSignature}";

        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", fakeToken);

        var response = await client.GetAsync("/api/notifications");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // NOTIFICATIONS — IDOR (Authorization / Access Control)
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_ReturnNotFound_When_UserBMarksUserANotificationAsRead()
    {
        // User A registers and gets their welcome notification
        var emailA = $"sec-idor-userA-{Guid.NewGuid():N}@lemondo.dev";
        using var clientA = await RegisterAndAuthenticateAsync(emailA);

        var listResponse = await clientA.GetAsync("/api/notifications");
        var list = await listResponse.Content.ReadFromJsonAsync<NotificationListResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(list);
        Assert.IsNotEmpty(list.Items, "User A must have at least one notification");
        var userANotificationId = list.Items[0].Id;

        // User B tries to mark user A's notification as read — should be blocked
        var emailB = $"sec-idor-userB-{Guid.NewGuid():N}@lemondo.dev";
        using var clientB = await RegisterAndAuthenticateAsync(emailB);

        var response = await clientB.PostAsync(
            $"/api/notifications/{userANotificationId}/read", null);

        // Expect 404 (ownership check: returns NotFound when notification doesn't belong to requester)
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_NotExposeOtherUsersNotifications_When_ListingNotifications()
    {
        // User A has a welcome notification
        var emailA = $"sec-list-userA-{Guid.NewGuid():N}@lemondo.dev";
        using var clientA = await RegisterAndAuthenticateAsync(emailA);

        var listA = await (await clientA.GetAsync("/api/notifications"))
            .Content.ReadFromJsonAsync<NotificationListResponse>(TestJsonOptions.Default);
        Assert.IsNotNull(listA);

        // User B's list must NOT contain user A's notification IDs
        var emailB = $"sec-list-userB-{Guid.NewGuid():N}@lemondo.dev";
        using var clientB = await RegisterAndAuthenticateAsync(emailB);

        var listB = await (await clientB.GetAsync("/api/notifications"))
            .Content.ReadFromJsonAsync<NotificationListResponse>(TestJsonOptions.Default);
        Assert.IsNotNull(listB);

        var userAIds = listA.Items.Select(n => n.Id).ToHashSet();
        var containsCrossUser = listB.Items.Any(n => userAIds.Contains(n.Id));

        Assert.IsFalse(containsCrossUser,
            "User B's notification list must not contain User A's notification IDs");
    }

    // -------------------------------------------------------------------------
    // NOTIFICATIONS — Input Validation
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_MarkAsReadWithNonGuidId()
    {
        var response = await _authenticatedClient.PostAsync(
            "/api/notifications/not-a-guid/read", null);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_MarkAsReadWithSqlInjectionInId()
    {
        // SQL injection attempts in the ID path segment
        var sqlPayload = Uri.EscapeDataString("'; DROP TABLE Notifications; --");
        var response = await _authenticatedClient.PostAsync(
            $"/api/notifications/{sqlPayload}/read", null);

        // Must be 400 (not a valid GUID) — not 500 (unhandled exception)
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_MarkAsReadWithXssPayloadInId()
    {
        var xssPayload = Uri.EscapeDataString("<script>alert('xss')</script>");
        var response = await _authenticatedClient.PostAsync(
            $"/api/notifications/{xssPayload}/read", null);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_MarkAsReadWithPathTraversalInId()
    {
        var traversalPayload = Uri.EscapeDataString("../../etc/passwd");
        var response = await _authenticatedClient.PostAsync(
            $"/api/notifications/{traversalPayload}/read", null);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_MarkAsReadWithTemplateInjectionInId()
    {
        var templatePayload = Uri.EscapeDataString("{{7*7}}");
        var response = await _authenticatedClient.PostAsync(
            $"/api/notifications/{templatePayload}/read", null);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_MarkAsReadWithOversizedId()
    {
        // 2000-character string — tests that the server doesn't crash on oversized route params
        var bigId = new string('x', 2000);
        var response = await _authenticatedClient.PostAsync(
            $"/api/notifications/{bigId}/read", null);

        // Must be 400 or 404, not 500
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.IsTrue(
            response.StatusCode == HttpStatusCode.BadRequest ||
            response.StatusCode == HttpStatusCode.NotFound,
            $"Expected 400 or 404, got {(int)response.StatusCode}");
    }

    // -------------------------------------------------------------------------
    // NOTIFICATIONS — Pagination Abuse
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_NotReturn500_When_ListNotificationsWithNegativePage()
    {
        var response = await _authenticatedClient.GetAsync("/api/notifications?page=-1&pageSize=20");

        // The repository does Skip((-1-1)*20) = Skip(-40) which EF Core may handle oddly.
        // We verify the endpoint does NOT 500 on this input — ideally it returns 400.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Negative page number must not cause an unhandled server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ListNotificationsWithZeroPageSize()
    {
        var response = await _authenticatedClient.GetAsync("/api/notifications?page=1&pageSize=0");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Zero pageSize must not cause an unhandled server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ListNotificationsWithHugePageSize()
    {
        // Attempt to force a massive DB read
        var response = await _authenticatedClient.GetAsync("/api/notifications?page=1&pageSize=1000000");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Extremely large pageSize must not cause an unhandled server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ListNotificationsWithNegativePageSize()
    {
        var response = await _authenticatedClient.GetAsync("/api/notifications?page=1&pageSize=-999");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Negative pageSize must not cause an unhandled server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_ListNotificationsWithMaxIntPage()
    {
        var response = await _authenticatedClient.GetAsync(
            $"/api/notifications?page={int.MaxValue}&pageSize=20");

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "MAX_INT page must not overflow and cause an unhandled server error");
    }

    // -------------------------------------------------------------------------
    // NOTIFICATIONS — Information Leakage
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_ReturnGenericError_When_MarkReadWithNonExistentGuid()
    {
        var response = await _authenticatedClient.PostAsync(
            $"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        // Response body must not contain stack traces or internal implementation details
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", body, $"Response must not leak exception details: {body}");
        Assert.DoesNotContain("StackTrace", body, $"Response must not leak stack trace: {body}");
        Assert.DoesNotContain("at LemonDo", body, $"Response must not leak internal namespace: {body}");
    }

    [TestMethod]
    public async Task Should_HaveSecurityHeaders_When_ListNotificationsAuthenticated()
    {
        var response = await _authenticatedClient.GetAsync("/api/notifications");

        Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options header must be present");
        Assert.AreEqual("nosniff",
            response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault());

        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options header must be present");
        Assert.AreEqual("DENY",
            response.Headers.GetValues("X-Frame-Options").FirstOrDefault());
    }

    [TestMethod]
    public async Task Should_NotExposeServerHeader_When_ListNotifications()
    {
        var response = await _authenticatedClient.GetAsync("/api/notifications");

        // ASP.NET Core does not add X-Powered-By by default, but verify it's absent
        Assert.IsFalse(response.Headers.Contains("X-Powered-By"),
            "X-Powered-By header must not be present");
    }

    // -------------------------------------------------------------------------
    // ONBOARDING — Authentication Bypass
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_Return401_When_GetOnboardingStatusWithNoToken()
    {
        var response = await _anonymousClient.GetAsync("/api/onboarding/status");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_CompleteOnboardingWithNoToken()
    {
        var response = await _anonymousClient.PostAsync("/api/onboarding/complete", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_GetOnboardingStatusWithMalformedToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJIUzI1NiJ9.INVALID.PAYLOAD");

        var response = await client.GetAsync("/api/onboarding/status");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_CompleteOnboardingWithEmptyBearerToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");

        var response = await client.PostAsync("/api/onboarding/complete", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // ONBOARDING — Business Logic / Idempotency
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_Return200_When_CompleteOnboardingCalledTwice()
    {
        var email = $"sec-onboard-idem-{Guid.NewGuid():N}@lemondo.dev";
        using var client = await RegisterAndAuthenticateAsync(email);

        var first = await client.PostAsync("/api/onboarding/complete", null);
        Assert.AreEqual(HttpStatusCode.OK, first.StatusCode);

        // Second call must not throw 500 or 409 — it should be idempotent
        var second = await client.PostAsync("/api/onboarding/complete", null);
        Assert.AreEqual(HttpStatusCode.OK, second.StatusCode,
            "Calling /complete twice must be idempotent (200, not 500/409)");
    }

    [TestMethod]
    public async Task Should_PreserveOriginalCompletedAt_When_CompleteOnboardingCalledTwice()
    {
        var email = $"sec-onboard-ts-{Guid.NewGuid():N}@lemondo.dev";
        using var client = await RegisterAndAuthenticateAsync(email);

        var first = await client.PostAsync("/api/onboarding/complete", null);
        var firstStatus = await first.Content.ReadFromJsonAsync<OnboardingStatusResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(firstStatus?.CompletedAt);

        await Task.Delay(50); // Ensure a different timestamp would be produced

        var second = await client.PostAsync("/api/onboarding/complete", null);
        var secondStatus = await second.Content.ReadFromJsonAsync<OnboardingStatusResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(secondStatus?.CompletedAt);

        // The CompletedAt timestamp must not be reset/overwritten by the second call
        Assert.AreEqual(firstStatus.CompletedAt, secondStatus.CompletedAt,
            "Second call to /complete must not overwrite the original CompletedAt timestamp");
    }

    // -------------------------------------------------------------------------
    // ANALYTICS — Authentication Bypass
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_Return401_When_TrackEventsWithNoToken()
    {
        var request = new TrackEventsRequest
        {
            Events = [new AnalyticsEvent { EventName = "test_event" }]
        };

        var response = await _anonymousClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_TrackEventsWithMalformedToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "totally.invalid.token");

        var response = await client.PostAsJsonAsync("/api/analytics/events",
            new TrackEventsRequest { Events = [new AnalyticsEvent { EventName = "test" }] });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_TrackEventsWithEmptyBearerToken()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");

        var response = await client.PostAsJsonAsync("/api/analytics/events",
            new TrackEventsRequest { Events = [new AnalyticsEvent { EventName = "test" }] });

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // ANALYTICS — Input Validation / Injection in Payloads
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_Return200_When_EventNameContainsSqlInjection()
    {
        // The analytics service must store/log this safely — verifying no 500 crash
        var request = new TrackEventsRequest
        {
            Events = [new AnalyticsEvent { EventName = "'; DROP TABLE AnalyticsEvents; --" }]
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in EventName must not cause a server error");
    }

    [TestMethod]
    public async Task Should_Return200_When_EventNameContainsXssPayload()
    {
        var request = new TrackEventsRequest
        {
            Events = [new AnalyticsEvent { EventName = "<script>alert('xss')</script>" }]
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "XSS payload in EventName must not cause a server error");
    }

    [TestMethod]
    public async Task Should_Return200_When_PropertiesContainSqlInjection()
    {
        var request = new TrackEventsRequest
        {
            Events =
            [
                new AnalyticsEvent
                {
                    EventName = "test_event",
                    Properties = new Dictionary<string, string>
                    {
                        ["key"] = "'; DROP TABLE Users; --",
                        ["other"] = "1 OR 1=1"
                    }
                }
            ]
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "SQL injection in Properties must not cause a server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_NullEventsField()
    {
        var request = new TrackEventsRequest { Events = null };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Null Events field must not crash the endpoint");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_EmptyEventsArray()
    {
        var request = new TrackEventsRequest { Events = [] };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            "Empty Events array must return 200");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_EventNameIsUnicodeWithZeroWidthChars()
    {
        // Zero-width space + RTL override — potential log injection
        var unicodePayload = "task\u200Bcreated\u202E";
        var request = new TrackEventsRequest
        {
            Events = [new AnalyticsEvent { EventName = unicodePayload }]
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Unicode special chars in EventName must not cause a server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_EventNameIsExtremelyLong()
    {
        var longName = new string('a', 100_000);
        var request = new TrackEventsRequest
        {
            Events = [new AnalyticsEvent { EventName = longName }]
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "100,000-char EventName must not cause an unhandled server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_LargeEventsBatch()
    {
        // Attempt a large batch to test for DoS via payload bombing
        var events = Enumerable.Range(0, 500)
            .Select(i => new AnalyticsEvent
            {
                EventName = $"event_{i}",
                Properties = new Dictionary<string, string> { ["idx"] = i.ToString() }
            })
            .ToList();

        var request = new TrackEventsRequest { Events = events };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Large events batch must not cause an unhandled server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_AnalyticsBodyIsOversized()
    {
        // Build a payload that is approximately 2 MB
        var hugeProperties = Enumerable.Range(0, 200)
            .ToDictionary(i => $"key_{i}", i => new string('v', 5000));

        var request = new TrackEventsRequest
        {
            Events = [new AnalyticsEvent { EventName = "test", Properties = hugeProperties }]
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _authenticatedClient.PostAsync("/api/analytics/events", content);

        // 400 (body too large / validation) or 413 (request entity too large) are both acceptable
        // What must NOT happen is a 500 (unhandled crash)
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Oversized analytics payload must not cause an unhandled server error");
    }

    [TestMethod]
    public async Task Should_NotReturn500_When_PropertiesValueIsExtremelyLong()
    {
        var request = new TrackEventsRequest
        {
            Events =
            [
                new AnalyticsEvent
                {
                    EventName = "test_event",
                    Properties = new Dictionary<string, string>
                    {
                        ["key"] = new string('x', 50_000)
                    }
                }
            ]
        };

        var response = await _authenticatedClient.PostAsJsonAsync("/api/analytics/events", request);

        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Extremely long property value must not cause a server error");
    }

    // -------------------------------------------------------------------------
    // CONFIG (/api/config) — Information Disclosure
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_BeAccessibleWithoutAuthentication_When_GetConfig()
    {
        // Intentional design: /api/config is public
        var response = await _anonymousClient.GetAsync("/api/config");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_OnlyExposeAllowedFields_When_GetConfig()
    {
        var response = await _anonymousClient.GetAsync("/api/config");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        // The ONLY field allowed is EnableDemoAccounts
        // Any additional fields would be information leakage
        var properties = root.EnumerateObject().Select(p => p.Name).ToList();
        Assert.HasCount(1, properties,
            $"Config endpoint must expose exactly 1 field. Found: {string.Join(", ", properties)}");
        Assert.IsTrue(properties.Contains("enableDemoAccounts", StringComparer.OrdinalIgnoreCase),
            "Expected only 'enableDemoAccounts' field");
    }

    [TestMethod]
    public async Task Should_NotExposeConnectionStrings_When_GetConfig()
    {
        var response = await _anonymousClient.GetAsync("/api/config");
        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        // StringComparison.OrdinalIgnoreCase is checked via ToLower normalization
        // since Assert.DoesNotContain performs ordinal (case-sensitive) comparison
        Assert.DoesNotContain("connectionstring", body.ToLowerInvariant(),
            "Config endpoint must not expose connection strings");
        Assert.DoesNotContain("datasource", body.ToLowerInvariant(),
            "Config endpoint must not expose database source");
        Assert.DoesNotContain("server=", body.ToLowerInvariant(),
            "Config endpoint must not expose server address");
    }

    [TestMethod]
    public async Task Should_NotExposeJwtSecrets_When_GetConfig()
    {
        var response = await _anonymousClient.GetAsync("/api/config");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("secretkey", body.ToLowerInvariant(),
            "Config endpoint must not expose JWT secret key");
        Assert.DoesNotContain("secret", body.ToLowerInvariant(),
            "Config endpoint must not expose any secret values");
        Assert.DoesNotContain("issuer", body.ToLowerInvariant(),
            "Config endpoint must not expose JWT issuer config");
    }

    [TestMethod]
    public async Task Should_NotExposeEncryptionKeys_When_GetConfig()
    {
        var response = await _anonymousClient.GetAsync("/api/config");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("encryption", body.ToLowerInvariant(),
            "Config endpoint must not expose encryption configuration");
        Assert.DoesNotContain("encryptionkey", body.ToLowerInvariant(),
            "Config endpoint must not expose field encryption keys");
    }

    [TestMethod]
    public async Task Should_NotExposeInternalServerErrors_When_GetConfigReturnsSuccessfully()
    {
        var response = await _anonymousClient.GetAsync("/api/config");

        // Must respond with a clean JSON body, not a stack trace
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("Exception", body, $"Response must not leak exception details: {body}");
        Assert.DoesNotContain("StackTrace", body, $"Response must not contain stack traces: {body}");
    }

    [TestMethod]
    public async Task Should_ReturnValidBooleanOnly_When_GetConfigEnableDemoAccounts()
    {
        var response = await _anonymousClient.GetAsync("/api/config");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        // The EnableDemoAccounts field must be a boolean (true or false), not a string or object
        var prop = doc.RootElement.EnumerateObject()
            .FirstOrDefault(p => p.Name.Equals("enableDemoAccounts", StringComparison.OrdinalIgnoreCase));

        Assert.IsTrue(
            prop.Value.ValueKind == JsonValueKind.True || prop.Value.ValueKind == JsonValueKind.False,
            $"enableDemoAccounts must be a JSON boolean true or false, got: {prop.Value.ValueKind}");
    }

    [TestMethod]
    public async Task Should_HaveSecurityHeaders_When_GetConfigAnonymously()
    {
        var response = await _anonymousClient.GetAsync("/api/config");

        Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options security header must be present on /api/config");
        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options security header must be present on /api/config");
    }

    [TestMethod]
    public async Task Should_NotExposeEnvironmentVariables_When_GetConfig()
    {
        // Use a factory configured with sensitive environment-like values to ensure
        // they cannot be extracted via the config endpoint
        using var factory = new CustomWebApplicationFactory()
            .WithWebHostBuilder(hostBuilder =>
            {
                hostBuilder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Features:EnableDemoAccounts"] = "true",
                        ["SensitiveInternalKey"] = "super-secret-value-12345",
                        ["DatabasePassword"] = "db-pass-should-not-appear",
                    });
                });
            });

        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/config");
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("super-secret-value-12345", body,
            "Config endpoint must not expose values from arbitrary config keys");
        Assert.DoesNotContain("db-pass-should-not-appear", body,
            "Config endpoint must not expose database passwords from config");
        Assert.DoesNotContain("sensitiveinternalkey", body.ToLowerInvariant(),
            "Config endpoint must not expose internal config key names");
    }

    // -------------------------------------------------------------------------
    // HTTP Security — Method Not Allowed
    // -------------------------------------------------------------------------

    [TestMethod]
    public async Task Should_Return405_When_DeleteSentToNotificationsListEndpoint()
    {
        var response = await _authenticatedClient.DeleteAsync("/api/notifications");

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_PutSentToOnboardingStatusEndpoint()
    {
        var response = await _authenticatedClient.PutAsync("/api/onboarding/status",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_GetSentToAnalyticsEventsEndpoint()
    {
        var response = await _authenticatedClient.GetAsync("/api/analytics/events");

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_PostSentToConfigEndpoint()
    {
        var response = await _anonymousClient.PostAsync("/api/config",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private async Task<HttpClient> RegisterAndAuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = "Security Test User" });
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }
}
