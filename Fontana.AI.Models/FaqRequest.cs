using System.ComponentModel.DataAnnotations;

namespace Fontana.AI.Models
{
    // DTO för att skapa eller uppdatera en FAQ-post
    public class FaqRequest
    {
        [Required(ErrorMessage = "Frågan är obligatorisk.")]
        [MaxLength(500, ErrorMessage = "Frågan får vara max 500 tecken.")]
        public required string Question { get; set; }

        [Required(ErrorMessage = "Svaret är obligatoriskt.")]
        [MaxLength(2000, ErrorMessage = "Svaret får vara max 2000 tecken.")]
        public required string Answer { get; set; }

        [MaxLength(100, ErrorMessage = "Kategorin får vara max 100 tecken.")]
        public string? Category { get; set; }
    }
}
