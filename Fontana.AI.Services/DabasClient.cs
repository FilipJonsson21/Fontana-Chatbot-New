using System.Net.Http.Json;
using System.Text.Json;
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

        // Hämtar detaljinfo för en produkt via GTIN
        public async Task<DabasProduct?> GetProductByGtinAsync(string gtin)
        {
            _logger.LogInformation("Hämtar produkt från DABAS med GTIN: {Gtin}", gtin);
            try
            {
                var url = $"https://api.dabas.com/DABASService/V2/article/gtin/{gtin}/JSON?apikey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("DABAS returnerade {StatusCode} för GTIN {Gtin}", (int)response.StatusCode, gtin);
                    return null;
                }

                var detail = await response.Content.ReadFromJsonAsync<DabasDetailDto>();
                if (detail is null) return null;

                _logger.LogInformation("Produkt med GTIN {Gtin} hämtades framgångsrikt", gtin);
                return MapDetailToProduct(detail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid hämtning av produkt med GTIN {Gtin}", gtin);
                return null;
            }
        }

        // Hämtar alla produkter för en leverantör via GLN — tvåstegs-sync:
        // 1. Lista alla GTINs via leverantörsendpoint
        // 2. Hämta detaljer per GTIN för ingredienser/allergener
        public async Task<List<DabasProduct>> GetProductsBySupplierGlnAsync(string supplierGln)
        {
            _logger.LogInformation("Hämtar produkter från DABAS för GLN: {Gln}", supplierGln);
            try
            {
                // Steg 1: hämta produktlistan
                var listUrl = $"https://api.dabas.com/DABASService/V2/articles/gln/{supplierGln}/JSON?apikey={_apiKey}";
                var listResponse = await _httpClient.GetAsync(listUrl);

                if (!listResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("DABAS returnerade {StatusCode} för GLN {Gln}", (int)listResponse.StatusCode, supplierGln);
                    return [];
                }

                var listItems = await listResponse.Content.ReadFromJsonAsync<List<DabasListItemDto>>();
                if (listItems is null || listItems.Count == 0)
                {
                    _logger.LogInformation("Inga produkter hittades för GLN {Gln}", supplierGln);
                    return [];
                }

                _logger.LogInformation("DABAS listade {Count} produkter för GLN {Gln} — hämtar detaljer", listItems.Count, supplierGln);

                // Steg 2: hämta detaljer per GTIN
                var products = new List<DabasProduct>(listItems.Count);
                foreach (var item in listItems)
                {
                    if (string.IsNullOrEmpty(item.Gtin)) continue;

                    var product = await FetchDetailAsync(item);
                    products.Add(product);
                }

                _logger.LogInformation("Sync klar — {Count} produkter hämtade med detaljer", products.Count);
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid hämtning av produkter för GLN {Gln}", supplierGln);
                return [];
            }
        }

        // Hämtar detaljer för ett listobjekt — vid fel används grunddatan från listan
        private async Task<DabasProduct> FetchDetailAsync(DabasListItemDto item)
        {
            try
            {
                // DABAS detaljendpoint kräver 14-siffrigt GTIN (GTIN-14) med ledande nolla
                var gtin14 = item.Gtin.PadLeft(14, '0');
                var detailUrl = $"https://api.dabas.com/DABASService/V2/article/gtin/{gtin14}/JSON?apikey={_apiKey}";
                var detailResponse = await _httpClient.GetAsync(detailUrl);

                if (detailResponse.IsSuccessStatusCode)
                {
                    var detail = await detailResponse.Content.ReadFromJsonAsync<DabasDetailDto>();
                    if (detail is not null)
                        return MapDetailToProduct(detail, item);
                }

                _logger.LogWarning("Detaljer saknas för GTIN {Gtin} — använder grunddata", item.Gtin);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Kunde inte hämta detaljer för GTIN {Gtin}", item.Gtin);
            }

            // Fallback: använd grunddata från listan
            return MapListItemToProduct(item);
        }

        // Mappar ett detaljsvar till DabasProduct-entitet
        private static DabasProduct MapDetailToProduct(DabasDetailDto detail, DabasListItemDto? listItem = null)
        {
            return new DabasProduct
            {
                Gtin = detail.Gtin,
                ProductName = detail.Produktnamn,
                Ingredients = detail.Ingrediensforteckning,
                Allergens = detail.Allergenpastande,
                Origin = detail.Ursprungsland,
                Nutrition = detail.Naringsvarden is not null
                    ? JsonSerializer.Serialize(detail.Naringsvarden)
                    : string.Empty
            };
        }

        // Mappar ett listobjekt till DabasProduct-entitet (används som fallback)
        private static DabasProduct MapListItemToProduct(DabasListItemDto item)
        {
            return new DabasProduct
            {
                Gtin = item.Gtin,
                ProductName = string.IsNullOrEmpty(item.Produktnamn) ? item.Artikelbenamning : item.Produktnamn,
                Ingredients = string.Empty,
                Allergens = string.Empty,
                Origin = string.Empty,
                Nutrition = string.Empty
            };
        }
    }
}
