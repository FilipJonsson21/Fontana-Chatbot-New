using Fontana.AI.Data;
using Fontana.AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fontana.AI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DabasController : ControllerBase
    {
        private readonly DabasClient _dabasClient;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public DabasController(DabasClient dabasClient, ApplicationDbContext context, IConfiguration configuration)
        {
            _dabasClient = dabasClient;
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncProducts()
        {
            var supplierGln = _configuration["Dabas:SupplierGln"] ?? "";

            if (string.IsNullOrEmpty(supplierGln))
                return BadRequest("Dabas:SupplierGln saknas i appsettings.json.");

            var products = await _dabasClient.GetProductsBySupplierGlnAsync(supplierGln);

            if (!products.Any())
                return Ok(new { message = "Inga produkter hittades i DABAS för detta GLN-nummer.", synced = 0 });

            // Rensa gamla och spara nya (enkel full-sync)
            var existing = await _context.DabasProducts.ToListAsync();
            _context.DabasProducts.RemoveRange(existing);

            var now = DateTime.UtcNow;
            foreach (var product in products)
            {
                product.LastSynced = now;
            }

            await _context.DabasProducts.AddRangeAsync(products);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Sync slutförd.", synced = products.Count });
        }
    }
}
