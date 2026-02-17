namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

[TestClass]
public sealed class AuthEndpointTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _anonymousClient = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _anonymousClient = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _anonymousClient.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Should_Register_When_ValidCredentials()
    {
        var request = new { Email = "new@lemondo.dev", Password = "NewPass123!", DisplayName = "New User" };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        Assert.IsNotNull(auth.AccessToken);
        // Protected data zero-trust: register returns redacted values
        Assert.AreEqual("n***@lemondo.dev", auth.User.Email);
        Assert.AreEqual("N***r", auth.User.DisplayName);

        // Refresh token should be in HttpOnly cookie, NOT in JSON body
        AssertRefreshTokenCookie(response);
    }

    [TestMethod]
    public async Task Should_ReturnConflict_When_DuplicateEmail()
    {
        // Test user is pre-seeded by factory
        var request = new
        {
            Email = CustomWebApplicationFactory.TestUserEmail,
            Password = "AnotherPass123!",
            DisplayName = "Duplicate"
        };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_WeakPassword()
    {
        var request = new { Email = "weak@lemondo.dev", Password = "short", DisplayName = "Weak Password" };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Login_When_ValidCredentials()
    {
        var request = new
        {
            Email = CustomWebApplicationFactory.TestUserEmail,
            Password = CustomWebApplicationFactory.TestUserPassword
        };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/login", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        Assert.IsNotNull(auth.AccessToken);
        // Protected data zero-trust: login returns redacted email
        Assert.AreEqual("t***@lemondo.dev", auth.User.Email);

        // Refresh token should be in HttpOnly cookie, NOT in JSON body
        AssertRefreshTokenCookie(response);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_WrongPassword()
    {
        var request = new
        {
            Email = CustomWebApplicationFactory.TestUserEmail,
            Password = "WrongPassword123!"
        };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/login", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_NonexistentEmail()
    {
        var request = new { Email = "nobody@lemondo.dev", Password = "Pass123!" };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/login", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_NoToken()
    {
        var response = await _anonymousClient.GetAsync("/api/tasks");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnOk_When_ValidToken()
    {
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await authedClient.GetAsync("/api/tasks");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_RefreshToken_When_ValidCookie()
    {
        // Login to get refresh token cookie
        var loginResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);

        // Extract the refresh cookie from the login response
        var refreshCookie = ExtractRefreshTokenCookie(loginResponse);
        Assert.IsNotNull(refreshCookie, "Login should set refresh_token cookie");

        // Send refresh request with cookie
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);

        var refreshResponse = await _anonymousClient.SendAsync(refreshRequest);

        Assert.AreEqual(HttpStatusCode.OK, refreshResponse.StatusCode);
        var newAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(newAuth);
        Assert.AreNotEqual(auth.AccessToken, newAuth.AccessToken);
        AssertRefreshTokenCookie(refreshResponse);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_RefreshWithNoCookie()
    {
        // Use a fresh client with no cookie jar to avoid inheriting cookies from other tests
        using var freshClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
        });
        var response = await freshClient.PostAsync("/api/auth/refresh", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUserProfile_When_Authenticated()
    {
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await authedClient.GetAsync("/api/auth/me");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.IsNotNull(user);
        // Protected data zero-trust: /me returns redacted values
        Assert.AreEqual("t***@lemondo.dev", user.Email);
        Assert.AreEqual("T***r", user.DisplayName);
    }

    [TestMethod]
    public async Task Should_IncludeRoles_When_LoginResponse()
    {
        var request = new
        {
            Email = CustomWebApplicationFactory.TestUserEmail,
            Password = CustomWebApplicationFactory.TestUserPassword
        };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/login", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        Assert.IsNotNull(auth.User.Roles);
        CollectionAssert.Contains(auth.User.Roles.ToList(), "User");
    }

    [TestMethod]
    public async Task Should_IncludeRoles_When_MeEndpoint()
    {
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();

        var response = await authedClient.GetAsync("/api/auth/me");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var user = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.IsNotNull(user);
        Assert.IsNotNull(user.Roles);
        CollectionAssert.Contains(user.Roles.ToList(), "User");
    }

    [TestMethod]
    public async Task Should_IncludeAdminRole_When_AdminLogin()
    {
        var request = new
        {
            Email = CustomWebApplicationFactory.AdminUserEmail,
            Password = CustomWebApplicationFactory.AdminUserPassword
        };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/login", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        Assert.IsNotNull(auth.User.Roles);
        CollectionAssert.Contains(auth.User.Roles.ToList(), "Admin");
    }

    [TestMethod]
    public async Task Should_InvalidateRefreshToken_When_Logout()
    {
        // Login to get tokens + cookie
        var loginResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/login",
            new { Email = CustomWebApplicationFactory.TestUserEmail, Password = CustomWebApplicationFactory.TestUserPassword });
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        var refreshCookie = ExtractRefreshTokenCookie(loginResponse);
        Assert.IsNotNull(refreshCookie);

        // Logout with the refresh token cookie
        var logoutRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        logoutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        logoutRequest.Headers.Add("Cookie", refreshCookie);

        var logoutResponse = await _anonymousClient.SendAsync(logoutRequest);
        Assert.AreEqual(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Refresh should now fail with the old cookie
        var refreshRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refresh");
        refreshRequest.Headers.Add("Cookie", refreshCookie);

        var refreshResponse = await _anonymousClient.SendAsync(refreshRequest);
        Assert.AreEqual(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [TestMethod]
    public async Task Should_RevealProfile_When_ValidPassword()
    {
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();
        var request = new { Password = CustomWebApplicationFactory.TestUserPassword };

        var response = await authedClient.PostAsJsonAsync("/api/auth/reveal-profile", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // RevealedField is decrypted to plain strings by the server's RevealedFieldConverter,
        // so we deserialize into a JsonDocument to read strings directly.
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.IsNotNull(json);
        var email = json.RootElement.GetProperty("email").GetString();
        var displayName = json.RootElement.GetProperty("displayName").GetString();
        // Should return UNREDACTED data
        Assert.AreEqual(CustomWebApplicationFactory.TestUserEmail, email);
        Assert.AreEqual("Test User", displayName);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_WrongPasswordOnRevealProfile()
    {
        using var authedClient = await _factory.CreateAuthenticatedClientAsync();
        var request = new { Password = "WrongPassword123!" };

        var response = await authedClient.PostAsJsonAsync("/api/auth/reveal-profile", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_AnonymousRevealProfile()
    {
        var request = new { Password = "AnyPassword123!" };

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/reveal-profile", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>Asserts that the response contains a Set-Cookie header for refresh_token with correct flags.</summary>
    private static void AssertRefreshTokenCookie(HttpResponseMessage response)
    {
        Assert.IsTrue(response.Headers.TryGetValues("Set-Cookie", out var cookies),
            "Response should have Set-Cookie header");

        var cookieValues = cookies.ToList();
        var refreshCookie = cookieValues.FirstOrDefault(c => c.StartsWith("refresh_token="));
        Assert.IsNotNull(refreshCookie, "Set-Cookie should contain refresh_token");
        Assert.IsTrue(refreshCookie.Contains("httponly", StringComparison.OrdinalIgnoreCase),
            "refresh_token cookie should be HttpOnly");
        Assert.IsTrue(refreshCookie.Contains("samesite=strict", StringComparison.OrdinalIgnoreCase),
            "refresh_token cookie should be SameSite=Strict");
        Assert.IsTrue(refreshCookie.Contains("path=/api/auth", StringComparison.OrdinalIgnoreCase),
            "refresh_token cookie should be scoped to /api/auth");
    }

    /// <summary>Extracts the refresh_token cookie value from Set-Cookie for use in subsequent requests.</summary>
    private static string? ExtractRefreshTokenCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        var setCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        if (setCookie is null) return null;

        // Extract "refresh_token=value" (before the first semicolon)
        var semiIndex = setCookie.IndexOf(';');
        return semiIndex > 0 ? setCookie[..semiIndex] : setCookie;
    }
}
