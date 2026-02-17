namespace LemonDo.Api.Tests.Notifications;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Endpoints;
using LemonDo.Api.Tests.Infrastructure;

[TestClass]
public sealed class NotificationEndpointTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;
    private static HttpClient _anonymousClient = null!;

    [ClassInitialize]
    public static async Task ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = await _factory.CreateAuthenticatedClientAsync();
        _anonymousClient = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _anonymousClient.Dispose();
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Should_Return401_When_ListNotificationsUnauthenticated()
    {
        var response = await _anonymousClient.GetAsync("/api/notifications");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_GetUnreadCountUnauthenticated()
    {
        var response = await _anonymousClient.GetAsync("/api/notifications/unread-count");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_MarkAsReadUnauthenticated()
    {
        var response = await _anonymousClient.PostAsync(
            $"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_MarkAllAsReadUnauthenticated()
    {
        var response = await _anonymousClient.PostAsync("/api/notifications/read-all", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_HaveWelcomeNotification_When_NewUserRegisters()
    {
        // Register a fresh user to get a clean notification state
        var email = $"notif-welcome-{Guid.NewGuid():N}@lemondo.dev";
        using var freshClient = await RegisterAndAuthenticateAsync(email);

        var response = await freshClient.GetAsync("/api/notifications");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NotificationListResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(result);
        Assert.IsGreaterThanOrEqualTo(result.TotalCount, 1, "New user should have at least 1 notification");
        Assert.IsTrue(result.Items.Any(n => n.Type == "Welcome"),
            "New user should have a Welcome notification");
    }

    [TestMethod]
    public async Task Should_ReturnPaginatedResults_When_ListNotifications()
    {
        var response = await _client.GetAsync("/api/notifications?page=1&pageSize=5");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<NotificationListResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Page);
        Assert.AreEqual(5, result.PageSize);
        Assert.IsNotNull(result.Items);
        // Each item should have required fields
        foreach (var item in result.Items)
        {
            Assert.IsNotNull(item.Id);
            Assert.IsNotNull(item.Type);
            Assert.IsNotNull(item.Title);
            Assert.IsNotNull(item.CreatedAt);
        }
    }

    [TestMethod]
    public async Task Should_ReturnUnreadCount_When_Authenticated()
    {
        // Register a fresh user for predictable unread count
        var email = $"notif-unread-{Guid.NewGuid():N}@lemondo.dev";
        using var freshClient = await RegisterAndAuthenticateAsync(email);

        var response = await freshClient.GetAsync("/api/notifications/unread-count");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<UnreadCountResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(result);
        // Welcome notification should be unread
        Assert.IsGreaterThanOrEqualTo(result.Count, 1, "New user should have at least 1 unread notification");
    }

    [TestMethod]
    public async Task Should_MarkNotificationAsRead_When_ValidId()
    {
        // Register a fresh user with a known welcome notification
        var email = $"notif-read-{Guid.NewGuid():N}@lemondo.dev";
        using var freshClient = await RegisterAndAuthenticateAsync(email);

        // Get the notification list to find the welcome notification
        var listResponse = await freshClient.GetAsync("/api/notifications");
        var list = await listResponse.Content.ReadFromJsonAsync<NotificationListResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(list);
        Assert.IsNotEmpty(list.Items);

        var notificationId = list.Items[0].Id;

        // Mark it as read
        var markResponse = await freshClient.PostAsync(
            $"/api/notifications/{notificationId}/read", null);
        Assert.AreEqual(HttpStatusCode.OK, markResponse.StatusCode);

        // Verify unread count decreased
        var countResponse = await freshClient.GetAsync("/api/notifications/unread-count");
        var count = await countResponse.Content.ReadFromJsonAsync<UnreadCountResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(count);
        Assert.AreEqual(0, count.Count);
    }

    [TestMethod]
    public async Task Should_MarkAllAsRead_When_Authenticated()
    {
        // Register a fresh user
        var email = $"notif-readall-{Guid.NewGuid():N}@lemondo.dev";
        using var freshClient = await RegisterAndAuthenticateAsync(email);

        // Verify there's at least 1 unread
        var beforeResponse = await freshClient.GetAsync("/api/notifications/unread-count");
        var beforeCount = await beforeResponse.Content.ReadFromJsonAsync<UnreadCountResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(beforeCount);
        Assert.IsGreaterThanOrEqualTo(beforeCount.Count, 1);

        // Mark all as read
        var markAllResponse = await freshClient.PostAsync("/api/notifications/read-all", null);
        Assert.AreEqual(HttpStatusCode.OK, markAllResponse.StatusCode);

        // Verify count is 0
        var afterResponse = await freshClient.GetAsync("/api/notifications/unread-count");
        var afterCount = await afterResponse.Content.ReadFromJsonAsync<UnreadCountResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(afterCount);
        Assert.AreEqual(0, afterCount.Count);
    }

    [TestMethod]
    public async Task Should_ReturnBadRequest_When_MarkReadWithInvalidGuid()
    {
        var response = await _client.PostAsync("/api/notifications/not-a-guid/read", null);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnNotFound_When_MarkReadWithNonExistentId()
    {
        var response = await _client.PostAsync(
            $"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnNotFound_When_UserTriesToReadAnotherUsersNotification()
    {
        // Register user A and get their notification
        var emailA = $"notif-userA-{Guid.NewGuid():N}@lemondo.dev";
        using var clientA = await RegisterAndAuthenticateAsync(emailA);

        var listResponse = await clientA.GetAsync("/api/notifications");
        var list = await listResponse.Content.ReadFromJsonAsync<NotificationListResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(list);
        Assert.IsNotEmpty(list.Items);
        var userANotificationId = list.Items[0].Id;

        // Register user B and try to mark user A's notification as read
        var emailB = $"notif-userB-{Guid.NewGuid():N}@lemondo.dev";
        using var clientB = await RegisterAndAuthenticateAsync(emailB);

        var response = await clientB.PostAsync(
            $"/api/notifications/{userANotificationId}/read", null);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    /// <summary>Registers a new user via the API and returns an authenticated HttpClient.</summary>
    private async Task<HttpClient> RegisterAndAuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "TestPass123!", "Test Notif User"));
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }
}
