namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
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
        var request = new RegisterRequest("new@lemondo.dev", "NewPass123!", "New User");

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        Assert.IsNotNull(auth.AccessToken);
        Assert.AreEqual("new@lemondo.dev", auth.User.Email);
        Assert.AreEqual("New User", auth.User.DisplayName);

        // Refresh token should be in HttpOnly cookie, NOT in JSON body
        AssertRefreshTokenCookie(response);
    }

    [TestMethod]
    public async Task Should_ReturnConflict_When_DuplicateEmail()
    {
        // Test user is pre-seeded by factory
        var request = new RegisterRequest(
            CustomWebApplicationFactory.TestUserEmail,
            "AnotherPass123!",
            "Duplicate");

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_WeakPassword()
    {
        var request = new RegisterRequest("weak@lemondo.dev", "short", "Weak Password");

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/register", request);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Login_When_ValidCredentials()
    {
        var request = new LoginRequest(
            CustomWebApplicationFactory.TestUserEmail,
            CustomWebApplicationFactory.TestUserPassword);

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/login", request);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var auth = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);
        Assert.IsNotNull(auth.AccessToken);
        Assert.AreEqual(CustomWebApplicationFactory.TestUserEmail, auth.User.Email);

        // Refresh token should be in HttpOnly cookie, NOT in JSON body
        AssertRefreshTokenCookie(response);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_WrongPassword()
    {
        var request = new LoginRequest(
            CustomWebApplicationFactory.TestUserEmail,
            "WrongPassword123!");

        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/login", request);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_NonexistentEmail()
    {
        var request = new LoginRequest("nobody@lemondo.dev", "Pass123!");

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
            new LoginRequest(CustomWebApplicationFactory.TestUserEmail, CustomWebApplicationFactory.TestUserPassword));
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
        Assert.AreEqual(CustomWebApplicationFactory.TestUserEmail, user.Email);
        Assert.AreEqual(CustomWebApplicationFactory.TestUserDisplayName, user.DisplayName);
    }

    [TestMethod]
    public async Task Should_InvalidateRefreshToken_When_Logout()
    {
        // Login to get tokens + cookie
        var loginResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.TestUserEmail, CustomWebApplicationFactory.TestUserPassword));
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
