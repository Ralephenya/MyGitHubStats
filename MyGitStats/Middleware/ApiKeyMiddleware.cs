using MyGitStats.Models;

namespace MyGitStats.Middleware;

// API Key Middleware
public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var apiKey = configuration.GetSection("Api").Get<ApiConfig>()?.ApiKey;
        
        if (string.IsNullOrEmpty(apiKey))
        {
            await next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing");
            return;
        }

        if (!apiKey.Equals(extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }

        await next(context);
    }
}