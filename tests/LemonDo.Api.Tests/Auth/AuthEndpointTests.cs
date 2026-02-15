namespace LemonDo.Api.Tests.Auth;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Tests.Infrastructure;

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
        Assert.IsNotNull(auth.RefreshToken);
        Assert.AreEqual("new@lemondo.dev", auth.User.Email);
        Assert.AreEqual("New User", auth.User.DisplayName);
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
        Assert.IsNotNull(auth.RefreshToken);
        Assert.AreEqual(CustomWebApplicationFactory.TestUserEmail, auth.User.Email);
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
    public async Task Should_RefreshToken_When_ValidRefreshToken()
    {
        // Login to get tokens
        var loginResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.TestUserEmail, CustomWebApplicationFactory.TestUserPassword));
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);

        // Refresh
        var refreshResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth.RefreshToken));

        Assert.AreEqual(HttpStatusCode.OK, refreshResponse.StatusCode);
        var newAuth = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(newAuth);
        Assert.AreNotEqual(auth.AccessToken, newAuth.AccessToken);
    }

    [TestMethod]
    public async Task Should_ReturnUnauthorized_When_RefreshWithInvalidToken()
    {
        var response = await _anonymousClient.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest("invalid-token"));

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
        // Login to get tokens
        var loginResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(CustomWebApplicationFactory.TestUserEmail, CustomWebApplicationFactory.TestUserPassword));
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.IsNotNull(auth);

        // Logout with the refresh token
        var logoutClient = _factory.CreateClient();
        logoutClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var logoutResponse = await logoutClient.PostAsJsonAsync("/api/auth/logout",
            new RefreshRequest(auth.RefreshToken));
        Assert.AreEqual(HttpStatusCode.OK, logoutResponse.StatusCode);

        // Refresh should now fail
        var refreshResponse = await _anonymousClient.PostAsJsonAsync("/api/auth/refresh",
            new RefreshRequest(auth.RefreshToken));
        Assert.AreEqual(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);

        logoutClient.Dispose();
    }
}
