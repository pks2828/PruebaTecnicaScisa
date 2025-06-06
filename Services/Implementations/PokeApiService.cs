using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using MiPokemonApp.Models.PokeApi;
using MiPokemonApp.Models.ViewModels;
using MiPokemonApp.Services.Interfaces;

namespace MiPokemonApp.Services.Implementations
{
    public class PokeApiService : IPokeApiService
    {
        private readonly HttpClient _httpClient;
        private readonly SpeciesCache _speciesCache;

        public PokeApiService(HttpClient httpClient, SpeciesCache speciesCache)
        {
            _httpClient = httpClient;
            _speciesCache = speciesCache;
        }

        public async Task<PokemonListResponse> GetPokemonListAsync(int offset, int limit)
        {
            try
            {
                var relativeUrl = $"pokemon?offset={offset}&limit={limit}";
                Console.WriteLine($"[DEBUG] Solicitando a PokeAPI: {_httpClient.BaseAddress}{relativeUrl}");

                var resp = await _httpClient.GetAsync(relativeUrl);
                Console.WriteLine($"[DEBUG] PokeAPI respondió con status: {(int)resp.StatusCode} ({resp.StatusCode})");
                resp.EnsureSuccessStatusCode();

                using var stream = await resp.Content.ReadAsStreamAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var list = await JsonSerializer.DeserializeAsync<PokemonListResponse>(stream, options)
                           ?? throw new Exception("El JSON vino nulo al deserializar.");

                Console.WriteLine($"[DEBUG] PokeApi devolvió {list.Results.Count} resultados. TotalCount = {list.Count}");
                return list;
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[ERROR] HttpRequestException en GetPokemonListAsync: {ex.Message}");
                throw new Exception($"Error al obtener lista de Pokémon: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Excepción en GetPokemonListAsync: {ex.Message}");
                throw;
            }
        }


        public async Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId)
        {
            try
            {
                var resp = await _httpClient.GetAsync($"pokemon/{nameOrId}");
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                var root = doc.RootElement;
                int id = root.GetProperty("id").GetInt32();
                string name = root.GetProperty("name").GetString()!;
                string imageUrl = root
                    .GetProperty("sprites")
                    .GetProperty("other")
                    .GetProperty("official-artwork")
                    .GetProperty("front_default")
                    .GetString()!;

                var abilities = new List<string>();
                foreach (var a in root.GetProperty("abilities").EnumerateArray())
                {
                    var n = a.GetProperty("ability").GetProperty("name").GetString();
                    if (n != null) abilities.Add(n);
                }

                var types = new List<string>();
                foreach (var t in root.GetProperty("types").EnumerateArray())
                {
                    var tn = t.GetProperty("type").GetProperty("name").GetString();
                    if (tn != null) types.Add(tn);
                }


                return new PokemonDetailViewModel
                {
                    Id = id,
                    Name = name,
                    ImageUrl = imageUrl,
                    Abilities = abilities,
                    Types = types
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener detalle de {nameOrId}: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GetAllTypesAsync()
        {
            var types = new List<string>();
            try
            {
                var resp = await _httpClient.GetAsync("type");
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                foreach (var item in doc.RootElement.GetProperty("results").EnumerateArray())
                {
                    var typeName = item.GetProperty("name").GetString();
                    if (typeName != null) types.Add(typeName);
                }
            }
            catch
            {
                // En caso de error, puedes devolver una lista vacía o con "desconocido"
            }

            return types;
        }

        public async Task<List<string>> GetPokemonTypesAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"pokemon/{id}");
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);

                var types = new List<string>();

                foreach (var typeEntry in doc.RootElement.GetProperty("types").EnumerateArray())
                {
                    var typeName = typeEntry.GetProperty("type").GetProperty("name").GetString();
                    if (typeName != null)
                        types.Add(typeName);
                }

                return types;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error al obtener tipos del Pokémon con ID {id}: {ex.Message}");
                return new List<string>();
            }
        }
    }
}
