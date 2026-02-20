namespace LemonDo.Api.Middleware;

/// <summary>
/// Adds standard security headers to every HTTP response.
/// Should be placed early in the pipeline (after error handling).
/// </summary>
/// <remarks>
/// Headers added: X-Content-Type-Options (nosniff), X-Frame-Options (DENY),
/// Referrer-Policy (strict-origin-when-cross-origin), X-XSS-Protection (0),
/// Content-Security-Policy (default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline';
/// img-src 'self' data:; font-src 'self' data:).
/// </remarks>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    private static readonly string[] DocsPathPrefixes = ["/scalar", "/openapi"];

    /// <summary>Adds security headers and invokes the next middleware.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["X-XSS-Protection"] = "0";
            headers["Cache-Control"] = "no-store";

            // Skip restrictive CSP for API documentation pages — Scalar loads JS/CSS from CDN
            var path = context.Request.Path.Value ?? string.Empty;
            if (!IsDocsPath(path))
            {
                headers["Content-Security-Policy"] =
                    "default-src 'self'; script-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self' data:";
            }

            return Task.CompletedTask;
        });

        await next(context);
    }

    private static bool IsDocsPath(string path)
    {
        foreach (var prefix in DocsPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
