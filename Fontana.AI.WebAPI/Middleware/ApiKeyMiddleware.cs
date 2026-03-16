namespace Fontana.AI.WebAPI.Middleware;

// Kontrollerar att alla API-anrop innehåller en giltig X-Api-Key header.
// Scalar- och OpenAPI-endpoints är undantagna så att dokumentationen
// fortfarande är åtkomlig i Development utan nyckel.
public class ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string ApiKeyHeader = "X-Api-Key";

    public async Task InvokeAsync(HttpContext context)
    {
        // Låt Scalar, OpenAPI och statiska filer (demo-sidan) passera utan nyckel
        var path = context.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/scalar", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("/openapi", StringComparison.OrdinalIgnoreCase) ||
            !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var configuredKey = configuration["ApiKey"];

        if (!context.Request.Headers.TryGetValue(ApiKeyHeader, out var providedKey) ||
            !string.Equals(configuredKey, providedKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Ogiltig eller saknad API-nyckel.\"}");
            return;
        }

        await next(context);
    }
}
