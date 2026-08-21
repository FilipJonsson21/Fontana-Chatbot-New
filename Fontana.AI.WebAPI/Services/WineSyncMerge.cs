using Fontana.AI.Data;
using Fontana.AI.Models;

namespace Fontana.AI.WebAPI.Services
{
    // Slår ihop nysynkade viner/sprit med befintliga i databasen, matchat på URL (unikt per produktsida).
    internal static class WineSyncMerge
    {
        public static (int Added, int Updated) Apply(ApplicationDbContext context, List<Wine> existing, List<Wine> incoming)
        {
            var byUrl = existing.ToDictionary(w => w.Url, StringComparer.OrdinalIgnoreCase);
            var added = 0;
            var updated = 0;

            foreach (var wine in incoming)
            {
                if (byUrl.TryGetValue(wine.Url, out var existingWine))
                {
                    existingWine.Name = wine.Name;
                    existingWine.Type = wine.Type;
                    existingWine.Producer = wine.Producer;
                    existingWine.Origin = wine.Origin;
                    existingWine.AlcoholPercent = wine.AlcoholPercent;
                    existingWine.AssortmentType = wine.AssortmentType;
                    existingWine.SystembolagNumber = wine.SystembolagNumber;
                    existingWine.Description = wine.Description;
                    updated++;
                }
                else
                {
                    context.Wines.Add(wine);
                    added++;
                }
            }

            return (added, updated);
        }
    }
}
