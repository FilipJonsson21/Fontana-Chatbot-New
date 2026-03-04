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
        private readonly DabasClient _dabasClient;
        private readonly string _apiKey;

        public ChatService(ApplicationDbContext context, IConfiguration configuration, DabasClient dabasClient)
        {
            _context = context;
            _dabasClient = dabasClient;
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
        }

        public async Task<string> GetAiResponseAsync(string userMessage)
        {
            try
            {
                // 1. Kontrollera API-nyckel
                if (string.IsNullOrEmpty(_apiKey) || _apiKey.StartsWith("sk-..."))
                    return "Fel: API-nyckeln saknas eller är inte korrekt inlagd i appsettings.json.";

                // 2. Hämta FAQ från databasen
                var faqs = await _context.Faqs.ToListAsync();
                var faqContext = faqs.Any()
                    ? string.Join("\n", faqs.Select(f => $"Fråga: {f.Question}\nSvar: {f.Answer}"))
                    : "Inga FAQ-poster finns ännu.";

                // 3. Hämta alla Fontanas produkter från DABAS
                var products = await _dabasClient.GetAllProductsBySupplierGlnAsync();
                var dabasProductInfo = products.Any()
                    ? string.Join("\n\n", products.Select(p =>
                        $"Produkt: {p.ProductName}\n" +
                        $"GTIN: {p.Gtin}\n" +
                        $"Ingredienser: {p.Ingredients}\n" +
                        $"Allergener: {p.Allergens}\n" +
                        $"Ursprung: {p.Origin}\n" +
                        $"Näringsvärden: {p.Nutrition}"))
                    : "Ingen produktdata tillgänglig från DABAS just nu.";

                // 4. Initiera OpenAI-klienten (GPT-4o)
                ChatClient client = new(model: "gpt-4o", _apiKey);

                // 5. Definiera systemets personlighet och regler
                string systemInstruction = $@"Du är Fontanas passionerade och hjälpsamma AI-assistent.
Du representerar ett familjeföretag med rötter i Grekland och Cypern. Svara varmt och välkomnande på svenska.

Här är din kunskapsbas:
---
ALLMÄN FAQ:
{faqContext}

PRODUKTFAKTA FRÅN DABAS:
{dabasProductInfo}
---

VIKTIGA REGLER FÖR DINA SVAR:
1. NOGGRANNHET: Svara baserat på informationen ovan. Om du är osäker, säg: 'Det var en bra fråga! För ett helt korrekt svar ber jag dig kontakta oss på fontana@support.com'.
2. INGA GISSNINGAR: Chansa aldrig om innehåll, allergener eller ursprung.
3. MEDICINSKA RÅD: Gör aldrig medicinska påståenden. Du får citera näringsvärden men påstå aldrig att något botar sjukdomar.
4. PRISER: Diskutera aldrig priser. Hänvisa kunden till sin lokala livsmedelsbutik.
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
                return $"Ett fel uppstod i ChatService: {ex.Message}";
            }
        }
    }
}
