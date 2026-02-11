using Fontana.AI.Data;
using Fontana.AI.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin() // Tillåter alla källor (bra för utveckling)
              .AllowAnyMethod() // Tillåter POST, GET etc.
              .AllowAnyHeader(); // Tillåter alla headers
    });
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IChatService, ChatService>(); 

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddHttpClient<DabasClient>();

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Denna ska finnas EN gång
    app.MapScalarApiReference(); // Denna kopplar ihop Scalar med OpenAPI
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
