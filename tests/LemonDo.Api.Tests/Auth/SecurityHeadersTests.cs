namespace LemonDo.Api.Tests.Auth;

using System.Net;
using LemonDo.Api.Tests.Infrastructure;

[TestClass]
public sealed class SecurityHeadersTests
{
    private static CustomWebApplicationFactory _factory = null!;
    private static HttpClient _client = null!;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        _factory = new CustomWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [TestMethod]
    public async Task Should_IncludeXContentTypeOptions_OnEveryResponse()
    {
        var response = await _client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"));
        Assert.AreEqual("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());
    }

    [TestMethod]
    public async Task Should_IncludeXFrameOptions_OnEveryResponse()
    {
        var response = await _client.GetAsync("/health");

        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"));
        Assert.AreEqual("DENY", response.Headers.GetValues("X-Frame-Options").First());
    }

    [TestMethod]
    public async Task Should_IncludeReferrerPolicy_OnEveryResponse()
    {
        var response = await _client.GetAsync("/health");

        Assert.IsTrue(response.Headers.Contains("Referrer-Policy"));
        Assert.AreEqual("strict-origin-when-cross-origin",
            response.Headers.GetValues("Referrer-Policy").First());
    }

    [TestMethod]
    public async Task Should_IncludeXXssProtection_OnEveryResponse()
    {
        var response = await _client.GetAsync("/health");

        Assert.IsTrue(response.Headers.Contains("X-XSS-Protection"));
        Assert.AreEqual("0", response.Headers.GetValues("X-XSS-Protection").First());
    }

    [TestMethod]
    public async Task Should_IncludeSecurityHeaders_On401Response()
    {
        // Even error responses should have security headers
        var response = await _client.GetAsync("/api/tasks");

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"));
        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"));
    }

    [TestMethod]
    public async Task Should_NotIncludeRestrictiveCsp_OnScalarDocsPage()
    {
        var response = await _client.GetAsync("/scalar/v1");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        // Scalar needs CDN scripts + inline scripts — restrictive 'self'-only CSP must not be applied
        var cspValues = response.Headers.TryGetValues("Content-Security-Policy", out var values)
            ? string.Join("; ", values)
            : null;
        Assert.IsTrue(
            cspValues is null || !cspValues.Contains("script-src 'self'"),
            $"Scalar docs page should not have restrictive script-src 'self' CSP, but got: {cspValues}");
    }

    [TestMethod]
    public async Task Should_NotIncludeRestrictiveCsp_OnOpenApiEndpoint()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var cspValues = response.Headers.TryGetValues("Content-Security-Policy", out var values)
            ? string.Join("; ", values)
            : null;
        Assert.IsTrue(
            cspValues is null || !cspValues.Contains("script-src 'self'"),
            $"OpenAPI endpoint should not have restrictive CSP, but got: {cspValues}");
    }

    [TestMethod]
    public async Task Should_StillIncludeRestrictiveCsp_OnApiEndpoints()
    {
        // Regression guard: regular API endpoints must keep the restrictive CSP
        var response = await _client.GetAsync("/health");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsTrue(response.Headers.Contains("Content-Security-Policy"));
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("script-src 'self'", csp);
    }
}
