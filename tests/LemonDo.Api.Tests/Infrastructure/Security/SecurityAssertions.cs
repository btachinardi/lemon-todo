namespace LemonDo.Api.Tests.Infrastructure.Security;

using System.Net;

/// <summary>
/// Reusable assertion helpers for security tests.
/// </summary>
public static class SecurityAssertions
{
    private static readonly string[] LeakagePatterns =
    [
        "StackTrace",
        "at LemonDo",
        "Microsoft.EntityFrameworkCore",
        "System.Exception",
        "System.NullReferenceException",
        "UNIQUE constraint",
        "SqliteException",
        "SqlException",
    ];

    /// <summary>Asserts the response body does not contain stack traces, namespace paths, or EF Core internals.</summary>
    public static async Task AssertNoInfoLeakageAsync(HttpResponseMessage response, string context)
    {
        var body = await response.Content.ReadAsStringAsync();
        AssertNoInfoLeakage(body, context);
    }

    /// <summary>Asserts the body string does not contain stack traces, namespace paths, or EF Core internals.</summary>
    public static void AssertNoInfoLeakage(string body, string context)
    {
        foreach (var pattern in LeakagePatterns)
        {
            Assert.IsFalse(body.Contains(pattern, StringComparison.OrdinalIgnoreCase),
                $"[{context}] Response body must not contain '{pattern}'");
        }
    }

    /// <summary>Asserts the response is not a 500 Internal Server Error.</summary>
    public static void AssertNotServerError(HttpResponseMessage response, string context)
    {
        Assert.AreNotEqual(HttpStatusCode.InternalServerError, response.StatusCode,
            $"[{context}] Must not return 500 Internal Server Error");
    }

    /// <summary>Asserts the response is not a 500 and is one of the expected statuses.</summary>
    public static void AssertSafeResponse(HttpResponseMessage response, string context,
        params HttpStatusCode[] expectedStatuses)
    {
        AssertNotServerError(response, context);
        if (expectedStatuses.Length > 0)
        {
            Assert.IsTrue(expectedStatuses.Contains(response.StatusCode),
                $"[{context}] Expected one of [{string.Join(", ", expectedStatuses)}], got {response.StatusCode}");
        }
    }

    /// <summary>Asserts standard security headers are present on the response.</summary>
    public static void AssertSecurityHeaders(HttpResponseMessage response, string context)
    {
        Assert.IsTrue(response.Headers.Contains("X-Content-Type-Options"),
            $"[{context}] X-Content-Type-Options header must be present");
        Assert.AreEqual("nosniff",
            response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault(),
            $"[{context}] X-Content-Type-Options must be 'nosniff'");

        Assert.IsTrue(response.Headers.Contains("X-Frame-Options"),
            $"[{context}] X-Frame-Options header must be present");
    }

    /// <summary>Asserts that specific sensitive field names are absent from the response body.</summary>
    public static void AssertNoSensitiveFields(string body, string context, params string[] forbiddenFields)
    {
        foreach (var field in forbiddenFields)
        {
            Assert.IsFalse(body.Contains(field, StringComparison.OrdinalIgnoreCase),
                $"[{context}] Response must not contain field '{field}'");
        }
    }
}
