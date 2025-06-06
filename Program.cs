using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MiPokemonApp.Models.Email;
using MiPokemonApp.Services.Implementations;
using MiPokemonApp.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// --- 1) Configuración de servicios ---
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient<IPokeApiService, PokeApiService>(client =>
{
    client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

builder.Services.AddScoped<IPokemonService, PokemonService>();
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

// --- 2) Middlewares básicos ---
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

// --- 3) Ruta “por defecto”: /  → Pokemon/Index ---
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pokemon}/{action=Index}/{id?}");

// --- 4) Ruta catch-all: cualquier URL inválida → Pokemon/Index ---
app.MapControllerRoute(
    name: "catchAllToPokemon",
    pattern: "{*catchall}",
    defaults: new { controller = "Pokemon", action = "Index" });

app.Run();