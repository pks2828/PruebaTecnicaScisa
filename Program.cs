using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MiPokemonApp.Models.Email;
using MiPokemonApp.Services.Implementations;
using MiPokemonApp.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar EmailSettings
builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// 2. Registrar MemoryCache
builder.Services.AddMemoryCache();

// 3. Registrar HttpClient tipado para PokeApiService
builder.Services.AddHttpClient<IPokeApiService, PokeApiService>(client =>
{
    client.BaseAddress = new Uri("https://pokeapi.co/api/v2/");
    client.Timeout = TimeSpan.FromSeconds(10);
    // Asegurar que acepte JSON
    client.DefaultRequestHeaders.Accept.Add(
        new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});

// 4. Registrar nuestros servicios
builder.Services.AddScoped<IExcelService, ExcelService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 5. Registrar SpeciesCache como Singleton
builder.Services.AddSingleton<SpeciesCache>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Middlewares básicos
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pokemon}/{action=Index}/{id?}");

app.Run();
