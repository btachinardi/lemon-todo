namespace LemonDo.Api.Tests.Onboarding;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using LemonDo.Api.Endpoints;
using LemonDo.Api.Tests.Infrastructure;

[TestClass]
public sealed class OnboardingEndpointTests
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
    public async Task Should_Return401_When_GetStatusUnauthenticated()
    {
        var response = await _anonymousClient.GetAsync("/api/onboarding/status");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_Return401_When_CompleteUnauthenticated()
    {
        var response = await _anonymousClient.PostAsync("/api/onboarding/complete", null);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [TestMethod]
    public async Task Should_ReturnNotCompleted_When_NewUser()
    {
        var email = $"onboard-new-{Guid.NewGuid():N}@lemondo.dev";
        using var client = await RegisterAndAuthenticateAsync(email);

        var response = await client.GetAsync("/api/onboarding/status");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var status = await response.Content.ReadFromJsonAsync<OnboardingStatusResponse>(
            TestJsonOptions.Default);
        Assert.IsNotNull(status);
        Assert.IsFalse(status.Completed);
        Assert.IsNull(status.CompletedAt);
    }

    [TestMethod]
    public async Task Should_SetCompleted_When_CompleteOnboarding()
    {
        var email = $"onboard-complete-{Guid.NewGuid():N}@lemondo.dev";
        using var client = await RegisterAndAuthenticateAsync(email);

        // Complete onboarding
        var completeResponse = await client.PostAsync("/api/onboarding/complete", null);
        Assert.AreEqual(HttpStatusCode.OK, completeResponse.StatusCode);

        var completeStatus = await completeResponse.Content
            .ReadFromJsonAsync<OnboardingStatusResponse>(TestJsonOptions.Default);
        Assert.IsNotNull(completeStatus);
        Assert.IsTrue(completeStatus.Completed);
        Assert.IsNotNull(completeStatus.CompletedAt);
    }

    [TestMethod]
    public async Task Should_BeIdempotent_When_CompleteCalledTwice()
    {
        var email = $"onboard-idempotent-{Guid.NewGuid():N}@lemondo.dev";
        using var client = await RegisterAndAuthenticateAsync(email);

        // Complete twice
        var firstResponse = await client.PostAsync("/api/onboarding/complete", null);
        Assert.AreEqual(HttpStatusCode.OK, firstResponse.StatusCode);

        var secondResponse = await client.PostAsync("/api/onboarding/complete", null);
        Assert.AreEqual(HttpStatusCode.OK, secondResponse.StatusCode);

        // Verify status is still completed
        var statusResponse = await client.GetAsync("/api/onboarding/status");
        var status = await statusResponse.Content
            .ReadFromJsonAsync<OnboardingStatusResponse>(TestJsonOptions.Default);
        Assert.IsNotNull(status);
        Assert.IsTrue(status.Completed);
    }

    [TestMethod]
    public async Task Should_ReturnCorrectTimestamp_When_StatusAfterComplete()
    {
        var email = $"onboard-timestamp-{Guid.NewGuid():N}@lemondo.dev";
        using var client = await RegisterAndAuthenticateAsync(email);

        var beforeComplete = DateTimeOffset.UtcNow;

        // Complete onboarding
        var completeResponse = await client.PostAsync("/api/onboarding/complete", null);
        Assert.AreEqual(HttpStatusCode.OK, completeResponse.StatusCode);

        var afterComplete = DateTimeOffset.UtcNow;

        // Verify status endpoint returns the timestamp
        var statusResponse = await client.GetAsync("/api/onboarding/status");
        var status = await statusResponse.Content
            .ReadFromJsonAsync<OnboardingStatusResponse>(TestJsonOptions.Default);
        Assert.IsNotNull(status);
        Assert.IsTrue(status.Completed);
        Assert.IsNotNull(status.CompletedAt);

        var completedAt = DateTimeOffset.Parse(status.CompletedAt);
        Assert.IsTrue(completedAt >= beforeComplete.AddSeconds(-1),
            $"CompletedAt ({completedAt}) should be >= {beforeComplete.AddSeconds(-1)}");
        Assert.IsTrue(completedAt <= afterComplete.AddSeconds(1),
            $"CompletedAt ({completedAt}) should be <= {afterComplete.AddSeconds(1)}");
    }

    /// <summary>Registers a new user via the API and returns an authenticated HttpClient.</summary>
    private async Task<HttpClient> RegisterAndAuthenticateAsync(string email)
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest(email, "TestPass123!", "Test Onboard User"));
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }
}
