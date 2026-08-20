using Fontana.AI.Data;
using Fontana.AI.Services;
using Fontana.AI.WebAPI.Middleware;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Tillåtna origins läses från konfiguration — sätt "AllowedOrigins" i appsettings
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorNumbersToAdd: null)));

builder.Services.AddMemoryCache();
builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<DabasClient>();
builder.Services.AddHostedService<Fontana.AI.WebAPI.Services.DabasSyncBackgroundService>();

builder.Services.AddHttpClient<RecipeSyncClient>();
builder.Services.AddHostedService<Fontana.AI.WebAPI.Services.RecipeSyncBackgroundService>();

builder.Services.AddHttpClient<WineSyncClient>();
builder.Services.AddHostedService<Fontana.AI.WebAPI.Services.WineSyncBackgroundService>();

// Rate limiting: max 20 anrop per minut per IP-adress på chat-endpointen,
// max 5 inloggningsförsök per minut per IP-adress på admin-inloggningen
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("chat", context =>
    {
        // Partitionera per IP-adress så varje klient har sin egen gräns
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 20,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
    options.AddPolicy("admin-login", context =>
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 4,
            QueueLimit = 0,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst
        });
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Returnera JSON-fel vid 429
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.ContentType = "application/json";
        await context.HttpContext.Response.WriteAsync(
            "{\"error\":\"För många förfrågningar. Försök igen om en stund.\"}",
            cancellationToken);
    };
});

var app = builder.Build();

// Skapa och migrera databasen automatiskt vid uppstart
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseMiddleware<ApiKeyMiddleware>();
app.UseMiddleware<AdminAuthMiddleware>();
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Denna ska finnas EN gång
    app.MapScalarApiReference(); // Denna kopplar ihop Scalar med OpenAPI
}

app.UseAuthorization();

app.MapControllers();

app.Run();

