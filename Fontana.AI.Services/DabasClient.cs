using System.Net.Http.Json;
using Fontana.AI.Models;
using Microsoft.Extensions.Configuration;

namespace Fontana.AI.Services
{
    public class DabasClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public DabasClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Dabas:ApiKey"] ?? "";
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FontanaAiBot");
        }

        public async Task<DabasProduct?> GetProductByGtinAsync(string gtin)
        {
            try
            {
                var url = $"https://api.dabas.com/DABASService.svc/article/{gtin}/JSON?apikey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DabasProduct>();
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Hämtar alla produkter för en leverantör via deras GLN-nummer
        public async Task<List<DabasProduct>> GetProductsBySupplierGlnAsync(string supplierGln)
        {
            try
            {
                var url = $"https://api.dabas.com/DABASService.svc/articles/supplier/{supplierGln}/JSON?apikey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var products = await response.Content.ReadFromJsonAsync<List<DabasProduct>>();
                    return products ?? [];
                }
                return [];
            }
            catch (Exception)
            {
                return [];
            }
        }
    }
}
