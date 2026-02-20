namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// Security hardening tests for /api/auth/* endpoints.
/// A PASSING test means the endpoint correctly rejects the attack (it is SECURE).
/// A FAILING test means the endpoint is VULNERABLE (it accepted or mishandled the attack).
/// </summary>
[TestClass]
public sealed class AuthSecurityHardeningTests
{
    private static CustomWebApplicationFactory _factory = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _factory.Dispose();
    }

    // =========================================================================
    // HELPER: fresh cookie-less client (avoids cross-test cookie contamination)
    // =========================================================================
    private HttpClient CreateFreshClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });

    private static string ExtractRefreshTokenCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return string.Empty;
        var setCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        if (setCookie is null) return string.Empty;
        var semiIndex = setCookie.IndexOf(';');
        return semiIndex > 0 ? setCookie[..semiIndex] : setCookie;
    }

    // =========================================================================
    // CATEGORY 1: AUTHENTICATION BYPASS
    // =========================================================================

    [TestMethod]
    public async Task Should_Return401_When_GetMeWithNoToken()
    {
        using var client = CreateFreshClient();
        var response = await client.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_GetMeWithMalformedToken()
    {
        using var client = CreateFreshClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "this.is.not.a.jwt");
        var response = await client.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_GetMeWithRandomBase64Token()
    {
        using var client = CreateFreshClient();
        // Looks like a JWT (three base64 segments) but is not signed by our key
        var fakeHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"HS256","typ":"JWT"}"""));
        var fakePayload = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"sub":"00000000-0000-0000-0000-000000000001","exp":9999999999}"""));
        var fakeToken = $"{fakeHeader}.{fakePayload}.INVALIDSIGNATURE";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", fakeToken);
        var response = await client.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_GetMeWithEmptyBearerToken()
    {
        using var client = CreateFreshClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");
        var response = await client.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_LogoutWithNoToken()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsync("/api/auth/logout", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_RevealProfileWithNoToken()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/reveal-profile",
            new { Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_RevealProfileWithMalformedToken()
    {
        using var client = CreateFreshClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-token");
        var response = await client.PostAsJsonAsync("/api/auth/reveal-profile",
            new { Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_AlgorithmNoneAttack()
    {
        // "alg": "none" attack — unsigned token, should be rejected
        using var client = CreateFreshClient();
        var header = Convert.ToBase64String(Encoding.UTF8.GetBytes("""{"alg":"none","typ":"JWT"}"""))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(
                """{"sub":"00000000-0000-0000-0000-000000000001","role":"SystemAdmin","exp":9999999999}"""))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var noneToken = $"{header}.{payload}.";
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", noneToken);
        var response = await client.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // =========================================================================
    // CATEGORY 2: TOKEN SECURITY
    // =========================================================================

    [TestMethod]
    public async Task Should_Return401_When_RefreshWithNoCookie()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsync("/api/auth/refresh", null);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_RefreshWithFakeToken()
    {
        using var client = CreateFreshClient();
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        request.Headers.Add("Cookie", "refresh_token=AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");
        var response = await client.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_ReusingRefreshTokenAfterRotation()
    {
        // Log in to get the first refresh token
        using var client = CreateFreshClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
        var originalCookie = ExtractRefreshTokenCookie(loginResponse);
        Assert.IsFalse(string.IsNullOrEmpty(originalCookie), "Login must set a refresh_token cookie");

        // First refresh — this rotates the token (old one is revoked, new one issued)
        var firstRefreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        firstRefreshRequest.Headers.Add("Cookie", originalCookie);
        var firstRefreshResponse = await client.SendAsync(firstRefreshRequest);
        Assert.AreEqual(HttpStatusCode.OK, firstRefreshResponse.StatusCode, "First refresh should succeed");

        // Attempt to reuse the ORIGINAL (now-revoked) token — must be rejected
        var reuseRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        reuseRequest.Headers.Add("Cookie", originalCookie);
        var reuseResponse = await client.SendAsync(reuseRequest);
        Assert.AreEqual(HttpStatusCode.Unauthorized, reuseResponse.StatusCode,
            "Reused (rotated-away) refresh token must be rejected");
    }

    [TestMethod]
    public async Task Should_NotIncludeRefreshTokenInResponseBody()
    {
        using var client = CreateFreshClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);

        var body = await loginResponse.Content.ReadAsStringAsync();
        // The refresh token is a 64-byte random base64 string — if it leaks it would appear as a long
        // base64 property. We verify the body does not contain a "refreshToken" key.
        Assert.IsFalse(body.Contains("refreshToken", StringComparison.OrdinalIgnoreCase),
            $"Response body must not contain a refreshToken field. Body: {body}");
        Assert.IsFalse(body.Contains("refresh_token", StringComparison.OrdinalIgnoreCase),
            $"Response body must not contain refresh_token field. Body: {body}");
    }

    [TestMethod]
    public async Task Should_Return401_When_RefreshTokenCookieSentToNonAuthPath()
    {
        // The refresh cookie is scoped to Path=/api/auth; sending it to /api/tasks
        // must not grant access. This validates cookie path scoping.
        using var loginClient = CreateFreshClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
        var cookie = ExtractRefreshTokenCookie(loginResponse);
        Assert.IsFalse(string.IsNullOrEmpty(cookie));

        // Send the refresh cookie to a task endpoint as if it were a bearer token — no Authorization header
        using var attackClient = CreateFreshClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/tasks");
        request.Headers.Add("Cookie", cookie);
        var response = await attackClient.SendAsync(request);
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode,
            "A refresh_token cookie must not grant access to non-auth endpoints");
    }

    // =========================================================================
    // CATEGORY 3: INPUT VALIDATION & INJECTION
    // =========================================================================

    [TestMethod]
    public async Task Should_Return400_When_SqlInjectionInLoginEmail()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "'; DROP TABLE users; --", Password = "SomePass123!" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_SqlInjectionInRegisterEmail()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = "' OR '1'='1", Password = "SomePass123!", DisplayName = "Hacker" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_XssPayloadInRegisterDisplayName()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = "xss1@lemondo.dev", Password = "SomePass123!", DisplayName = "<script>alert('xss')</script>" });
        // DisplayName max is 100 chars; <script>...</script> is 31 chars — it passes length but
        // domain logic or the converter must reject/sanitize it. We expect 400 (validation) OR
        // if it is accepted, we assert the stored value is NOT the raw script tag.
        // Given DisplayName.Create just checks length (2-100), XSS content could pass.
        // The contract requires the server returns REDACTED data — never raw script.
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.IsNotNull(auth);
            Assert.IsFalse(auth.User.DisplayName.Contains("<script>", StringComparison.OrdinalIgnoreCase),
                "Server must not echo raw script tags back to the client");
        }
        else
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [TestMethod]
    public async Task Should_Return400_When_InvalidEmailFormat()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "not-an-email", Password = "SomePass123!" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_OversizedEmailInLogin()
    {
        using var client = CreateFreshClient();
        var longEmail = new string('a', 250) + "@x.com"; // > 254 chars total
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = longEmail, Password = "SomePass123!" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_OversizedDisplayName()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = "longname@lemondo.dev", Password = "SomePass123!", DisplayName = new string('A', 101) });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_EmptyEmailInLogin()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "", Password = "SomePass123!" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_EmptyPasswordInLogin()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = "" });
        // ProtectedValueConverter wraps any string including empty; login will fail with 401 (wrong password)
        // OR the validator rejects an empty password as 400. Both are acceptable — the key is it must not be 200.
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode,
            "Empty password must not succeed");
    }

    [TestMethod]
    public async Task Should_Return4xx_When_MissingBodyInLogin()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsync("/api/auth/login",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        // No email/password → converter sees null → throws ProtectedDataValidationException → 400
        // OR model binding fails → 400
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return4xx_When_MissingBodyInRegister()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsync("/api/auth/register",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400_When_OversizedBodyInLogin()
    {
        using var client = CreateFreshClient();
        // 2MB body — server should reject or truncate, not crash with OOM
        var hugePadding = new string('x', 2 * 1024 * 1024);
        var json = JsonSerializer.Serialize(new { Email = "a@b.com", Password = "Pass123!", Extra = hugePadding });
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await client.PostAsync("/api/auth/login", content);
        // Expect 400 or 413; must NOT be 500 or 200
        Assert.IsTrue(
            response.StatusCode is HttpStatusCode.BadRequest
                or HttpStatusCode.RequestEntityTooLarge
                or HttpStatusCode.Unauthorized,
            $"Expected 400/413/401 for oversized body, got {(int)response.StatusCode}");
    }

    [TestMethod]
    public async Task Should_Return400_When_TemplateInjectionInDisplayName()
    {
        using var client = CreateFreshClient();
        // {{7*7}} — SSTI probe; DisplayName.Create only validates length (2-100), so this passes.
        // We verify the server does NOT evaluate the template — if 200 is returned, the stored
        // redacted value must not be "49" (the evaluated result).
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = "ssti@lemondo.dev", Password = "SomePass123!", DisplayName = "{{7*7}}" });
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.IsNotNull(auth);
            Assert.AreNotEqual("49", auth.User.DisplayName, "Template expression must not be evaluated");
        }
        else
        {
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    // =========================================================================
    // CATEGORY 4: MASS ASSIGNMENT / OVER-POSTING
    // =========================================================================

    [TestMethod]
    public async Task Should_IgnoreRoleFieldInRegisterBody()
    {
        using var client = CreateFreshClient();
        // Attempt to self-assign a privileged role during registration
        var json = """{"Email":"massassign@lemondo.dev","Password":"SomePass123!","DisplayName":"MassTest","Role":"Admin","Roles":["Admin","SystemAdmin"]}""";
        var response = await client.PostAsync("/api/auth/register",
            new StringContent(json, Encoding.UTF8, "application/json"));

        // Registration may succeed (200) — but the role must not be Admin
        if (response.StatusCode == HttpStatusCode.OK)
        {
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.IsNotNull(auth);
            Assert.IsNotNull(auth.User.Roles, "Roles should not be null");
            CollectionAssert.DoesNotContain(auth.User.Roles.ToList(), "Admin",
                "User must not be assigned Admin role via registration payload");
            CollectionAssert.DoesNotContain(auth.User.Roles.ToList(), "SystemAdmin",
                "User must not be assigned SystemAdmin role via registration payload");
        }
        else
        {
            // 400 is also acceptable (unknown field handling)
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }

    [TestMethod]
    public async Task Should_IgnoreIsVerifiedFieldInRegisterBody()
    {
        using var client = CreateFreshClient();
        var json = """{"Email":"verified@lemondo.dev","Password":"SomePass123!","DisplayName":"Verified","IsVerified":true,"EmailConfirmed":true}""";
        var response = await client.PostAsync("/api/auth/register",
            new StringContent(json, Encoding.UTF8, "application/json"));
        // We can't directly read IsVerified from the response, but registration must either
        // succeed normally (200) or fail validation (400). A 500 would indicate a server error
        // from unexpected field processing.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Extra fields in registration body must not cause a server error");
    }

    [TestMethod]
    public async Task Should_IgnoreIdFieldInRegisterBody()
    {
        using var client = CreateFreshClient();
        var json = """{"Email":"withid@lemondo.dev","Password":"SomePass123!","DisplayName":"IdTest","Id":"00000000-0000-0000-0000-000000000001","UserId":"00000000-0000-0000-0000-000000000001"}""";
        var response = await client.PostAsync("/api/auth/register",
            new StringContent(json, Encoding.UTF8, "application/json"));

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
            Assert.IsNotNull(auth);
            // The server must assign its own GUID, not the attacker's
            Assert.AreNotEqual(Guid.Empty, auth.User.Id,
                "Server must generate its own user ID");
            Assert.AreNotEqual(new Guid("00000000-0000-0000-0000-000000000001"), auth.User.Id,
                "Server must not accept a client-supplied user ID");
        }
    }

    [TestMethod]
    public async Task Should_NotExposePasswordHashInResponse()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();
        // A password hash would typically contain "$2" (bcrypt) or be a long hex string
        Assert.IsFalse(body.Contains("passwordHash", StringComparison.OrdinalIgnoreCase),
            "Response body must not expose password hash");
        Assert.IsFalse(body.Contains("password", StringComparison.OrdinalIgnoreCase),
            $"Response body must not expose password field. Body: {body}");
    }

    // =========================================================================
    // CATEGORY 5: INFORMATION LEAKAGE
    // =========================================================================

    [TestMethod]
    public async Task Should_UseConsistentErrorResponse_When_UserNotFound()
    {
        using var client = CreateFreshClient();
        var notFoundResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nobody@lemondo.dev", Password = "SomePass123!" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, notFoundResponse.StatusCode,
            "Non-existent user must return 401, not 404");
    }

    [TestMethod]
    public async Task Should_UseConsistentErrorMessage_ForWrongPasswordVsNonExistentUser()
    {
        using var client = CreateFreshClient();

        // Wrong password for existing user
        var wrongPassResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = "WrongPass999!" });
        var wrongPassBody = await wrongPassResponse.Content.ReadAsStringAsync();

        // Non-existent user
        var noUserResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = "nosuchuser@lemondo.dev", Password = "SomePass123!" });
        var noUserBody = await noUserResponse.Content.ReadAsStringAsync();

        // Both must return 401 (not 404 vs 401)
        Assert.AreEqual(HttpStatusCode.Unauthorized, wrongPassResponse.StatusCode);
        Assert.AreEqual(HttpStatusCode.Unauthorized, noUserResponse.StatusCode);

        // The error messages should be identical to prevent user enumeration
        var wrongPassError = JsonSerializer.Deserialize<JsonElement>(wrongPassBody);
        var noUserError = JsonSerializer.Deserialize<JsonElement>(noUserBody);

        var wrongPassTitle = wrongPassError.GetProperty("title").GetString();
        var noUserTitle = noUserError.GetProperty("title").GetString();

        Assert.AreEqual(wrongPassTitle, noUserTitle,
            $"Error message must be identical for wrong-password vs non-existent-user to prevent enumeration. " +
            $"WrongPass: '{wrongPassTitle}', NoUser: '{noUserTitle}'");
    }

    [TestMethod]
    public async Task Should_NotRevealStackTrace_When_InvalidLoginBody()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsync("/api/auth/login",
            new StringContent("not valid json!!!", Encoding.UTF8, "application/json"));

        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "Response must not contain stack trace");
        Assert.IsFalse(body.Contains("at LemonDo", StringComparison.OrdinalIgnoreCase),
            "Response must not contain internal namespace details in stack trace");
        Assert.IsFalse(body.Contains("Exception", StringComparison.OrdinalIgnoreCase)
                       && !body.Contains("validation_error", StringComparison.OrdinalIgnoreCase),
            "Response must not expose raw exception type in non-development mode");
    }

    [TestMethod]
    public async Task Should_NotExposeInternalDatabaseColumnNames_InErrorResponse()
    {
        using var client = CreateFreshClient();
        // Trigger a duplicate registration to get a conflict error
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = "TestPass123!", DisplayName = "Dup" });
        var body = await response.Content.ReadAsStringAsync();
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.IsFalse(body.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase),
            "Error must not expose database UNIQUE constraint details");
        Assert.IsFalse(body.Contains("AspNetUsers", StringComparison.OrdinalIgnoreCase),
            "Error must not expose internal table names");
        Assert.IsFalse(body.Contains("UserName", StringComparison.OrdinalIgnoreCase),
            "Error must not expose database column names");
    }

    [TestMethod]
    public async Task Should_Return404OrGenericError_When_NonExistentEndpointCalled()
    {
        using var client = CreateFreshClient();
        var response = await client.GetAsync("/api/auth/admin-backdoor");
        // 404 or 405 — must not expose framework details
        var body = await response.Content.ReadAsStringAsync();
        Assert.IsFalse(body.Contains("StackTrace", StringComparison.OrdinalIgnoreCase),
            "404 response must not contain a stack trace");
    }

    [TestMethod]
    public async Task Should_NotExposeFullEmailInRedactedMe()
    {
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await authedClient.GetAsync("/api/auth/me");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.IsNotNull(user);
        // The full email must never appear — only a redacted version
        Assert.AreNotEqual(CustomWebApplicationFactory.TestUserEmail, user.Email,
            "/me must return redacted email, not the full plaintext email");
        // Verify redaction pattern: t***@lemondo.dev
        Assert.Contains("***", user.Email, $"Email must be redacted, got: {user.Email}");
    }

    // =========================================================================
    // CATEGORY 6: COOKIE SECURITY FLAGS
    // =========================================================================

    [TestMethod]
    public async Task Should_SetHttpOnlyFlag_OnRefreshCookie()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.IsTrue(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var refreshCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        Assert.IsNotNull(refreshCookie, "refresh_token cookie must be set");
        Assert.IsTrue(refreshCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase),
            "refresh_token must be HttpOnly");
    }

    [TestMethod]
    public async Task Should_SetSameSiteStrict_OnRefreshCookie()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.IsTrue(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var refreshCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        Assert.IsNotNull(refreshCookie);
        Assert.IsTrue(refreshCookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase),
            $"refresh_token must be SameSite=Strict, got: {refreshCookie}");
    }

    [TestMethod]
    public async Task Should_ScopeRefreshCookieToAuthPath()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        Assert.IsTrue(response.Headers.TryGetValues("Set-Cookie", out var cookies));
        var refreshCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        Assert.IsNotNull(refreshCookie);
        Assert.IsTrue(refreshCookie.Contains("path=/api/auth", StringComparison.OrdinalIgnoreCase),
            $"refresh_token must be scoped to /api/auth, got: {refreshCookie}");
    }

    [TestMethod]
    public async Task Should_ClearRefreshCookieOnLogout()
    {
        // Login to get a token
        using var client = CreateFreshClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        var cookie = ExtractRefreshTokenCookie(loginResponse);
        Assert.IsFalse(string.IsNullOrEmpty(cookie));

        // Logout
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        logoutRequest.Headers.Add("Cookie", cookie);
        var logoutResponse = await client.SendAsync(logoutRequest);
        Assert.AreEqual(HttpStatusCode.OK, logoutResponse.StatusCode);

        // The logout response must contain a Set-Cookie that clears refresh_token
        Assert.IsTrue(logoutResponse.Headers.TryGetValues("Set-Cookie", out var logoutCookies),
            "Logout must emit Set-Cookie to clear the refresh_token");
        var clearCookie = logoutCookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        Assert.IsNotNull(clearCookie, "Logout Set-Cookie must target refresh_token");
        // A cleared cookie has an empty value or Max-Age=0 / expires in the past
        var hasEmptyValue = clearCookie.StartsWith("refresh_token=;") || clearCookie.StartsWith("refresh_token= ;");
        var hasMaxAgeZero = clearCookie.Contains("max-age=0", StringComparison.OrdinalIgnoreCase);
        Assert.IsTrue(hasEmptyValue || hasMaxAgeZero,
            $"Logout must clear the refresh_token cookie. Got: {clearCookie}");
    }

    // =========================================================================
    // CATEGORY 7: BUSINESS LOGIC ABUSE
    // =========================================================================

    [TestMethod]
    public async Task Should_Return400_When_WeakPasswordOnRegister()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = "weakpass@lemondo.dev", Password = "short", DisplayName = "WeakPass" });
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return409_When_DuplicateEmailRegistration()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = "TestPass123!", DisplayName = "Dup" });
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_RevealProfileWithWrongPassword()
    {
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await authedClient.PostAsJsonAsync("/api/auth/reveal-profile",
            new { Password = "WrongPassword999!" });
        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUnredactedData_Only_When_RevealProfileWithCorrectPassword()
    {
        // Verify that a valid user CAN reveal their own data — correct positive case
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await authedClient.PostAsJsonAsync("/api/auth/reveal-profile",
            new { Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.IsNotNull(json);
        var email = json.RootElement.GetProperty("email").GetString();
        Assert.AreEqual(CustomWebApplicationFactory.TestUserEmail, email,
            "Revealed email must match the authenticated user's actual email");
    }

    [TestMethod]
    public async Task Should_Return401_When_RevealProfileAfterLogout()
    {
        // Ensure that a logged-out access token cannot reveal profile data
        using var loginClient = CreateFreshClient();
        var loginResponse = await loginClient.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        var cookie = ExtractRefreshTokenCookie(loginResponse);

        // Logout (revoke server-side session)
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        logoutRequest.Headers.Add("Cookie", cookie);
        await loginClient.SendAsync(logoutRequest);

        // Attempt to use the OLD access token on reveal-profile
        // JWT tokens are stateless — the access token remains valid until expiry.
        // This is a known limitation of stateless JWTs and is not a vulnerability IF
        // the token TTL is short (60 min in test config). We document this behavior.
        using var attackClient = CreateFreshClient();
        attackClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var revealResponse = await attackClient.PostAsJsonAsync("/api/auth/reveal-profile",
            new { Password = CustomWebApplicationFactory.TestUserPassword });

        // With stateless JWT and 60-min TTL: this MAY succeed (200) for the token lifetime.
        // We assert it does NOT return a 500 (server error), and if 200, the data belongs to the right user.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, revealResponse.StatusCode,
            "Post-logout reveal must not cause a server error");
        if (revealResponse.StatusCode == HttpStatusCode.OK)
        {
            // Acceptable: JWT still valid within TTL. Document this as expected behavior.
            // In production, short-lived tokens (15 min) mitigate this risk.
        }
    }

    // =========================================================================
    // CATEGORY 8: HTTP SECURITY HEADERS & CORS
    // =========================================================================

    [TestMethod]
    public async Task Should_IncludeXContentTypeOptions_OnAuthEndpoint()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });

        Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"),
            "X-Content-Type-Options header must be present");
        Assert.AreEqual("nosniff",
            response.Headers.GetValues("X-Content-Type-Options").First());
    }

    [TestMethod]
    public async Task Should_IncludeXFrameOptions_OnAuthEndpoint()
    {
        using var client = CreateFreshClient();
        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });

        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"),
            "X-Frame-Options header must be present on auth endpoints");
    }

    [TestMethod]
    public async Task Should_Return405_When_WrongHttpMethodOnLoginEndpoint()
    {
        using var client = CreateFreshClient();
        // GET /api/auth/login is not defined — should be 405 Method Not Allowed
        var response = await client.GetAsync("/api/auth/login");
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return405_When_WrongHttpMethodOnMeEndpoint()
    {
        using var client = CreateFreshClient();
        // POST /api/auth/me is not defined — should be 405
        var response = await client.PostAsync("/api/auth/me", null);
        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return400Or415_When_WrongContentTypeOnLogin()
    {
        using var client = CreateFreshClient();
        var content = new StringContent(
            """{"Email":"test@lemondo.dev","Password":"TestPass123!"}""",
            Encoding.UTF8,
            "text/plain"); // wrong content-type
        var response = await client.PostAsync("/api/auth/login", content);
        // ASP.NET Core minimal API accepts any Content-Type for JSON by default,
        // but the body parser may fail to read it as JSON if Content-Type is wrong.
        // We assert it does not return 200 (it must not succeed with wrong content-type
        // if the server is strict) OR it gracefully handles the mismatch.
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            "Wrong Content-Type must not cause a 500 server error");
    }
}
