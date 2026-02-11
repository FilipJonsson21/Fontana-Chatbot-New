using System.Net.Http.Json;
using Fontana.AI.Models;

namespace Fontana.AI.Services
{
    public class DabasClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "DIN_DABAS_API_NYCKEL_HÄR"; // Byt ut mot din nyckel

        public DabasClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // DABAS kräver ofta en User-Agent eller specifik header
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "FontanaAiBot");
        }

        public async Task<DabasProduct?> GetProductByGtinAsync(string gtin)
        {
            try
            {
                // Exempel på endpoint: https://api.dabas.com/v1/article/{gtin}
                // OBS: Kontrollera exakt URL i din DABAS-dokumentation
                var url = $"https://api.dabas.com/v1/article/{gtin}?apikey={_apiKey}";

                var response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    // Här mappar vi JSON-svaret till vår modell
                    return await response.Content.ReadFromJsonAsync<DabasProduct>();
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
