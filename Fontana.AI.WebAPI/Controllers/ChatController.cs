using Microsoft.AspNetCore.Mvc;
using Fontana.AI.Services;

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

        [HttpPost("ask")]
        public async Task<IActionResult> Ask([FromBody] string userQuestion)
        {
            if (string.IsNullOrEmpty(userQuestion))
                return BadRequest("Frågan får inte vara tom.");

            var response = await _chatService.GetAiResponseAsync(userQuestion);
            return Ok(new { answer = response });
        }
    }
}