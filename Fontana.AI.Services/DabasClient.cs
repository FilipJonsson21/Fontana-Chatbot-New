using System.Net.Http.Json;
using Fontana.AI.Models;
using Microsoft.Extensions.Configuration;

namespace Fontana.AI.Services
{
    public class DabasClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly string _supplierGln;

        public DabasClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["Dabas:ApiKey"] ?? "";
            _supplierGln = configuration["Dabas:SupplierGln"] ?? "";

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FontanaAiBot");
        }

        // Hämtar alla Fontanas produkter via leverantörens GLN-nummer
        public async Task<List<DabasProduct>> GetAllProductsBySupplierGlnAsync()
        {
            if (string.IsNullOrEmpty(_apiKey) || string.IsNullOrEmpty(_supplierGln))
                return new List<DabasProduct>();

            try
            {
                var url = $"https://api.dabas.com/DABASService/V2/articles/GLN/{_supplierGln}/JSON?apikey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var products = await response.Content.ReadFromJsonAsync<List<DabasProduct>>();
                    return products ?? new List<DabasProduct>();
                }

                return new List<DabasProduct>();
            }
            catch (Exception)
            {
                return new List<DabasProduct>();
            }
        }

        // Hämtar en enskild produkt via GTIN (streckkod)
        public async Task<DabasProduct?> GetProductByGtinAsync(string gtin)
        {
            if (string.IsNullOrEmpty(_apiKey))
                return null;

            try
            {
                var url = $"https://api.dabas.com/DABASService/V2/article/gtin/{gtin}/JSON?apikey={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadFromJsonAsync<DabasProduct>();

                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
