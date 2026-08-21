using Fontana.AI.Data;
using Fontana.AI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Fontana.AI.WebAPI.Services
{
    // Kör automatisk vinsynk från fontana.se varje natt vid konfigurerad tid (standard 04:00).
    // Kör även en initial synk 120 sekunder efter uppstart.
    public class WineSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<WineSyncBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private const string WineCacheKey = "wines";

        public WineSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<WineSyncBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory  = scopeFactory;
            _logger        = logger;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(120), stoppingToken);
            await RunSyncAsync(stoppingToken);

            var syncHour = _configuration.GetValue<int>("Wine:SyncHour", 4);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeUntilNextOccurrence(syncHour);
                _logger.LogInformation(
                    "Nästa vinsynk planerad om {Hours:0.0} timmar (kl {Hour:00}:00)",
                    delay.TotalHours, syncHour);

                await Task.Delay(delay, stoppingToken);
                await RunSyncAsync(stoppingToken);
            }
        }

        private async Task RunSyncAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope     = _scopeFactory.CreateScope();
                var wineSyncClient  = scope.ServiceProvider.GetRequiredService<WineSyncClient>();
                var context         = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cache            = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

                _logger.LogInformation("Vinsynk från fontana.se startar");
                var wines = await wineSyncClient.GetAllWinesAsync();

                if (wines.Count == 0)
                {
                    _logger.LogWarning("Vinsynk: inga viner/spritsorter hittades — hoppar över denna körning");
                    return;
                }

                var existing = await context.Wines.ToListAsync(stoppingToken);
                var (added, updated) = WineSyncMerge.Apply(context, existing, wines);
                await context.SaveChangesAsync(stoppingToken);
                cache.Remove(WineCacheKey);

                _logger.LogInformation(
                    "Vinsynk klar — {Added} nya, {Updated} uppdaterade (av {Total} hittade)",
                    added, updated, wines.Count);
            }
            catch (OperationCanceledException)
            {
                // Servern stängs av — normalt beteende, logga inte som fel
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid vinsynk");
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
