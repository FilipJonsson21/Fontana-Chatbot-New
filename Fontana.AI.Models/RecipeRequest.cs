using System.ComponentModel.DataAnnotations;

namespace Fontana.AI.Models
{
    public class RecipeRequest
    {
        [Required(ErrorMessage = "Titeln är obligatorisk.")]
        [MaxLength(200, ErrorMessage = "Titeln får vara max 200 tecken.")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Huvudingrediensen är obligatorisk.")]
        [MaxLength(100, ErrorMessage = "Huvudingrediensen får vara max 100 tecken.")]
        public required string MainIngredient { get; set; }

        [MaxLength(100, ErrorMessage = "Måltid får vara max 100 tecken.")]
        public string? MealType { get; set; }

        [MaxLength(100, ErrorMessage = "Tillfälle får vara max 100 tecken.")]
        public string? Occasion { get; set; }

        [MaxLength(100, ErrorMessage = "Recepttyp får vara max 100 tecken.")]
        public string? RecipeType { get; set; }

        [MaxLength(500, ErrorMessage = "Beskrivningen får vara max 500 tecken.")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Länken till receptet är obligatorisk.")]
        [MaxLength(500, ErrorMessage = "Länken får vara max 500 tecken.")]
        [Url(ErrorMessage = "Länken måste vara en giltig URL.")]
        public required string Url { get; set; }
    }
}
