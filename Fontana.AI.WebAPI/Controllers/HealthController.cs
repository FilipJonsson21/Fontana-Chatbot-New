using Fontana.AI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fontana.AI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public HealthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/health — kontrollerar att tjänsten och databasen är uppe
        [HttpGet]
        public async Task<IActionResult> GetHealth()
        {
            try
            {
                await _context.Database.ExecuteSqlRawAsync("SELECT 1");
                return Ok(new { status = "healthy", database = "connected", timestamp = DateTime.UtcNow });
            }
            catch (Exception ex)
            {
                return StatusCode(503, new { status = "unhealthy", database = "error", error = ex.Message });
            }
        }
    }
}
