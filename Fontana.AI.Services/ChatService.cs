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
                // 1. Kontrollera API-nyckel
                if (string.IsNullOrEmpty(_apiKey) || _apiKey.StartsWith("sk-..."))
                {
                    return "Fel: API-nyckeln saknas eller är inte korrekt inlagd i appsettings.json.";
                }

                // 2. Hämta kunskap från databasen (SQL FAQ)
                var faqs = await _context.Faqs.ToListAsync();
                var faqContext = string.Join("\n", faqs.Select(f => $"Fråga: {f.Question} Svar: {f.Answer}"));

                // 3. Förberedelse för DABAS-data (Placeholder tills DABAS-klienten är klar)
                // Här kommer vi senare anropa DabasService.GetProductInfo(userMessage)
                string dabasProductInfo = "[Ingen specifik produktinfo hämtad ännu]";

                // 4. Initiera OpenAI-klienten (GPT-4o)
                ChatClient client = new(model: "gpt-4o", _apiKey);

                // 5. Definiera systemets personlighet och "Viktiga regler"
                string systemInstruction = $@"Du är Fontanas passionerade och hjälpsamma AI-assistent. 
                Du representerar ett familjeföretag med rötter i Grekland och Cypern. Svara varmt och välkomnande.

                Här är din kunskapsbas:
                ---
                ALLMÄN FAQ:
                {faqContext}

                PRODUKTFAKTA FRÅN DABAS:
                {dabasProductInfo}
                ---

                VIKTIGA REGLER FÖR DINA SVAR:
                1. NOGGRANNHET: Svara endast baserat på informationen ovan. Om du är osäker eller om information saknas, säg: 'Det var en bra fråga! För att du ska få ett helt korrekt svar ber jag dig kontakta oss på fontana@support.com'.
                2. INGA GISSNINGAR: Chansa aldrig om innehåll, allergener eller ursprung.
                3. MEDICINSKA RÅD: Gör aldrig medicinska påståenden. Du får citera näringsvärden men aldrig påstå att något botar sjukdomar.
                4. PRISER: Diskutera aldrig priser. Hänvisa kunden till deras lokala livsmedelsbutik.
                5. KONKURRENTER: Var alltid lojal mot Fontana. Prata aldrig illa om andra varumärken.";

                // 6. Skicka anropet till OpenAI
                ChatCompletion completion = await client.CompleteChatAsync(new ChatMessage[]
                {
                     new SystemChatMessage(systemInstruction),
                     new UserChatMessage(userMessage)
                });

                return completion.Content[0].Text;
            }
            catch (Exception ex)
            {
                // Returnerar felet så det syns i gränssnittet under utveckling
                return $"Ett fel uppstod i ChatService: {ex.Message}";
            }
        }
    }
}