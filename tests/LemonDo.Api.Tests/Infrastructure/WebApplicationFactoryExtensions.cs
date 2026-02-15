namespace LemonDo.Api.Tests.Infrastructure;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

public static class WebApplicationFactoryExtensions
{
    /// <summary>Creates an HttpClient authenticated as the test user.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this WebApplicationFactory<Program> factory)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(
                CustomWebApplicationFactory.TestUserEmail,
                CustomWebApplicationFactory.TestUserPassword));
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        return client;
    }
}
