namespace LemonDo.Api.Middleware;

using System.Net;
using System.Text.Json;

/// <summary>
/// Catches unhandled exceptions and returns a structured JSON error response (500).
/// In development, includes the full exception detail in the response body.
/// </summary>
public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var isDev = context.RequestServices.GetService<IHostEnvironment>()?.IsDevelopment() == true;
            var response = new
            {
                type = "internal_server_error",
                title = "An unexpected error occurred.",
                status = 500,
                detail = isDev ? ex.ToString() : null
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
