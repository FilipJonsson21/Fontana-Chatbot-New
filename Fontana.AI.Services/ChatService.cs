using Fontana.AI.Data;
using Fontana.AI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;

namespace Fontana.AI.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;

        public ChatService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            // Hämtar nyckeln från appsettings.json
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
        }

        public async Task<string> GetAiResponseAsync(string userMessage)
        {
            try
            {
                // 1. Kontrollera nyckel
                if (string.IsNullOrEmpty(_apiKey) || _apiKey.StartsWith("DIN-"))
                {
                    return "Fel: API-nyckeln saknas eller är felaktig i appsettings.json.";
                }

                // 2. Hämta kunskap från databasen
                var faqs = await _context.Faqs.ToListAsync();
                var faqContext = string.Join("\n", faqs.Select(f => $"Fråga: {f.Question} Svar: {f.Answer}"));

                // 3. Initiera klienten
                ChatClient client = new(model: "gpt-4o", _apiKey);

                // 4. Skicka anropet
                ChatCompletion completion = await client.CompleteChatAsync(new ChatMessage[]
                {
            new SystemChatMessage($"Du är en kundtjänst-bot för Fontana. Svara baserat på detta: {faqContext}"),
            new UserChatMessage(userMessage)
                });

                return completion.Content[0].Text;
            }
            catch (Exception ex)
            {
                // Detta gör att vi ser felet i Scalar istället för att programmet dör!
                return $"Ett fel uppstod: {ex.Message}";
            }
        }
    }
}