namespace LemonDo.Api.Tests.Infrastructure.Security;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using LemonDo.Api.Contracts.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

/// <summary>
/// Extension methods for <see cref="HttpClient"/> and <see cref="CustomWebApplicationFactory"/>
/// used across security tests.
/// </summary>
public static class SecurityTestExtensions
{
    private static readonly JsonSerializerOptions JsonOpts = TestJsonOptions.Default;

    /// <summary>Sends a request with no authentication token.</summary>
    public static async Task<HttpResponseMessage> SendUnauthenticatedAsync(
        this CustomWebApplicationFactory factory, EndpointDescriptor endpoint)
    {
        using var client = factory.CreateClient();
        return await client.SendEndpointAsync(endpoint);
    }

    /// <summary>Sends a request with a specific bearer token.</summary>
    public static async Task<HttpResponseMessage> SendWithTokenAsync(
        this CustomWebApplicationFactory factory, EndpointDescriptor endpoint, string token)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendEndpointAsync(endpoint);
    }

    /// <summary>Sends a request with an empty bearer token.</summary>
    public static async Task<HttpResponseMessage> SendWithEmptyBearerAsync(
        this CustomWebApplicationFactory factory, EndpointDescriptor endpoint)
    {
        using var client = factory.CreateClient();
        client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", "Bearer ");
        return await client.SendEndpointAsync(endpoint);
    }

    /// <summary>Dispatches a request based on the endpoint descriptor's method and body.</summary>
    public static async Task<HttpResponseMessage> SendEndpointAsync(
        this HttpClient client, EndpointDescriptor endpoint)
    {
        var path = endpoint.ResolvedPath;
        return await client.SendMethodAsync(endpoint.Method, path, endpoint.Body);
    }

    /// <summary>Sends an arbitrary HTTP method to a path with an optional body.</summary>
    public static async Task<HttpResponseMessage> SendMethodAsync(
        this HttpClient client, HttpMethod method, string path, object? body = null)
    {
        if (method == HttpMethod.Get)
            return await client.GetAsync(path);

        if (method == HttpMethod.Delete)
            return await client.DeleteAsync(path);

        if (method == HttpMethod.Post)
        {
            return body is not null
                ? await client.PostAsJsonAsync(path, body)
                : await client.PostAsync(path, null);
        }

        if (method == HttpMethod.Put)
        {
            return body is not null
                ? await client.PutAsJsonAsync(path, body)
                : await client.PutAsync(path, null);
        }

        // PATCH or others
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = new StringContent(
                JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }

    /// <summary>
    /// Registers a fresh user and returns an authenticated HttpClient.
    /// Centralizes the duplicated helper from multiple test files.
    /// </summary>
    public static async Task<HttpClient> RegisterFreshUserAsync(
        this CustomWebApplicationFactory factory, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@lemondo.dev";
        var client = factory.CreateClient();

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register",
            new { Email = email, Password = "TestPass123!", DisplayName = $"User {prefix}" });
        registerResponse.EnsureSuccessStatusCode();

        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        return client;
    }

    /// <summary>Creates a task and returns its ID.</summary>
    public static async Task<Guid> CreateTaskAsync(
        this HttpClient client, string title = "Security test task")
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new { Title = title });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        return body.GetProperty("id").GetGuid();
    }

    /// <summary>Extracts the refresh_token cookie value from a Set-Cookie header.</summary>
    public static string ExtractRefreshTokenCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
            return string.Empty;
        var setCookie = cookies.FirstOrDefault(c => c.StartsWith("refresh_token="));
        if (setCookie is null) return string.Empty;
        var semiIndex = setCookie.IndexOf(';');
        return semiIndex > 0 ? setCookie[..semiIndex] : setCookie;
    }
}
