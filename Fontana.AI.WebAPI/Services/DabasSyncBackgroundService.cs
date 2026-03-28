using Fontana.AI.Data;
using Fontana.AI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Fontana.AI.WebAPI.Services
{
    // Kör automatisk DABAS-synk varje natt vid konfigurerad tid (standard 02:00).
    // Kör även en initial synk 60 sekunder efter uppstart.
    public class DabasSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DabasSyncBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private const string ProductCacheKey = "dabas_products";

        public DabasSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<DabasSyncBackgroundService> logger,
            IConfiguration configuration)
        {
            _scopeFactory   = scopeFactory;
            _logger         = logger;
            _configuration  = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Vänta lite efter uppstart så att databasen hinner migreras klart
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            await RunSyncAsync(stoppingToken);

            var syncHour = _configuration.GetValue<int>("Dabas:SyncHour", 2);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = TimeUntilNextOccurrence(syncHour);
                _logger.LogInformation(
                    "Nästa DABAS auto-synk planerad om {Hours:0.0} timmar (kl {Hour:00}:00)",
                    delay.TotalHours, syncHour);

                await Task.Delay(delay, stoppingToken);
                await RunSyncAsync(stoppingToken);
            }
        }

        private async Task RunSyncAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope   = _scopeFactory.CreateScope();
                var dabasClient   = scope.ServiceProvider.GetRequiredService<DabasClient>();
                var context       = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cache         = scope.ServiceProvider.GetRequiredService<IMemoryCache>();
                var config        = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var supplierGln = config["Dabas:SupplierGln"] ?? "";
                if (string.IsNullOrEmpty(supplierGln))
                {
                    _logger.LogWarning("DABAS auto-synk avbruten: Dabas:SupplierGln saknas i konfigurationen");
                    return;
                }

                _logger.LogInformation("DABAS auto-synk startar (GLN: {Gln})", supplierGln);
                var products = await dabasClient.GetProductsBySupplierGlnAsync(supplierGln);

                if (!products.Any())
                {
                    _logger.LogWarning("DABAS auto-synk: Inga produkter returnerades från DABAS");
                    return;
                }

                // Upsert: uppdatera befintliga, lägg till nya, ta bort utgångna
                var existing       = await context.DabasProducts.ToListAsync(stoppingToken);
                var existingByGtin = existing.ToDictionary(p => p.Gtin);
                var incomingGtins  = products.Select(p => p.Gtin).ToHashSet();
                var now            = DateTime.UtcNow;

                var toRemove = existing.Where(p => !incomingGtins.Contains(p.Gtin)).ToList();
                if (toRemove.Count > 0)
                    context.DabasProducts.RemoveRange(toRemove);

                foreach (var product in products)
                {
                    product.LastSynced = now;
                    if (existingByGtin.TryGetValue(product.Gtin, out var existingProduct))
                    {
                        existingProduct.ProductName = product.ProductName;
                        existingProduct.Ingredients = product.Ingredients;
                        existingProduct.Allergens   = product.Allergens;
                        existingProduct.Origin      = product.Origin;
                        existingProduct.Nutrition   = product.Nutrition;
                        existingProduct.LastSynced  = now;
                    }
                    else
                    {
                        context.DabasProducts.Add(product);
                    }
                }

                await context.SaveChangesAsync(stoppingToken);
                cache.Remove(ProductCacheKey);

                _logger.LogInformation(
                    "DABAS auto-synk klar — {Synced} synkade, {Removed} borttagna",
                    products.Count, toRemove.Count);
            }
            catch (OperationCanceledException)
            {
                // Servern stängs av — normalt beteende, logga inte som fel
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fel vid DABAS auto-synk");
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
