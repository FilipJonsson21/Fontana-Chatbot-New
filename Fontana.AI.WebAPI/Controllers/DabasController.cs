using Fontana.AI.Data;
using Fontana.AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        private readonly IMemoryCache _cache;
        private const string ProductCacheKey = "dabas_products";

        public DabasController(DabasClient dabasClient, ApplicationDbContext context, IConfiguration configuration, IMemoryCache cache)
        {
            _dabasClient = dabasClient;
            _context = context;
            _configuration = configuration;
            _cache = cache;
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

            // Upsert-sync: uppdatera befintliga, lägg till nya, ta bort utgångna
            var existing = await _context.DabasProducts.ToListAsync();
            var existingByGtin = existing.ToDictionary(p => p.Gtin);
            var incomingGtins = products.Select(p => p.Gtin).ToHashSet();
            var now = DateTime.UtcNow;

            // Ta bort produkter som inte längre finns i DABAS
            var toRemove = existing.Where(p => !incomingGtins.Contains(p.Gtin)).ToList();
            _context.DabasProducts.RemoveRange(toRemove);

            foreach (var product in products)
            {
                product.LastSynced = now;
                if (existingByGtin.TryGetValue(product.Gtin, out var existingProduct))
                {
                    // Uppdatera befintlig rad i stället för att radera och återskapa
                    existingProduct.ProductName  = product.ProductName;
                    existingProduct.Ingredients  = product.Ingredients;
                    existingProduct.Allergens    = product.Allergens;
                    existingProduct.Origin       = product.Origin;
                    existingProduct.Nutrition    = product.Nutrition;
                    existingProduct.LastSynced   = now;
                }
                else
                {
                    _context.DabasProducts.Add(product);
                }
            }

            await _context.SaveChangesAsync();

            // Rensa produktcachen så ChatService hämtar ny data nästa anrop
            _cache.Remove(ProductCacheKey);

            return Ok(new { message = "Sync slutförd.", synced = products.Count });
        }
    }
}
