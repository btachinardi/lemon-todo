namespace LemonDo.Api.Middleware;

using System.Net;
using System.Text.Json;

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

            var response = new
            {
                type = "internal_server_error",
                title = "An unexpected error occurred.",
                status = 500
            };

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
