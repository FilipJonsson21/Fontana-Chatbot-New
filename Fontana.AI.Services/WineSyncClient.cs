using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Fontana.AI.Models;
using Microsoft.Extensions.Logging;

namespace Fontana.AI.Services
{
    // Hämtar vin/sprit-produkter från fontana.se genom att läsa vin-sitemapen och
    // skrapa fälten (typ, producent, ursprung, Systembolagsnummer m.m.) ur produktsidans HTML.
    public partial class WineSyncClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WineSyncClient> _logger;

        private const string SitemapUrl = "https://www.fontana.se/vin_product-sitemap.xml";
        private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";

        public WineSyncClient(HttpClient httpClient, ILogger<WineSyncClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FontanaAiBot/1.0 (vinsynk; +https://www.fontana.se)");
        }

        public async Task<List<string>> GetWineUrlsAsync()
        {
            try
            {
                var xml = await _httpClient.GetStringAsync(SitemapUrl);
                var urls = XDocument.Parse(xml)
                    .Descendants(SitemapNs + "loc")
                    .Select(e => e.Value.Trim())
                    .Where(IsWinePageUrl)
                    .ToList();

                _logger.LogInformation("Hittade {Count} vin-/spritlänkar i sitemap", urls.Count);
                return urls;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kunde inte hämta vin-sitemap från {Url}", SitemapUrl);
                return [];
            }
        }

        internal static bool IsWinePageUrl(string url)
        {
            if (!url.Contains("/vin/", StringComparison.OrdinalIgnoreCase))
                return false;

            // Uteslut vinöversiktssidan själv (t.ex. https://www.fontana.se/vin/)
            return !url.TrimEnd('/').EndsWith("/vin", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<List<Wine>> GetAllWinesAsync(int maxConcurrency = 5)
        {
            var urls = await GetWineUrlsAsync();
            if (urls.Count == 0)
                return [];

            using var semaphore = new SemaphoreSlim(maxConcurrency);
            var wines = (await Task.WhenAll(urls.Select(async url =>
            {
                await semaphore.WaitAsync();
                try { return await GetWineAsync(url); }
                finally { semaphore.Release(); }
            })))
                .Where(w => w is not null)
                .Select(w => w!)
                .ToList();

            _logger.LogInformation("Vinsynk: {Ok} av {Total} sidor gav ett giltigt vin/sprit", wines.Count, urls.Count);
            return wines;
        }

        public async Task<Wine?> GetWineAsync(string url)
        {
            try
            {
                var html = await _httpClient.GetStringAsync(url);
                var name = MatchOne(NamePattern(), html);
                if (string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Ingen produktrubrik hittades på {Url}", url);
                    return null;
                }

                return new Wine
                {
                    Name = name,
                    Type = MatchLabelValue(html, "Typ"),
                    Producer = MatchLabelValue(html, "Producenter"),
                    Origin = MatchLabelValue(html, "Region"),
                    AlcoholPercent = MatchLabelValue(html, "Alkoholhalt"),
                    AssortmentType = MatchLabelValue(html, "Sortiment"),
                    SystembolagNumber = MatchOne(SystembolagPattern(), html),
                    Description = MatchOne(DescriptionPattern(), html),
                    Url = url
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kunde inte hämta/tolka vin/sprit från {Url}", url);
                return null;
            }
        }

        // Fontanas produktsidor lägger ut fält som en <p>-etikett följt av en elementor-rubrik med värdet
        internal static string? MatchLabelValue(string html, string label)
        {
            var pattern = "<p(?: class=\"p1\")?>" + Regex.Escape(label) + "</p>[\\s\\S]{0,400}?elementor-heading-title[^>]*>([^<]+)</";
            var match = Regex.Match(html, pattern);
            return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
        }

        internal static string? MatchOne(Regex regex, string html)
        {
            var match = regex.Match(html);
            return match.Success ? WebUtility.HtmlDecode(match.Groups[1].Value).Trim() : null;
        }

        [GeneratedRegex("<h1[^>]*elementor-heading-title[^>]*>([^<]+)</h1>")]
        internal static partial Regex NamePattern();

        [GeneratedRegex("BEST[ÄA]LLNINGSNUMMER SYSTEMBOLAGET</h2>[\\s\\S]{0,400}?elementor-widget-container\">\\s*([0-9]{4,10})\\s*</div>")]
        internal static partial Regex SystembolagPattern();

        [GeneratedRegex(">Karakt[äa]r</h2>[\\s\\S]{0,600}?<p>([^<]{10,600})</p>")]
        internal static partial Regex DescriptionPattern();
    }
}
