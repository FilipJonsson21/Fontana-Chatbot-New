using Fontana.AI.Data;
using Fontana.AI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Fontana.AI.WebAPI.Services
{
    // Kör automatisk receptsynk från fontana.se varje natt vid konfigurerad tid (standard 03:00).
    // Kör även en initial synk 90 sekunder efter uppstart.
    public class RecipeSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecipeSyncBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private const string RecipeCacheKey = "recipes";

        public RecipeSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<RecipeSyncBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory  = scopeFactory;
            _logger        = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(90), stoppingToken);
            await RunSyncAsync(stoppingToken);

            var syncHour = _configuration.GetValue<int>("Recipe:SyncHour", 3);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeUntilNextOccurrence(syncHour);
                _logger.LogInformation(
                    "Nästa receptsynk planerad om {Hours:0.0} timmar (kl {Hour:00}:00)",
                    delay.TotalHours, syncHour);

                await Task.Delay(delay, stoppingToken);
                await RunSyncAsync(stoppingToken);
            }
        }

        private async Task RunSyncAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope       = _scopeFactory.CreateScope();
                var recipeSyncClient  = scope.ServiceProvider.GetRequiredService<RecipeSyncClient>();
                var context           = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cache              = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

                _logger.LogInformation("Receptsynk från fontana.se startar");
                var recipes = await recipeSyncClient.GetAllRecipesAsync();

                if (recipes.Count == 0)
                {
                    _logger.LogWarning("Receptsynk: inga recept hittades — hoppar över denna körning");
                    return;
                }

                var existing = await context.Recipes.ToListAsync(stoppingToken);
                var (added, updated) = RecipeSyncMerge.Apply(context, existing, recipes);
                await context.SaveChangesAsync(stoppingToken);
                cache.Remove(RecipeCacheKey);

                _logger.LogInformation(
                    "Receptsynk klar — {Added} nya, {Updated} uppdaterade (av {Total} hittade recept)",
                    added, updated, recipes.Count);
            }
            catch (OperationCanceledException)
            {
                // Servern stängs av — normalt beteende, logga inte som fel
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid receptsynk");
            }
        }

        // Beräknar tid tills kl HH:00 nästa gång (idag eller imorgon)
        private static TimeSpan TimeUntilNextOccurrence(int hour)
        {
            var now  = DateTime.Now;
            var next = DateTime.Today.AddHours(hour);
            if (next <= now)
                next = next.AddDays(1);
            return next - now;
        }
    }
}
