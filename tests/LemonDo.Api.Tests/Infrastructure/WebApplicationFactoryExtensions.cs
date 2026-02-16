namespace LemonDo.Api.Tests.Infrastructure;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using LemonDo.Api.Contracts.Auth;
using Microsoft.AspNetCore.Mvc.Testing;

public static class WebApplicationFactoryExtensions
{
    /// <summary>Creates an HttpClient authenticated as the test user (User role).</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this WebApplicationFactory<Program> factory)
    {
        return await factory.CreateAuthenticatedClientAsync(
            CustomWebApplicationFactory.TestUserEmail,
            CustomWebApplicationFactory.TestUserPassword);
    }

    /// <summary>Creates an HttpClient authenticated as an Admin user.</summary>
    public static async Task<HttpClient> CreateAdminClientAsync(
        this WebApplicationFactory<Program> factory)
    {
        return await factory.CreateAuthenticatedClientAsync(
            CustomWebApplicationFactory.AdminUserEmail,
            CustomWebApplicationFactory.AdminUserPassword);
    }

    /// <summary>Creates an HttpClient authenticated as a SystemAdmin user.</summary>
    public static async Task<HttpClient> CreateSystemAdminClientAsync(
        this WebApplicationFactory<Program> factory)
    {
        return await factory.CreateAuthenticatedClientAsync(
            CustomWebApplicationFactory.SystemAdminUserEmail,
            CustomWebApplicationFactory.SystemAdminUserPassword);
    }

    /// <summary>Creates an HttpClient authenticated with specific credentials.</summary>
    public static async Task<HttpClient> CreateAuthenticatedClientAsync(
        this WebApplicationFactory<Program> factory, string email, string password)
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();

        var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authResponse!.AccessToken);

        return client;
    }
}
