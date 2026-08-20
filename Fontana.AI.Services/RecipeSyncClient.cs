using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Fontana.AI.Models;
using Microsoft.Extensions.Logging;

namespace Fontana.AI.Services
{
    // Hämtar recept från fontana.se genom att läsa receptsitemapen och tolka
    // Recipe-schema.org JSON-LD som finns inbäddad på varje receptsida.
    public partial class RecipeSyncClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RecipeSyncClient> _logger;

        private const string SitemapUrl = "https://www.fontana.se/recipe-sitemap.xml";
        private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

        // Kategorisuffix som Fontana lägger till på receptsidor men som inte hör hemma i huvudingrediensen
        private static readonly string[] CategorySuffixesToStrip =
            [" inspiration", " klassiker", " klassiska", " klassiskt"];

        public RecipeSyncClient(HttpClient httpClient, ILogger<RecipeSyncClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FontanaAiBot/1.0 (receptsynk; +https://www.fontana.se)");
        }

        // Läser receptsitemapen och returnerar alla URL:er som ser ut som receptsidor
        public async Task<List<string>> GetRecipeUrlsAsync()
        {
            try
            {
                var xml = await _httpClient.GetStringAsync(SitemapUrl);
                var urls = XDocument.Parse(xml)
                    .Descendants(SitemapNs + "loc")
                    .Select(e => e.Value.Trim())
                    .Where(IsRecipePageUrl)
                    .ToList();

                _logger.LogInformation("Hittade {Count} receptlänkar i sitemap", urls.Count);
                return urls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kunde inte hämta receptsitemap från {Url}", SitemapUrl);
                return [];
            }
        }

        internal static bool IsRecipePageUrl(string url)
        {
            if (!url.Contains("/recept/", StringComparison.OrdinalIgnoreCase))
                return false;

            // Uteslut receptöversiktssidan själv (t.ex. https://www.fontana.se/recept/)
            return !url.TrimEnd('/').EndsWith("/recept", StringComparison.OrdinalIgnoreCase);
        }

        // Hämtar och tolkar alla receptsidor parallellt (begränsat antal samtidiga anrop)
        public async Task<List<Recipe>> GetAllRecipesAsync(int maxConcurrency = 5)
        {
            var urls = await GetRecipeUrlsAsync();
            if (urls.Count == 0)
                return [];

            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var recipes = (await Task.WhenAll(urls.Select(async url =>
            {
                await semaphore.WaitAsync();
                try { return await GetRecipeAsync(url); }
                finally { semaphore.Release(); }
            })))
                .Where(r => r is not null)
                .Select(r => r!)
                .ToList();

            _logger.LogInformation("Receptsynk: {Ok} av {Total} sidor gav ett giltigt recept", recipes.Count, urls.Count);
            return recipes;
        }

        public async Task<Recipe?> GetRecipeAsync(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var json = ExtractRecipeJsonLd(html);
                if (!json.HasValue)
                {
                    _logger.LogWarning("Ingen Recipe-JSON-LD hittades på {Url}", url);
                    return null;
                }

                return MapJsonToRecipe(json.Value, url);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kunde inte hämta/tolka recept från {Url}", url);
                return null;
            }
        }

        [GeneratedRegex("<script[^>]*type=[\"']application/ld\\+json[\"'][^>]*>(.*?)</script>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
        private static partial Regex JsonLdScriptRegex();

        // Letar igenom alla JSON-LD-block på sidan efter ett Recipe-objekt (även nästlat i @graph)
        internal static JsonElement? ExtractRecipeJsonLd(string html)
        {
            foreach (Match match in JsonLdScriptRegex().Matches(html))
            {
                JsonDocument doc;
                try
                {
                    doc = JsonDocument.Parse(match.Groups[1].Value);
                }
                catch (JsonException)
                {
                    continue;
                }

                using (doc)
                {
                    if (TryGetRecipeElement(doc.RootElement, out var recipeElement))
                        return recipeElement.Clone();
                }
            }
            return null;
        }

        private static bool TryGetRecipeElement(JsonElement element, out JsonElement recipeElement)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("@type", out var type) && IsRecipeType(type))
                {
                    recipeElement = element;
                    return true;
                }
                if (element.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in graph.EnumerateArray())
                    {
                        if (TryGetRecipeElement(item, out recipeElement))
                            return true;
                    }
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (TryGetRecipeElement(item, out recipeElement))
                        return true;
                }
            }

            recipeElement = default;
            return false;
        }

        private static bool IsRecipeType(JsonElement typeProp) => typeProp.ValueKind switch
        {
            JsonValueKind.String => typeProp.GetString() == "Recipe",
            JsonValueKind.Array => typeProp.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && t.GetString() == "Recipe"),
            _ => false
        };

        internal static Recipe MapJsonToRecipe(JsonElement json, string url)
        {
            var title = GetString(json, "name") ?? "(Namnlöst recept)";
            var category = GetString(json, "recipeCategory");

            return new Recipe
            {
                Title = title,
                MainIngredient = DeriveMainIngredient(category, title),
                RecipeType = category,
                Description = Truncate(GetString(json, "description"), 500),
                Url = url
            };
        }

        // Härleder en huvudingrediens ur receptkategorin (t.ex. "Halloumi inspiration" → "Halloumi").
        // Faller tillbaka på titeln om ingen kategori finns.
        internal static string DeriveMainIngredient(string? category, string title)
        {
            if (string.IsNullOrWhiteSpace(category))
                return title;

            var trimmed = category.Trim();
            foreach (var suffix in CategorySuffixesToStrip)
            {
                if (trimmed.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                    return trimmed[..^suffix.Length].Trim();
            }
            return trimmed;
        }

        private static string? GetString(JsonElement json, string property)
        {
            if (!json.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.String)
                return null;
            return value.GetString();
        }

        private static string? Truncate(string? text, int maxLength)
        {
            if (!string.IsNullOrEmpty(text) && text.Length > maxLength)
                return text[..maxLength] + "…";
            return text;
        }
    }
}
