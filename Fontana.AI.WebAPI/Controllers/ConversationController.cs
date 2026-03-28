using Fontana.AI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fontana.AI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ConversationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ConversationController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET /api/conversation?page=1&pageSize=50 — hämta loggade konversationer
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            page = Math.Max(1, page);

            var total = await _context.ConversationLogs.CountAsync();
            var logs = await _context.ConversationLogs
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.UserMessage,
                    l.BotResponse,
                    l.Timestamp,
                    l.TokensUsed,
                    l.ResponseTimeMs,
                    l.Feedback
                })
                .ToListAsync();

            return Ok(new { total, page, pageSize, logs });
        }

        // GET /api/conversation/stats — aggregerad statistik
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var total = await _context.ConversationLogs.CountAsync();
            var thumbsUp = await _context.ConversationLogs.CountAsync(l => l.Feedback == true);
            var thumbsDown = await _context.ConversationLogs.CountAsync(l => l.Feedback == false);
            var avgTokens = await _context.ConversationLogs
                .Where(l => l.TokensUsed.HasValue)
                .AverageAsync(l => (double?)l.TokensUsed) ?? 0;
            var avgResponseMs = await _context.ConversationLogs
                .Where(l => l.ResponseTimeMs.HasValue)
                .AverageAsync(l => (double?)l.ResponseTimeMs) ?? 0;

            return Ok(new
            {
                total,
                thumbsUp,
                thumbsDown,
                avgTokens = Math.Round(avgTokens),
                avgResponseMs = Math.Round(avgResponseMs)
            });
        }

        // POST /api/conversation/{id}/feedback — spara tumme upp/ner
        [HttpPost("{id:int}/feedback")]
        public async Task<IActionResult> SubmitFeedback(int id, [FromBody] FeedbackRequest request)
        {
            var log = await _context.ConversationLogs.FindAsync(id);
            if (log is null)
                return NotFound();

            log.Feedback = request.Positive;
            await _context.SaveChangesAsync();
            return Ok();
        }
    }

    public record FeedbackRequest(bool Positive);
}
