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
    public class RecipeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly RecipeSyncClient _recipeSyncClient;
        private const string RecipeCacheKey = "recipes";

        public RecipeController(ApplicationDbContext context, IMemoryCache cache, RecipeSyncClient recipeSyncClient)
        {
            _context = context;
            _cache = cache;
            _recipeSyncClient = recipeSyncClient;
        }

        // POST /api/recipe/sync — hämtar recept från fontana.se och synkar mot databasen
        [HttpPost("sync")]
        public async Task<IActionResult> SyncRecipes()
        {
            var recipes = await _recipeSyncClient.GetAllRecipesAsync();
            if (recipes.Count == 0)
                return Ok(new { message = "Inga recept hittades vid synk.", found = 0, added = 0, updated = 0 });

            var existing = await _context.Recipes.ToListAsync();
            var (added, updated) = RecipeSyncMerge.Apply(_context, existing, recipes);
            await _context.SaveChangesAsync();
            _cache.Remove(RecipeCacheKey);

            return Ok(new { message = "Receptsynk slutförd.", found = recipes.Count, added, updated });
        }

        // GET /api/recipe — hämta alla recept
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var recipes = await _context.Recipes.OrderBy(r => r.Title).ToListAsync();
            return Ok(recipes);
        }

        // GET /api/recipe/{id} — hämta ett specifikt recept
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe is null)
                return NotFound($"Recept med id {id} hittades inte.");

            return Ok(recipe);
        }

        // POST /api/recipe — skapa nytt recept
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RecipeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var recipe = new Recipe
            {
                Title = request.Title,
                MainIngredient = request.MainIngredient,
                MealType = request.MealType,
                Occasion = request.Occasion,
                RecipeType = request.RecipeType,
                Description = request.Description,
                Url = request.Url
            };

            _context.Recipes.Add(recipe);
            await _context.SaveChangesAsync();
            _cache.Remove(RecipeCacheKey);

            return CreatedAtAction(nameof(GetById), new { id = recipe.Id }, recipe);
        }

        // PUT /api/recipe/{id} — uppdatera befintligt recept
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] RecipeRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe is null)
                return NotFound($"Recept med id {id} hittades inte.");

            recipe.Title = request.Title;
            recipe.MainIngredient = request.MainIngredient;
            recipe.MealType = request.MealType;
            recipe.Occasion = request.Occasion;
            recipe.RecipeType = request.RecipeType;
            recipe.Description = request.Description;
            recipe.Url = request.Url;

            await _context.SaveChangesAsync();
            _cache.Remove(RecipeCacheKey);
            return Ok(recipe);
        }

        // DELETE /api/recipe/{id} — ta bort recept
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe is null)
                return NotFound($"Recept med id {id} hittades inte.");

            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
            _cache.Remove(RecipeCacheKey);
            return NoContent();
        }
    }
}
