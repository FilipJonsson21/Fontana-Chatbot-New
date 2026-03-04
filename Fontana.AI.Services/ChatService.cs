using Fontana.AI.Data;
using Fontana.AI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Fontana.AI.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;
        private readonly ILogger<ChatService> _logger;
        private readonly IMemoryCache _cache;

        // Cachenycklarna för FAQ och produktdata
        private const string FaqCacheKey = "faqs";
        private const string ProductCacheKey = "dabas_products";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public ChatService(ApplicationDbContext context, IConfiguration configuration, ILogger<ChatService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            // Hämtar nyckeln från appsettings.json
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
        }

        public async Task<string> GetAiResponseAsync(string userMessage, IList<ConversationMessage>? history = null)
        {
            var historyLength = history?.Count ?? 0;
            _logger.LogInformation("ChatService anropas. Historiklängd: {HistoryLength}", historyLength);
            try
            {
                // 1. Kontrollera API-nyckel
                if (string.IsNullOrEmpty(_apiKey) || _apiKey.StartsWith("sk-..."))
                {
                    _logger.LogError("OpenAI API-nyckel saknas eller är ogiltig");
                    return "Fel: API-nyckeln saknas eller är inte korrekt inlagd i appsettings.json.";
                }

                // 2. Hämta FAQ från cache eller databas
                if (!_cache.TryGetValue(FaqCacheKey, out List<FaqItem>? faqs) || faqs is null)
                {
                    faqs = await _context.Faqs.ToListAsync();
                    _cache.Set(FaqCacheKey, faqs, CacheDuration);
                    _logger.LogDebug("FAQ-poster laddade från databasen och cachades ({Count} st)", faqs.Count);
                }
                else
                {
                    _logger.LogDebug("FAQ-poster hämtades från cache ({Count} st)", faqs.Count);
                }
                var faqContext = string.Join("\n", faqs.Select(f => $"Fråga: {f.Question} Svar: {f.Answer}"));

                // 3. Hämta produktdata från cache eller databas
                if (!_cache.TryGetValue(ProductCacheKey, out List<DabasProduct>? products) || products is null)
                {
                    products = await _context.DabasProducts.ToListAsync();
                    _cache.Set(ProductCacheKey, products, CacheDuration);
                    _logger.LogDebug("Produkter laddade från databasen och cachades ({Count} st)", products.Count);
                }
                else
                {
                    _logger.LogDebug("Produkter hämtades från cache ({Count} st)", products.Count);
                }
                // Filtrera produkter baserat på nyckelord i användarens fråga (max 30 st för att hålla nere tokens)
                var relevantProducts = FilterRelevantProducts(products, userMessage, maxCount: 30);
                string dabasProductInfo = relevantProducts.Any()
                    ? string.Join("\n", relevantProducts.Select(p =>
                        $"Produkt: {p.ProductName} | GTIN: {p.Gtin} | Ingredienser: {p.Ingredients} | Allergener: {p.Allergens} | Ursprung: {p.Origin} | Näring: {p.Nutrition}"))
                    : products.Any()
                        ? "[Inga produkter matchade din fråga – försök med ett mer specifikt produktnamn]"
                        : "[Inga produkter synkade ännu – kör POST /api/dabas/sync för att hämta produkter]";

                // 4. Initiera OpenAI-klienten (GPT-4o)
                ChatClient client = new(model: "gpt-4o", _apiKey);

                // 5. Definiera systemets personlighet och viktiga regler
                string systemInstruction =
$"""
Du är Fontanas passionerade och hjälpsamma AI-assistent.
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
5. KONKURRENTER: Var alltid lojal mot Fontana. Prata aldrig illa om andra varumärken.
""";

                // 6. Bygg meddelandelistan — system + eventuell historik + aktuellt meddelande
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemInstruction)
                };

                // Lägg till konversationshistorik om den finns
                if (history is { Count: > 0 })
                {
                    foreach (var entry in history)
                    {
                        if (entry.Role == "user")
                            messages.Add(new UserChatMessage(entry.Content));
                        else if (entry.Role == "assistant")
                            messages.Add(new AssistantChatMessage(entry.Content));
                    }
                }

                // Lägg till det aktuella meddelandet sist
                messages.Add(new UserChatMessage(userMessage));

                // 7. Skicka anropet till OpenAI
                _logger.LogInformation("Skickar {MessageCount} meddelanden till OpenAI GPT-4o", messages.Count);
                ChatCompletion completion = await client.CompleteChatAsync(messages);
                _logger.LogInformation("Svar mottaget från OpenAI. Tokens: {Tokens}", completion.Usage?.TotalTokenCount ?? 0);

                return completion.Content[0].Text;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid anrop till OpenAI");
                // Returnerar felet så det syns i gränssnittet under utveckling
                return $"Ett fel uppstod i ChatService: {ex.Message}";
            }
        }
        // Filtrerar produkter baserat på nyckelord i frågan — returnerar de mest relevanta
        private static List<DabasProduct> FilterRelevantProducts(List<DabasProduct> products, string query, int maxCount)
        {
            if (!products.Any()) return [];

            // Dela upp frågan i ord och filtrera bort korta stoppord
            var keywords = query
                .ToLowerInvariant()
                .Split([' ', ',', '.', '?', '!', '-'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 2)
                .ToArray();

            if (keywords.Length == 0)
                return products.Take(maxCount).ToList();

            // Poängsätt varje produkt baserat på hur många nyckelord som matchar.
            // Kontrollerar även omvänt (om ett produktord ingår i sökordet) för att hantera
            // svenska sammansatta ord, t.ex. "olivoljeprodukter" matchar produkten "olivolja".
            var scored = products
                .Select(p =>
                {
                    var searchText = $"{p.ProductName} {p.Ingredients} {p.Allergens} {p.Origin}".ToLowerInvariant();
                    var searchWords = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    int score = keywords.Count(kw =>
                        searchText.Contains(kw) ||
                        searchWords.Any(sw => sw.Length > 3 && kw.StartsWith(sw)));
                    return (Product: p, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(maxCount)
                .Select(x => x.Product)
                .ToList();

            // Om ingen matchning — returnera de första produkterna som fallback
            return scored.Any() ? scored : products.Take(maxCount).ToList();
        }
    }
}
