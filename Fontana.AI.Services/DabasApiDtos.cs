using System.Text.Json.Serialization;

namespace Fontana.AI.Services
{
    // DTO för ett objekt i listsvar från GET /articles/gln/{gln}/JSON
    // Fältnamnen är bekräftade via API-anrop (svenska namn)
    internal class DabasListItemDto
    {
        [JsonPropertyName("GTIN")]
        public string Gtin { get; set; } = string.Empty;

        [JsonPropertyName("Produktnamn")]
        public string Produktnamn { get; set; } = string.Empty;

        [JsonPropertyName("Varumarke")]
        public string Varumarke { get; set; } = string.Empty;

        [JsonPropertyName("Artikelkategori")]
        public string Artikelkategori { get; set; } = string.Empty;

        [JsonPropertyName("Artikelbenamning")]
        public string Artikelbenamning { get; set; } = string.Empty;

        [JsonPropertyName("Forpackningsstorlek")]
        public string Forpackningsstorlek { get; set; } = string.Empty;
    }

    // DTO för detaljsvar från GET /article/gtin/{gtin}/JSON
    // OBS: fältnamnen är preliminära — uppdatera efter att ha sett ett riktigt API-svar
    internal class DabasDetailDto
    {
        [JsonPropertyName("GTIN")]
        public string Gtin { get; set; } = string.Empty;

        [JsonPropertyName("Produktnamn")]
        public string Produktnamn { get; set; } = string.Empty;

        [JsonPropertyName("Ingrediensforteckning")]
        public string Ingrediensforteckning { get; set; } = string.Empty;

        [JsonPropertyName("Allergenpastande")]
        public string Allergenpastande { get; set; } = string.Empty;

        [JsonPropertyName("Ursprungsland")]
        public string Ursprungsland { get; set; } = string.Empty;

        // Näringsvärden är ofta ett nästlat objekt/array — vi serialiserar dem som rå JSON-text
        [JsonPropertyName("Naringsvarden")]
        public object? Naringsvarden { get; set; }
    }
}
