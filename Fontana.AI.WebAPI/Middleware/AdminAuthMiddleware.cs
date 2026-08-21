using System.Security.Cryptography;
using System.Text;

namespace Fontana.AI.WebAPI.Middleware;

// Skyddar admin-sidor och känsliga API-endpoints med cookie-baserad session.
// Cookien innehåller ett tidsstämplat HMAC-token som valideras utan databas.
public class AdminAuthMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private const string CookieName = "frixos_admin";

    public static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    public async Task InvokeAsync(HttpContext context)
    {
        var path   = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Skyddade HTML-sidor
        bool isAdminPage = path.Equals("/faq-admin.html",     StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/recipe-admin.html",  StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/wine-admin.html",    StringComparison.OrdinalIgnoreCase)
                        || path.Equals("/conversations.html", StringComparison.OrdinalIgnoreCase);

        // Skyddade API-rutter
        // GET är publikt (chatboten läser FAQ/recept/vin), men ändringar kräver admin
        bool isAdminApi = path.StartsWith("/api/conversation", StringComparison.OrdinalIgnoreCase)
                       || (path.StartsWith("/api/faq",    StringComparison.OrdinalIgnoreCase) && !HttpMethods.IsGet(method))
                       || (path.StartsWith("/api/recipe", StringComparison.OrdinalIgnoreCase) && !HttpMethods.IsGet(method))
                       || (path.StartsWith("/api/wine",   StringComparison.OrdinalIgnoreCase) && !HttpMethods.IsGet(method));

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

    // Token-format: "<unix-tidsstämpel>.<base64-HMAC>" — låter oss upptäcka utgångna sessioner
    // utan att behöva spara sessionstillstånd i databasen.
    internal bool IsValidToken(string token)
    {
        if (string.IsNullOrEmpty(token))
            return false;

        var parts = token.Split('.', 2);
        if (parts.Length != 2 || !long.TryParse(parts[0], out var issuedAtUnix))
            return false;

        var issuedAt = DateTimeOffset.FromUnixTimeSeconds(issuedAtUnix);
        var now = DateTimeOffset.UtcNow;

        if (now - issuedAt > SessionLifetime)
            return false;
        // Litet toleransfönster mot klockskillnader, men skydda mot uppenbart förfalskade framtida tokens
        if (issuedAt - now > TimeSpan.FromMinutes(1))
            return false;

        try
        {
            var provided = Convert.FromBase64String(parts[1]);
            var expected = Convert.FromBase64String(ComputeSignature(configuration, parts[0]));
            return CryptographicOperations.FixedTimeEquals(provided, expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    // Genererar ett nytt, tidsstämplat token — giltigt i SessionLifetime från utfärdandet.
    internal static string GenerateToken(IConfiguration configuration)
    {
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        return $"{issuedAt}.{ComputeSignature(configuration, issuedAt)}";
    }

    // HMAC över AdminPassword + tidsstämpel, nyckelad med ApiKey.
    private static string ComputeSignature(IConfiguration configuration, string issuedAt)
    {
        var password = configuration["AdminPassword"] ?? "";
        var secret   = configuration["ApiKey"]        ?? "fontana-fallback-secret";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{password}:{issuedAt}"));
        return Convert.ToBase64String(hash);
    }
}
