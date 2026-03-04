using System.Net.Http.Json;
using Fontana.AI.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Fontana.AI.Services
{
    public class DabasClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<DabasClient> _logger;

        public DabasClient(HttpClient httpClient, IConfiguration configuration, ILogger<DabasClient> logger)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Dabas:ApiKey"] ?? "";
            _logger = logger;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FontanaAiBot");
        }

        public async Task<DabasProduct?> GetProductByGtinAsync(string gtin)
        {
            _logger.LogInformation("Hämtar produkt från DABAS med GTIN: {Gtin}", gtin);
            try
            {
                var url = $"https://api.dabas.com/DABASService/V2/article/gtin/{gtin}/JSON?apikey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Produkt med GTIN {Gtin} hämtades framgångsrikt", gtin);
                    return await response.Content.ReadFromJsonAsync<DabasProduct>();
                }

                _logger.LogWarning("DABAS returnerade {StatusCode} för GTIN {Gtin}", (int)response.StatusCode, gtin);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid hämtning av produkt med GTIN {Gtin}", gtin);
                return null;
            }
        }

        // Hämtar alla produkter för en leverantör via deras GLN-nummer
        public async Task<List<DabasProduct>> GetProductsBySupplierGlnAsync(string supplierGln)
        {
            _logger.LogInformation("Hämtar produkter från DABAS för GLN: {Gln}", supplierGln);
            try
            {
                var url = $"https://api.dabas.com/DABASService/V2/articles/gln/{supplierGln}/JSON?apikey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var products = await response.Content.ReadFromJsonAsync<List<DabasProduct>>();
                    _logger.LogInformation("DABAS returnerade {Count} produkter för GLN {Gln}", products?.Count ?? 0, supplierGln);
                    return products ?? [];
                }

                _logger.LogWarning("DABAS returnerade {StatusCode} för GLN {Gln}", (int)response.StatusCode, supplierGln);
                return [];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid hämtning av produkter för GLN {Gln}", supplierGln);
                return [];
            }
        }
    }
}
