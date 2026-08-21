using System.Diagnostics;
using System.Text.RegularExpressions;
using Fontana.AI.Data;
using Fontana.AI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace Fontana.AI.Services
{
    public partial class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;
        private readonly ILogger<ChatService> _logger;
        private readonly IMemoryCache _cache;

        // Cachenycklarna för FAQ, produkt-, recept- och vindata
        private const string FaqCacheKey = "faqs";
        private const string ProductCacheKey = "dabas_products";
        private const string RecipeCacheKey = "recipes";
        private const string WineCacheKey = "wines";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        // Max antal historikmeddelanden som skickas till OpenAI per anrop (håller token-kostnaden i schack)
        private const int MaxHistoryMessages = 10;

        // Svenska stoppord som inte bidrar till produkt-/recept-/vinmatchning
        private static readonly HashSet<string> Stopwords = new()
        {
            "det", "den", "ett", "och", "för", "med", "har", "är", "inte", "som",
            "kan", "ska", "vad", "var", "hur", "alla", "era", "ert", "din", "dina",
            "sin", "sina", "men", "att", "sig", "där", "här", "från", "till", "inom",
            "utan", "även", "just", "lite", "mer", "på", "av", "en", "om", "du",
            "vi", "ni", "de", "ja", "ha", "nu", "gå", "se", "må", "åt",
            "ut", "in", "än"
        };

        [GeneratedRegex(@"\balkoholfri\w*\b", RegexOptions.IgnoreCase)]
        private static partial Regex AlcoholFreeRegex();

        [GeneratedRegex(@"\b(vin|sprit|öl|alkoholhaltig\w*|cider|whisky|whiskey|vodka|gin|cognac|likör|akvavit|champagne|prosecco|rosé|rosè|grappa|ouzo|portvin|tequila|brännvin)\b", RegexOptions.IgnoreCase)]
        private static partial Regex AlcoholRegex();

        public ChatService(ApplicationDbContext context, IConfiguration configuration, ILogger<ChatService> logger, IMemoryCache cache)
        {
            _context = context;
            _logger = logger;
            _cache = cache;
            // Hämtar nyckeln från appsettings.json
            _apiKey = configuration["OpenAI:ApiKey"] ?? "";
        }

        public async Task<ChatResponse> GetAiResponseAsync(string userMessage, IList<ConversationMessage>? history = null)
        {
            var historyLength = history?.Count ?? 0;
            _logger.LogInformation("ChatService anropas. Historiklängd: {HistoryLength}", historyLength);
            var stopwatch = Stopwatch.StartNew();
            try
            {
                // 1. Kontrollera API-nyckel
                if (!IsValidApiKey(_apiKey))
                {
                    _logger.LogError("OpenAI API-nyckel saknas eller är ogiltig");
                    return new ChatResponse("Fel: API-nyckeln saknas eller är inte korrekt inlagd i appsettings.json.", 0);
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

                // 3. Hämta produktdata från cache eller databas — exkludera alkohol (hanteras separat i steg 5)
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
                var nonAlcoholProducts = products.Where(p => !IsAlcoholProduct(p)).ToList();

                // Filtrera produkter baserat på nyckelord i frågan + senaste historik,
                // så att uppföljningsfrågor ("innehåller den gluten?") hittar rätt produkt
                var filterQuery = BuildFilterQuery(userMessage, history, maxHistoryTurns: 2);
                var relevantProducts = FilterRelevantProducts(nonAlcoholProducts, filterQuery, maxCount: 30);

                // Om frågan gäller allergener/ingredienser/glutenfritt och flera specifika produkter matchar,
                // ställ en följdfråga istället för att gissa — 2–5 träffar är tillräckligt specifikt för att fråga tillbaka
                var detailTriggers = new[] { "allergener", "ingredienser", "glutenfri", "glutenfritt", "innehåller" };
                bool isDetailQuestion = detailTriggers.Any(t => userMessage.ToLowerInvariant().Contains(t));
                if (isDetailQuestion && relevantProducts.Count is >= 2 and <= 5)
                {
                    var names = relevantProducts.Select(p => p.ProductName).ToList();
                    string lista = names.Count == 2
                        ? $"{names[0]} eller {names[1]}"
                        : string.Join(", ", names[..^1]) + $" eller {names[^1]}";
                    _logger.LogInformation("Klarifieringsfråga ställs — {Count} produkter matchade", names.Count);
                    var clarificationAnswer = $"Det finns flera produkter som matchar din fråga – vilken menar du? {lista}?";
                    var clarificationLogId = await SaveConversationLogAsync(userMessage, clarificationAnswer, null, stopwatch.ElapsedMilliseconds);
                    return new ChatResponse(clarificationAnswer, clarificationLogId);
                }

                string dabasProductInfo = relevantProducts.Any()
                    ? string.Join("\n", relevantProducts.Select(p =>
                        $"Produkt: {p.ProductName} | GTIN: {p.Gtin} | Ingredienser: {p.Ingredients} | Allergener: {p.Allergens} | Ursprung: {p.Origin} | Näring: {p.Nutrition}"))
                    : nonAlcoholProducts.Any()
                        ? "[Inga produkter matchade din fråga – försök med ett mer specifikt produktnamn]"
                        : "[Inga produkter synkade ännu – kör POST /api/dabas/sync för att hämta produkter]";

                // 4. Hämta receptdata från cache eller databas
                if (!_cache.TryGetValue(RecipeCacheKey, out List<Recipe>? recipes) || recipes is null)
                {
                    recipes = await _context.Recipes.ToListAsync();
                    _cache.Set(RecipeCacheKey, recipes, CacheDuration);
                    _logger.LogDebug("Recept laddade från databasen och cachades ({Count} st)", recipes.Count);
                }
                else
                {
                    _logger.LogDebug("Recept hämtades från cache ({Count} st)", recipes.Count);
                }
                var relevantRecipes = FilterRelevantRecipes(recipes, filterQuery, maxCount: 10);
                string recipeInfo = relevantRecipes.Any()
                    ? string.Join("\n", relevantRecipes.Select(r =>
                        $"Recept: {r.Title} | Huvudingrediens: {r.MainIngredient} | Måltid: {r.MealType} | Tillfälle: {r.Occasion} | Typ: {r.RecipeType} | Beskrivning: {r.Description} | Länk: {r.Url}"))
                    : recipes.Any()
                        ? "[Inga recept matchade din fråga]"
                        : "[Inga recept inlagda ännu]";

                // 5. Hämta vin-/spritdata från cache eller databas
                if (!_cache.TryGetValue(WineCacheKey, out List<Wine>? wines) || wines is null)
                {
                    wines = await _context.Wines.ToListAsync();
                    _cache.Set(WineCacheKey, wines, CacheDuration);
                    _logger.LogDebug("Vin/sprit laddade från databasen och cachades ({Count} st)", wines.Count);
                }
                else
                {
                    _logger.LogDebug("Vin/sprit hämtades från cache ({Count} st)", wines.Count);
                }
                var relevantWines = FilterRelevantWines(wines, filterQuery, maxCount: 10);
                string wineInfo = relevantWines.Any()
                    ? string.Join("\n", relevantWines.Select(w =>
                        $"Namn: {w.Name} | Typ: {w.Type} | Producent: {w.Producer} | Ursprung: {w.Origin} | Alkoholhalt: {w.AlcoholPercent} | Systembolagets sortimentstyp: {w.AssortmentType} | Systembolagsnummer: {w.SystembolagNumber} | Beskrivning: {w.Description} | Länk: {w.Url}"))
                    : wines.Any()
                        ? "[Inget vin/sprit matchade din fråga]"
                        : "[Inget vin/sprit inlagt ännu]";

                // 6. Initiera OpenAI-klienten (GPT-4o)
                ChatClient client = new(model: "gpt-4o", _apiKey);

                // 7. Definiera systemets personlighet och viktiga regler
                string systemInstruction =
$"""
Du är Frixos — Fontanas passionerade och hjälpsamma AI-assistent.
Du representerar ett familjeföretag med rötter i Grekland och Cypern. Svara varmt och välkomnande.

Här är din kunskapsbas:
---
ALLMÄN FAQ:
{faqContext}

PRODUKTFAKTA FRÅN DABAS:
{dabasProductInfo}

RECEPT:
{recipeInfo}

VIN & SPRIT (OBS: priser finns aldrig med här, se regel 17):
{wineInfo}

VIKTIGA LÄNKAR:
- Reklamation: https://www.fontana.se/reklamation/
- Kontakta oss (allmänt): https://www.fontana.se/kontakta-oss/
- Konsumentkontakt (FORMULÄR för produktfrågor, reklamationer/klagomål och feedback från privatpersoner — hänvisa hit istället för att ge ut en e-postadress, så kommer ärendet in fullständigt): https://www.fontana.se/konsumentkontakt/
- Lediga jobb: https://www.fontana.se/jobba-pa-fontana-lediga-tjanster/
- Om Fontana / vår historia: https://www.fontana.se/fontanas-resa/
- Alla produkter: https://www.fontana.se/produkt/
- Foodservice-sortiment (restaurang/storkök — produkter): https://www.fontana.se/foodservice-sortiment/
- Foodservice-kontakt (restaurang/storkök — kontakt/inköp): https://www.fontana.se/foodservice-kontakt/
- Grossist & leverantör: https://www.fontana.se/grossist-och-leverantor/
- Systembolaget, hela Fontanas sortiment (privatpersoner köper vin/sprit här, inte från Fontana): https://www.systembolaget.se/sortiment/?q=fontana+food
---

VIKTIGA REGLER FÖR DINA SVAR:
1. NOGGRANNHET: Svara ENDAST baserat på informationen i kunskapsbasen ovan. Använd ALDRIG ditt allmänna träningsdata eller egna antaganden om Fontanas produkter — inte ens om du "tror" att det stämmer. Om produktinformation saknas eller är markerad som ej tillgänglig, säg: 'Jag har tyvärr inte tillgång till den informationen just nu. Kontakta oss gärna på info@fontanafood.se så hjälper vi dig!'
2. INGA GISSNINGAR: Chansa aldrig om innehåll, allergener eller ursprung.
3. MEDICINSKA RÅD: Gör aldrig medicinska påståenden. Du får citera näringsvärden men aldrig påstå att något botar sjukdomar.
4. PRISER: Diskutera aldrig priser — varken på vanliga produkter eller vin/sprit (se även regel 17). Om frågan är generell och inte nämner en specifik vara ("vad kostar en flaska?", "vad kostar era produkter?") — svara kort och generellt utan att själv plocka fram och namnge en specifik produkt eller ett specifikt vin ur kunskapsbasen; nämn bara en vara vid namn om användaren själv gjorde det. Hänvisa privatpersoner till deras lokala livsmedelsbutik för Fontanas vanliga produkter, eller till Systembolagets sortimentslänk (se VIKTIGA LÄNKAR) om frågan gäller eller kan gälla vin/sprit. Hänvisa restaurang-/storköksinköpare istället till Foodservice-kontakt (se regel 16 och VIKTIGA LÄNKAR) — de handlar inte i vanlig butik.
5. KONKURRENTER: Var alltid lojal mot Fontana. Prata aldrig illa om andra varumärken.
6. SPECIFICERING: Om frågan gäller ingredienser, allergener eller ursprung och flera produkter matchar — svara kort och generellt om det som är gemensamt, nämn 2–3 produktnamn som exempel, och avsluta med att användaren kan specificera sig för mer detaljerad info om en viss produkt.
7. ALLERGEN-DISCLAIMER: Avsluta ALLTID svar som rör allergener, ingredienser eller glutenfritt med meningen: "Kontrollera alltid originalförpackningen för den senaste och mest exakta informationen."
8. SPRÅK: Svara alltid på samma språk som användaren skriver på. Om användaren skriver på engelska — svarar du på engelska. Om på grekiska — svarar du på grekiska. Svenska är standard.
9. ESKALERING: Om användaren klagar på produktkvalitet (t.ex. fel smak, konstiga föremål, dålig förpackning, mögel, misstänkt receptändring eller produktionsfel) eller efterfrågar en mänsklig kontakt — visa empati, eskalera omedelbart och hänvisa till Konsumentkontakt-formuläret (https://www.fontana.se/konsumentkontakt/, se VIKTIGA LÄNKAR), så att ärendet — inklusive ev. bilder/video, LOT-nummer, EAN-kod och andra detaljer kunden nämnt — kommer in fullständigt och strukturerat. Ge ALDRIG ut en e-postadress för den här typen av ärende (varken info@fontanafood.se eller någon annan) — hänvisa alltid till formuläret istället. Försök inte hantera klagomål om produktsäkerhet själv, och gissa aldrig om vi faktiskt har ändrat recept eller haft ett produktionsfel — det vet du inte, det är precis därför ärendet ska in via formuläret.
10. HALAL/KOSHER/VEGANSKT: Bekräfta aldrig att en produkt är halal, kosher, vegan eller liknande — även om det verkar stämma utifrån ingredienslistan. Hänvisa alltid till originalförpackningen och certifierande organ.
11. OFF-TOPIC: Om frågan inte handlar om Fontanas produkter, tjänster eller företag — avvisa artigt: "Jag kan bara hjälpa dig med frågor om Fontanas produkter och tjänster." Svara inte på frågor om politik, andra varumärken, matlagning med andras produkter eller andra ämnen som saknar koppling till Fontana.
12. SVARSLÄNGD: Håll svar korta och tydliga. Enkla frågor besvaras på max 3–4 meningar. Använd punktlista när du listar flera saker (t.ex. flera produkter eller flera ingredienser). Skriv aldrig onödigt långa svar.
13. LÄNKAR: Om användarens fråga matchar ett ämne i VIKTIGA LÄNKAR ovan (t.ex. reklamation, kontakt, jobb, foodservice) — hänvisa till rätt länk i ditt svar. Hitta aldrig på egna länkar eller URL:er som inte finns i listan. Skriv alltid ut länken som ren text, t.ex. "https://www.fontana.se/reklamation/" — använd ALDRIG Markdown-länkformat som [text](url), eftersom det inte renderas i chattfönstret.
14. RECEPT: Om användaren frågar vad de kan laga med en viss ingrediens/produkt, ber om receptförslag, eller frågar vad ett recept innehåller — använd ENDAST recepten i RECEPT ovan. Föreslå 1–3 recept som passar, nämn receptnamnet kort och avsluta alltid med receptets länk (som ren text, se regel 13). Hitta aldrig på egna recept, ingredienser eller instruktioner. Om RECEPT-listan är tom eller inget recept passar frågan — säg det ärligt istället för att gissa, t.ex. "Jag har tyvärr inget recept med det just nu, men kolla gärna in vårt receptbibliotek på fontana.se." Koppla gärna produkter och recept ihop: nämner du en produkt kan du tipsa om ett recept som använder den, och tvärtom.
15. AI-TRANSPARENS: Om användaren frågar om du är en AI, en bot, eller pratar med en människa — svara alltid ärligt och tydligt att du är Frixos, en AI-assistent som drivs av Fontana. Dölj det aldrig och låtsas aldrig vara en människa.
16. PRIVAT VS. FOODSERVICE: Anta privatperson/konsument som standard. Om meddelandet (nu eller tidigare i konversationen) tyder på att användaren är inköpare för restaurang, café, storkök eller liknande verksamhet (t.ex. nämner "restaurang", "kök", "storhushåll", "inköp till verksamheten", "grossist", "leverantör") — byt spår: använd Foodservice-sortiment/Foodservice-kontakt istället för Konsumentkontakt, och anta att frågor om inköp/beställning/kvantitet gäller verksamhet, inte enstaka förpackningar i butik. Är det oklart och verkligen relevant (t.ex. frågan gäller inköp eller kontaktväg) — fråga kort: "Handlar det här om ditt eget hushåll, eller köper du in till en restaurang/verksamhet?" innan du svarar. Gissa inte i onödan — de flesta produkt- och receptfrågor är samma oavsett kundtyp och kräver ingen sådan fråga.
17. VIN & SPRIT (ALKOHOL): Diskutera ENDAST vin/sprit om användaren själv tar upp det — föreslå det aldrig oombett till recept eller produkter. Använd ENDAST informationen i VIN & SPRIT ovan, hitta aldrig på viner/sprit som inte finns där. Nämn ALDRIG pris — inte ens om användaren frågar (det finns heller aldrig med i kunskapsbasen). Fontana säljer inte alkohol direkt till privatpersoner (olagligt i Sverige utanför Systembolaget). Skilj på två fall:
    a) Vinet/spriten har ett Systembolagsnummer i kunskapsbasen ovan — bekräfta gärna kort och varmt att vi har varan (särskilt om kunden verkar entusiastisk över just den), och ANGE ALLTID numret så de kan söka upp den direkt hos Systembolaget (https://www.systembolaget.se/sortiment/?q=fontana+food, se VIKTIGA LÄNKAR — visar hela Fontanas sortiment hos Systembolaget) — hoppa aldrig över numret till förmån för ett generellt svar. Om "Systembolagets sortimentstyp" finns angiven i kunskapsbasen, förklara vad det innebär för hur kunden ska gå tillväga: "Fast sortiment" betyder att varan normalt finns på hyllan i Systembolagets fysiska butiker; "Beställningssortiment" (en s.k. ordervara) betyder att den INTE ligger framme i butik utan måste beställas i förväg — antingen på systembolaget.se för hemleverans/upphämtning, eller genom att be personalen i en fysisk butik beställa hem den, vanligtvis med några dagars leveranstid; "Tillfälligt sortiment" betyder en säsongs-/tillfällig vara som kan ta slut permanent. Om sortimentstypen saknas i kunskapsbasen — gissa aldrig vilken det är, säg bara att du inte vet och hänvisa till Systembolagets sida/numret för att kolla. Om kunden säger att varan är tillfälligt slut eller inte går att hitta hos Systembolaget just nu — gissa aldrig på varför eller när den kommer tillbaka (Systembolagets lagerstatus och restocking känner du inte till, oavsett sortimentstyp). Var ärlig med det, och hänvisa henne att antingen kontakta Systembolaget direkt (med numret) för aktuell status, eller höra av sig via Konsumentkontakt-formuläret om hon har frågor om varan från vår sida. Gå INTE direkt till privatimport-förklaringen i fall b nedan bara för att varan råkar vara slut just nu — privatimport är till för viner vi inte har i det vanliga Systembolags-sortimentet, inte för tillfälligt lågt lager eller en ordervara som redan finns där.
    b) Vinet/spriten säljs bara till restauranger/foodservice, eller finns inte alls i kunskapsbasen ovan (t.ex. ett specifikt vin en privatperson frågar om som vi inte känner igen) — förklara istället att privatpersoner kan kontakta Systembolagets avdelning för speciella viner/sprit och begära en s.k. "privatimport" av det specifika vinet/spriten. Systembolaget skickar då en förfrågan vidare till Fontana, och Fontana lämnar i så fall ett pris till Systembolaget — aldrig till privatpersonen direkt. Var tydlig med att hon INTE kan köpa varan via en restaurang istället, och gissa aldrig på om vi faktiskt har varan i sortimentet om det inte står i VIN & SPRIT ovan — hänvisa henne att fråga oss direkt om exakt den varan via Kontakta oss eller Konsumentkontakt om hon vill veta om vi importerar den. Rekommendera ALDRIG en tredjeparts vinimportör — Fontana är själva importören.
    Foodservice-kunder (restaurang/bar/storkök) hänvisas till Foodservice-kontakt istället i båda fallen. Gör aldrig hälsopåståenden om alkohol och uppmuntra aldrig till hög konsumtion — håll tonen sakligt informativ (ursprung, karaktär, vad den passar till), aldrig säljande eller uppmanande att dricka mer.
18. UTGÅNGNA PRODUKTER & BUTIKSTILLGÄNGLIGHET: Om användaren frågar varför en specifik produkt verkar ha utgått, blivit slutsåld eller plockats bort från en butik/kedja, om den kommer tillbaka, eller vilken specifik butik (eller vilken annan butik i närheten) som säljer/sålde en viss produkt — gissa ALDRIG. Kunskapsbasen har ingen realtidsdata om lagerstatus, sortimentsbeslut eller vilka butiker som för tillfället har en viss vara. Visa empati för att kunden saknar produkten, men säg ärligt att du inte har den informationen och hänvisa till Konsumentkontakt-formuläret (https://www.fontana.se/konsumentkontakt/, se VIKTIGA LÄNKAR) — inte en e-postadress — så att de kan titta på det specifika fallet (uppmana kunden att fylla i produktnamn och ort i formuläret om möjligt).
19. ALKOHOLFRIA DRYCKER: Om användaren specifikt frågar om alkoholfri öl, alkoholfritt vin eller andra alkoholfria dryckesalternativ inom samma kategori som VIN & SPRIT (inte mat/juice i allmänhet) — sök ENDAST i VIN & SPRIT-listan ovan efter alkoholfria alternativ och rekommendera dem om de finns där. Hitta ALDRIG på ett alkoholfritt alternativ som inte står med, och ersätt det INTE i onödan med orelaterade produkter (t.ex. fruktjuice eller nektar) bara för att de råkar vara alkoholfria — kunden frågade specifikt om öl/vin, inte vilken alkoholfri dryck som helst. Om inget alkoholfritt alternativ finns i VIN & SPRIT — säg det ärligt och hänvisa till Systembolaget (https://www.systembolaget.se/sortiment/?q=fontana+food, se VIKTIGA LÄNKAR) som en bra plats att leta vidare, precis som för alkoholhaltiga drycker (regel 17).
""";

                // 8. Bygg meddelandelistan — system + eventuell historik + aktuellt meddelande
                var messages = new List<ChatMessage>
                {
                    new SystemChatMessage(systemInstruction)
                };

                // Lägg till konversationshistorik — begränsad till de senaste MaxHistoryMessages
                // för att hålla token-kostnaden konstant oavsett hur lång chatten är
                if (history is { Count: > 0 })
                {
                    foreach (var entry in history.TakeLast(MaxHistoryMessages))
                    {
                        if (entry.Role == "user")
                            messages.Add(new UserChatMessage(entry.Content));
                        else if (entry.Role == "assistant")
                            messages.Add(new AssistantChatMessage(entry.Content));
                    }
                }

                // Lägg till det aktuella meddelandet sist
                messages.Add(new UserChatMessage(userMessage));

                // 9. Skicka anropet till OpenAI
                _logger.LogInformation("Skickar {MessageCount} meddelanden till OpenAI GPT-4o", messages.Count);
                ChatCompletion completion = await client.CompleteChatAsync(messages);
                stopwatch.Stop();
                var tokens = completion.Usage?.TotalTokenCount;
                _logger.LogInformation("Svar mottaget från OpenAI. Tokens: {Tokens}, Tid: {Ms}ms", tokens ?? 0, stopwatch.ElapsedMilliseconds);

                var answer = completion.Content[0].Text;
                var logId = await SaveConversationLogAsync(userMessage, answer, tokens, stopwatch.ElapsedMilliseconds);
                return new ChatResponse(answer, logId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid anrop till OpenAI");
                return new ChatResponse($"Ett fel uppstod i ChatService: {ex.Message}", 0);
            }
        }

        private async Task<int> SaveConversationLogAsync(string userMessage, string botResponse, int? tokensUsed, long responseTimeMs)
        {
            try
            {
                var log = new ConversationLog
                {
                    UserMessage = userMessage,
                    BotResponse = botResponse,
                    Timestamp = DateTime.UtcNow,
                    TokensUsed = tokensUsed,
                    ResponseTimeMs = responseTimeMs
                };
                _context.ConversationLogs.Add(log);
                await _context.SaveChangesAsync();
                return log.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kunde inte spara konversationslogg");
                return 0;
            }
        }

        internal static bool IsValidApiKey(string? apiKey) =>
            !string.IsNullOrEmpty(apiKey) && apiKey.StartsWith("sk-");

        // Kombinerar aktuell fråga med de senaste N användarturerna ur historiken.
        // Gör att uppföljningsfrågor som "innehåller den gluten?" kan hitta rätt produkt
        // tack vare att t.ex. "olivolja" finns kvar från föregående tur.
        internal static string BuildFilterQuery(string userMessage, IList<ConversationMessage>? history, int maxHistoryTurns)
        {
            if (history is null or { Count: 0 })
                return userMessage;

            var recentUserMessages = history
                .Where(m => m.Role == "user")
                .TakeLast(maxHistoryTurns)
                .Select(m => m.Content);

            return string.Join(" ", recentUserMessages.Append(userMessage));
        }

        // Filtrerar produkter baserat på nyckelord i frågan.
        internal static List<DabasProduct> FilterRelevantProducts(List<DabasProduct> products, string query, int maxCount)
        {
            if (!products.Any()) return [];

            var keywords = ExtractKeywords(query);
            if (keywords.Length == 0) return [];

            return products
                .Select(p =>
                {
                    var searchText = $"{p.ProductName} {p.Ingredients} {p.Allergens} {p.Origin}".ToLowerInvariant();
                    var searchWords = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    int score = keywords.Count(kw =>
                        searchText.Contains(kw) ||
                        searchWords.Any(sw => sw.Length > 2 && kw.StartsWith(sw)));
                    return (Product: p, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(maxCount)
                .Select(x => x.Product)
                .ToList();
        }

        // Filtrerar recept baserat på nyckelord i frågan (samma poängsättning som produkter).
        internal static List<Recipe> FilterRelevantRecipes(List<Recipe> recipes, string query, int maxCount)
        {
            if (!recipes.Any()) return [];

            var keywords = ExtractKeywords(query);
            if (keywords.Length == 0) return [];

            return recipes
                .Select(r =>
                {
                    var searchText = $"{r.Title} {r.MainIngredient} {r.MealType} {r.Occasion} {r.RecipeType} {r.Description}".ToLowerInvariant();
                    var searchWords = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    int score = keywords.Count(kw =>
                        searchText.Contains(kw) ||
                        searchWords.Any(sw => sw.Length > 2 && kw.StartsWith(sw)));
                    return (Recipe: r, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(maxCount)
                .Select(x => x.Recipe)
                .ToList();
        }

        // Filtrerar vin/sprit baserat på nyckelord i frågan (samma poängsättning som produkter).
        internal static List<Wine> FilterRelevantWines(List<Wine> wines, string query, int maxCount)
        {
            if (!wines.Any()) return [];

            var keywords = ExtractKeywords(query);
            if (keywords.Length == 0) return [];

            return wines
                .Select(w =>
                {
                    var searchText = $"{w.Name} {w.Type} {w.Producer} {w.Origin} {w.Description}".ToLowerInvariant();
                    var searchWords = searchText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    int score = keywords.Count(kw =>
                        searchText.Contains(kw) ||
                        searchWords.Any(sw => sw.Length > 2 && kw.StartsWith(sw)));
                    return (Wine: w, Score: score);
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .Take(maxCount)
                .Select(x => x.Wine)
                .ToList();
        }

        // Delar upp en fråga i sökbara nyckelord — filtrerar bort korta ord och stoppord
        internal static string[] ExtractKeywords(string query) =>
            query
                .ToLowerInvariant()
                .Split([' ', ',', '.', '?', '!', '-'], StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length >= 2 && !Stopwords.Contains(w))
                .ToArray();

        // Avgör om en DABAS-produkt är en alkoholhaltig dryck (för att exkludera den ur den vanliga
        // produktkunskapsbasen — alkohol hanteras separat via VIN & SPRIT, se regel 17/19).
        internal static bool IsAlcoholProduct(DabasProduct product)
        {
            var text = $"{product.Category} {product.ProductName}";
            if (AlcoholFreeRegex().IsMatch(text))
                return false;
            return AlcoholRegex().IsMatch(text);
        }
    }
}
