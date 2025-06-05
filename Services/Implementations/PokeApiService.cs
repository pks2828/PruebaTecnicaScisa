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

                string speciesUrl = root.GetProperty("species").GetProperty("url").GetString()!;
                int speciesId = int.Parse(speciesUrl.TrimEnd('/').Split('/').Last());
                string speciesName = await GetSpeciesNameAsync(speciesId);

                return new PokemonDetailViewModel
                {
                    Id = id,
                    Name = name,
                    ImageUrl = imageUrl,
                    Species = speciesName,
                    Abilities = abilities,
                    Types = types
                };
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener detalle de {nameOrId}: {ex.Message}", ex);
            }
        }

        public async Task<string> GetSpeciesNameAsync(int speciesIdOrPokemonId)
        {
            if (_speciesCache.TryGetSpeciesName(speciesIdOrPokemonId, out var cached))
                return cached;

            try
            {
                var resp = await _httpClient.GetAsync($"pokemon-species/{speciesIdOrPokemonId}");
                resp.EnsureSuccessStatusCode();
                using var stream = await resp.Content.ReadAsStreamAsync();
                using var doc = await JsonDocument.ParseAsync(stream);
                string name = doc.RootElement.GetProperty("name").GetString()!;
                _speciesCache.SetSpeciesName(speciesIdOrPokemonId, name);
                return name;
            }
            catch
            {
                return "desconocido";
            }
        }

        public async Task<List<string>> GetAllSpeciesNamesAsync()
        {
            var speciesNames = new List<string>();
            int offset = 0, limit = 100;
            bool seguir = true;

            while (seguir)
            {
                try
                {
                    var resp = await _httpClient.GetAsync($"pokemon-species?offset={offset}&limit={limit}");
                    resp.EnsureSuccessStatusCode();
                    using var stream = await resp.Content.ReadAsStreamAsync();
                    using var doc = await JsonDocument.ParseAsync(stream);
                    var results = doc.RootElement.GetProperty("results");
                    if (results.GetArrayLength() == 0) break;

                    foreach (var item in results.EnumerateArray())
                    {
                        var nm = item.GetProperty("name").GetString();
                        if (nm != null) speciesNames.Add(nm);
                    }

                    offset += limit;
                    if (doc.RootElement.GetProperty("next").ValueKind == JsonValueKind.Null)
                        seguir = false;
                }
                catch
                {
                    seguir = false;
                }
            }

            return speciesNames;
        }
    }
}
