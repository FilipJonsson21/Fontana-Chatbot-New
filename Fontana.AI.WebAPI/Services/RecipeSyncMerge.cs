using Fontana.AI.Data;
using Fontana.AI.Models;

namespace Fontana.AI.WebAPI.Services
{
    // Slår ihop nysynkade recept med befintliga i databasen, matchat på URL (unikt per receptsida).
    internal static class RecipeSyncMerge
    {
        public static (int Added, int Updated) Apply(ApplicationDbContext context, List<Recipe> existing, List<Recipe> incoming)
        {
            var byUrl = existing.ToDictionary(r => r.Url, StringComparer.OrdinalIgnoreCase);
            var added = 0;
            var updated = 0;

            foreach (var recipe in incoming)
            {
                if (byUrl.TryGetValue(recipe.Url, out var existingRecipe))
                {
                    existingRecipe.Title = recipe.Title;
                    existingRecipe.RecipeType = recipe.RecipeType;
                    existingRecipe.Description = recipe.Description;
                    updated++;
                }
                else
                {
                    context.Recipes.Add(recipe);
                    added++;
                }
            }

            return (added, updated);
        }
    }
}
