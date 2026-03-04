using Fontana.AI.Models;
using Fontana.AI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Fontana.AI.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ChatController(IChatService chatService)
        {
            _chatService = chatService;
        }

        // POST /api/chat/ask — max 20 anrop/minut per IP
        // Body: { "message": "...", "history": [ { "role": "user", "content": "..." }, ... ] }
        [HttpPost("ask")]
        [EnableRateLimiting("chat")]
        public async Task<IActionResult> Ask([FromBody] ChatRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var response = await _chatService.GetAiResponseAsync(request.Message, request.History);
            return Ok(new { answer = response });
        }
    }
}
