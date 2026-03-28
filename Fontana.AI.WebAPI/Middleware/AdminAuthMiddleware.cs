using System.Security.Cryptography;
using System.Text;

namespace Fontana.AI.WebAPI.Middleware;

// Skyddar admin-sidor och känsliga API-endpoints med cookie-baserad session.
// Cookien innehåller ett HMAC-token som valideras utan databas.
public class AdminAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string CookieName = "frixos_admin";

    public async Task InvokeAsync(HttpContext context)
    {
        var path   = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Skyddade HTML-sidor
        bool isAdminPage = path.Equals("/faq-admin.html",    StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/conversations.html", StringComparison.OrdinalIgnoreCase);

        // Skyddade API-rutter
        // FAQ GET är publikt (chatboten läser FAQ), men ändringar kräver admin
        bool isAdminApi = path.StartsWith("/api/conversation", StringComparison.OrdinalIgnoreCase)
                       || (path.StartsWith("/api/faq", StringComparison.OrdinalIgnoreCase)
                           && !HttpMethods.IsGet(method));

        if (!isAdminPage && !isAdminApi)
        {
            await next(context);
            return;
        }

        // Validera admin-cookie
        var token = context.Request.Cookies[CookieName] ?? string.Empty;
        if (IsValidToken(token))
        {
            await next(context);
            return;
        }

        // Inte autentiserad — skicka till inloggningssidan eller returnera 401
        if (isAdminPage)
        {
            context.Response.Redirect("/admin-login.html");
        }
        else
        {
            context.Response.StatusCode  = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Admin-autentisering krävs.\"}");
        }
    }

    internal bool IsValidToken(string token) =>
        !string.IsNullOrEmpty(token) &&
        string.Equals(token, GenerateToken(configuration), StringComparison.Ordinal);

    // Genererar ett statslöst HMAC-token från AdminPassword + ApiKey som nyckel.
    // Ingen databas behövs — token är giltigt så länge lösenordet inte ändras.
    internal static string GenerateToken(IConfiguration configuration)
    {
        var password = configuration["AdminPassword"] ?? "";
        var secret   = configuration["ApiKey"]        ?? "fontana-fallback-secret";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }
}
