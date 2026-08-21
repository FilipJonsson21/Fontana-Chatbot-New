using Fontana.AI.Data;
using Fontana.AI.Models;
using Fontana.AI.Services;
using Fontana.AI.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Fontana.AI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WineController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly WineSyncClient _wineSyncClient;
        private const string WineCacheKey = "wines";

        public WineController(ApplicationDbContext context, IMemoryCache cache, WineSyncClient wineSyncClient)
        {
            _context = context;
            _cache = cache;
            _wineSyncClient = wineSyncClient;
        }

        // POST /api/wine/sync — hämtar vin/sprit från fontana.se och synkar mot databasen
        [HttpPost("sync")]
        public async Task<IActionResult> SyncWines()
        {
            var wines = await _wineSyncClient.GetAllWinesAsync();
            if (wines.Count == 0)
                return Ok(new { message = "Inga viner/spritsorter hittades vid synk.", found = 0, added = 0, updated = 0 });

            var existing = await _context.Wines.ToListAsync();
            var (added, updated) = WineSyncMerge.Apply(_context, existing, wines);
            await _context.SaveChangesAsync();
            _cache.Remove(WineCacheKey);

            return Ok(new { message = "Vinsynk slutförd.", found = wines.Count, added, updated });
        }

        // GET /api/wine — hämta alla viner/sprit
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var wines = await _context.Wines.OrderBy(w => w.Name).ToListAsync();
            return Ok(wines);
        }

        // GET /api/wine/{id} — hämta ett specifikt vin/sprit
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine is null)
                return NotFound($"Vin/sprit med id {id} hittades inte.");

            return Ok(wine);
        }

        // POST /api/wine — skapa nytt vin/sprit
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WineRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var wine = new Wine
            {
                Name = request.Name,
                Type = request.Type,
                Producer = request.Producer,
                Origin = request.Origin,
                AlcoholPercent = request.AlcoholPercent,
                AssortmentType = request.AssortmentType,
                SystembolagNumber = request.SystembolagNumber,
                Description = request.Description,
                Url = request.Url
            };

            _context.Wines.Add(wine);
            await _context.SaveChangesAsync();
            _cache.Remove(WineCacheKey);

            return CreatedAtAction(nameof(GetById), new { id = wine.Id }, wine);
        }

        // PUT /api/wine/{id} — uppdatera befintligt vin/sprit
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] WineRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var wine = await _context.Wines.FindAsync(id);
            if (wine is null)
                return NotFound($"Vin/sprit med id {id} hittades inte.");

            wine.Name = request.Name;
            wine.Type = request.Type;
            wine.Producer = request.Producer;
            wine.Origin = request.Origin;
            wine.AlcoholPercent = request.AlcoholPercent;
            wine.AssortmentType = request.AssortmentType;
            wine.SystembolagNumber = request.SystembolagNumber;
            wine.Description = request.Description;
            wine.Url = request.Url;

            await _context.SaveChangesAsync();
            _cache.Remove(WineCacheKey);
            return Ok(wine);
        }

        // DELETE /api/wine/{id} — ta bort vin/sprit
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var wine = await _context.Wines.FindAsync(id);
            if (wine is null)
                return NotFound($"Vin/sprit med id {id} hittades inte.");

            _context.Wines.Remove(wine);
            await _context.SaveChangesAsync();
            _cache.Remove(WineCacheKey);
            return NoContent();
        }
    }
}
