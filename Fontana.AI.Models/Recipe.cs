namespace Fontana.AI.Models
{
    public class Recipe
    {
        public int Id { get; set; }
        public required string Title { get; set; }
        public required string MainIngredient { get; set; }
        public string? MealType { get; set; }
        public string? Occasion { get; set; }
        public string? RecipeType { get; set; }
        public string? Description { get; set; }
        public required string Url { get; set; }
    }
}
