namespace Fontana.AI.Models
{
    public class Wine
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string? Type { get; set; }
        public string? Producer { get; set; }
        public string? Origin { get; set; }
        public string? AlcoholPercent { get; set; }
        public string? SystembolagNumber { get; set; }

        // Systembolagets sortimentstyp, t.ex. "Fast sortiment", "Beställningssortiment", "Tillfälligt sortiment"
        public string? AssortmentType { get; set; }
        public string? Description { get; set; }
        public required string Url { get; set; }
    }
}
