using Fontana.AI.Data;
using Fontana.AI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Fontana.AI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FaqController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private const string FaqCacheKey = "faqs";

        public FaqController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        // GET /api/faq — hämta alla FAQ-poster
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var faqs = await _context.Faqs.OrderBy(f => f.Category).ThenBy(f => f.Id).ToListAsync();
            return Ok(faqs);
        }

        // GET /api/faq/{id} — hämta en specifik FAQ-post
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq is null)
                return NotFound($"FAQ med id {id} hittades inte.");

            return Ok(faq);
        }

        // POST /api/faq — skapa ny FAQ-post
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] FaqRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var faq = new FaqItem
            {
                Question = request.Question,
                Answer = request.Answer,
                Category = request.Category
            };

            _context.Faqs.Add(faq);
            await _context.SaveChangesAsync();
            _cache.Remove(FaqCacheKey);

            return CreatedAtAction(nameof(GetById), new { id = faq.Id }, faq);
        }

        // PUT /api/faq/{id} — uppdatera befintlig FAQ-post
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] FaqRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var faq = await _context.Faqs.FindAsync(id);
            if (faq is null)
                return NotFound($"FAQ med id {id} hittades inte.");

            faq.Question = request.Question;
            faq.Answer = request.Answer;
            faq.Category = request.Category;

            await _context.SaveChangesAsync();
            _cache.Remove(FaqCacheKey);
            return Ok(faq);
        }

        // DELETE /api/faq/{id} — ta bort FAQ-post
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var faq = await _context.Faqs.FindAsync(id);
            if (faq is null)
                return NotFound($"FAQ med id {id} hittades inte.");

            _context.Faqs.Remove(faq);
            await _context.SaveChangesAsync();
            _cache.Remove(FaqCacheKey);
            return NoContent();
        }
    }
}
