using System.ComponentModel.DataAnnotations;

namespace Fontana.AI.Models
{
    public class WineRequest
    {
        [Required(ErrorMessage = "Namnet är obligatoriskt.")]
        [MaxLength(200, ErrorMessage = "Namnet får vara max 200 tecken.")]
        public required string Name { get; set; }

        [MaxLength(100, ErrorMessage = "Typ får vara max 100 tecken.")]
        public string? Type { get; set; }

        [MaxLength(200, ErrorMessage = "Producent får vara max 200 tecken.")]
        public string? Producer { get; set; }

        [MaxLength(100, ErrorMessage = "Ursprung får vara max 100 tecken.")]
        public string? Origin { get; set; }

        [MaxLength(20, ErrorMessage = "Alkoholhalt får vara max 20 tecken.")]
        public string? AlcoholPercent { get; set; }

        [MaxLength(100, ErrorMessage = "Sortimentstyp får vara max 100 tecken.")]
        public string? AssortmentType { get; set; }

        [MaxLength(50, ErrorMessage = "Beställningsnummer får vara max 50 tecken.")]
        public string? SystembolagNumber { get; set; }

        [MaxLength(500, ErrorMessage = "Beskrivningen får vara max 500 tecken.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Länken är obligatorisk.")]
        [MaxLength(500, ErrorMessage = "Länken får vara max 500 tecken.")]
        [Url(ErrorMessage = "Länken måste vara en giltig URL.")]
        public required string Url { get; set; }
    }
}
