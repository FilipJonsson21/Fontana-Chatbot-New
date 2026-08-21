using Fontana.AI.WebAPI.Middleware;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fontana.AI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private const string CookieName = "frixos_admin";

        public AdminController(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _env = env;
        }

        // POST /api/admin/login — validerar lösenord och sätter admin-cookie
        [HttpPost("login")]
        [EnableRateLimiting("admin-login")]
        public IActionResult Login([FromBody] AdminLoginRequest request)
        {
            var adminPassword = _configuration["AdminPassword"] ?? "";
            var apiKey = _configuration["ApiKey"] ?? "";

            if (string.IsNullOrEmpty(adminPassword) || string.IsNullOrEmpty(apiKey))
                return StatusCode(503, new { error = "AdminPassword eller ApiKey är inte konfigurerat på servern." });

            if (!string.Equals(request.Password, adminPassword, StringComparison.Ordinal))
                return Unauthorized(new { error = "Fel lösenord." });

            var token = AdminAuthMiddleware.GenerateToken(_configuration);

            Response.Cookies.Append(CookieName, token, new CookieOptions
            {
                HttpOnly  = true,                          // Inte tillgänglig via JavaScript
                Secure    = !_env.IsDevelopment(),         // Kräv HTTPS i produktion
                SameSite  = SameSiteMode.Strict,           // Skyddar mot CSRF
                Expires   = DateTimeOffset.UtcNow.Add(AdminAuthMiddleware.SessionLifetime)
            });

            return Ok(new { success = true });
        }

        // POST /api/admin/logout — tar bort admin-cookie
        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(CookieName);
            return Ok(new { success = true });
        }
    }

    public record AdminLoginRequest(string Password);
}
