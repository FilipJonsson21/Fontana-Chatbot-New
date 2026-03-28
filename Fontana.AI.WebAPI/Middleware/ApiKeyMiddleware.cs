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

        // Tillåt anrop från betrodda origins — samma server ELLER konfigurerade externa domäner
        // Browsern sätter Origin/Referer automatiskt och kan inte fejkas cross-origin
        var serverBase     = $"{context.Request.Scheme}://{context.Request.Host}";
        var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
        var origin  = context.Request.Headers["Origin"].FirstOrDefault()  ?? string.Empty;
        var referer = context.Request.Headers["Referer"].FirstOrDefault() ?? string.Empty;

        bool isTrusted = origin.StartsWith(serverBase, StringComparison.OrdinalIgnoreCase)
                      || referer.StartsWith(serverBase, StringComparison.OrdinalIgnoreCase)
                      || allowedOrigins.Any(o => origin.StartsWith(o, StringComparison.OrdinalIgnoreCase));

        if (isTrusted)
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
