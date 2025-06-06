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
    /// <summary>
    /// Servicio para consumir la PokeAPI y obtener datos de Pokémon.
    /// </summary>
    public class PokeApiService : IPokeApiService
    {
        private readonly HttpClient _httpClient;
        // private readonly ILogger<PokeApiService> _logger; // Descomentar si se inyecta ILogger

        private const string ENDPOINT_POKEMON = "pokemon";
        private const string ENDPOINT_TYPE = "type";

        /// <summary>
        /// Inicializa una nueva instancia de <see cref="PokeApiService"/> con el cliente HTTP proporcionado.
        /// </summary>
        /// <param name="httpClient">Instancia de <see cref="HttpClient"/> ya configurada para llamar a la PokeAPI.</param>
        /// <exception cref="ArgumentNullException">Si <paramref name="httpClient"/> es nulo.</exception>
        public PokeApiService(HttpClient httpClient /*, ILogger<PokeApiService> logger */)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            // _logger = logger;
        }

        /// <summary>
        /// Obtiene de manera paginada la lista de Pokémon desde la PokeAPI.
        /// </summary>
        /// <param name="offset">Índice inicial de la paginación (>= 0).</param>
        /// <param name="limit">Cantidad máxima de resultados a retornar (> 0).</param>
        /// <returns>Objeto <see cref="PokemonListResponse"/> con los resultados paginados.</returns>
        /// <exception cref="ArgumentException">
        /// Si <paramref name="offset"/> es negativo o <paramref name="limit"/> es menor o igual a cero.
        /// </exception>
        /// <exception cref="PokeApiException">Si ocurre un error HTTP o de deserialización al llamar a la PokeAPI.</exception>
        public async Task<PokemonListResponse> GetPokemonListAsync(int offset, int limit)
        {
            // Validación de parámetros
            if (offset < 0)
                throw new ArgumentException("El parámetro offset debe ser mayor o igual a cero.", nameof(offset));
            if (limit <= 0)
                throw new ArgumentException("El parámetro limit debe ser mayor que cero.", nameof(limit));

            var relativeUrl = $"{ENDPOINT_POKEMON}?offset={offset}&limit={limit}";

            try
            {
                // _logger.LogDebug("Solicitando a PokeAPI: {BaseAddress}{RelativeUrl}", _httpClient.BaseAddress, relativeUrl);
                using var response = await _httpClient.GetAsync(relativeUrl).ConfigureAwait(false);
                // _logger.LogDebug("PokeAPI respondió con status: {StatusCode}", response.StatusCode);

                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var list = await JsonSerializer.DeserializeAsync<PokemonListResponse>(stream, options)
                           .ConfigureAwait(false)
                           ?? throw new PokeApiException("El JSON de la respuesta vino nulo al deserializar.");

                // _logger.LogDebug("PokeAPI devolvió {Count} resultados. TotalCount = {Total}", list.Results.Count, list.Count);
                return list;
            }
            catch (HttpRequestException ex)
            {
                // _logger.LogError(ex, "Error HTTP al obtener lista de Pokémon desde PokeAPI.");
                throw new PokeApiException($"Error al obtener lista de Pokémon: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // _logger.LogError(ex, "Error de deserialización JSON en GetPokemonListAsync.");
                throw new PokeApiException($"Error al procesar la respuesta JSON de la lista de Pokémon: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error inesperado en GetPokemonListAsync.");
                throw;
            }
        }

        /// <summary>
        /// Obtiene los detalles de un Pokémon por nombre o ID.
        /// </summary>
        /// <param name="nameOrId">Nombre o identificador numérico del Pokémon. No puede ser nulo ni vacío.</param>
        /// <returns>Instancia de <see cref="PokemonDetailViewModel"/> con la información detallada.</returns>
        /// <exception cref="ArgumentException">Si <paramref name="nameOrId"/> es nulo o vacío.</exception>
        /// <exception cref="PokeApiException">Si ocurre un error HTTP o de deserialización al llamar a la PokeAPI.</exception>
        public async Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId)
        {
            // Validación de parámetros
            if (string.IsNullOrWhiteSpace(nameOrId))
                throw new ArgumentException("El nombre o ID del Pokémon no puede ser nulo o vacío.", nameof(nameOrId));

            var relativeUrl = $"{ENDPOINT_POKEMON}/{nameOrId}";

            try
            {
                using var response = await _httpClient.GetAsync(relativeUrl).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                var root = doc.RootElement;

                // Extracción de propiedades
                int id = root.GetProperty("id").GetInt32();
                string name = root.GetProperty("name").GetString()!;
                string imageUrl = root
                    .GetProperty("sprites")
                    .GetProperty("other")
                    .GetProperty("official-artwork")
                    .GetProperty("front_default")
                    .GetString()!;

                var abilities = ParseStringArray(root, "abilities", "ability", "name");
                var types = ParseStringArray(root, "types", "type", "name");

                return new PokemonDetailViewModel
                {
                    Id = id,
                    Name = name,
                    ImageUrl = imageUrl,
                    Abilities = abilities,
                    Types = types
                };
            }
            catch (HttpRequestException ex)
            {
                // _logger.LogError(ex, "Error HTTP al obtener detalle de Pokémon {NameOrId}.", nameOrId);
                throw new PokeApiException($"Error al obtener detalle de {nameOrId}: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // _logger.LogError(ex, "Error de deserialización JSON en GetPokemonDetailAsync.");
                throw new PokeApiException($"Error al procesar la respuesta JSON de detalle de {nameOrId}: {ex.Message}", ex);
            }
            catch (Exception)
            {
                // _logger.LogError(ex, "Error inesperado en GetPokemonDetailAsync.");
                throw;
            }
        }

        /// <summary>
        /// Obtiene todos los nombres de tipos disponibles en la PokeAPI.
        /// </summary>
        /// <returns>Lista de nombres de tipos.</returns>
        /// <exception cref="PokeApiException">Si ocurre un error HTTP o de deserialización al llamar a la PokeAPI.</exception>
        public async Task<List<string>> GetAllTypesAsync()
        {
            var types = new List<string>();

            try
            {
                using var response = await _httpClient.GetAsync(ENDPOINT_TYPE).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                foreach (var item in doc.RootElement.GetProperty("results").EnumerateArray())
                {
                    var typeName = item.GetProperty("name").GetString();
                    if (!string.IsNullOrWhiteSpace(typeName))
                    {
                        types.Add(typeName);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // _logger.LogError(ex, "Error HTTP al obtener todos los tipos de Pokémon.");
                throw new PokeApiException($"Error al obtener los tipos de Pokémon: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // _logger.LogError(ex, "Error de deserialización JSON en GetAllTypesAsync.");
                throw new PokeApiException($"Error al procesar la respuesta JSON de tipos: {ex.Message}", ex);
            }
            catch (Exception)
            {
                // _logger.LogError(ex, "Error inesperado en GetAllTypesAsync.");
                throw;
            }

            return types;
        }

        /// <summary>
        /// Obtiene la lista de tipos de un Pokémon específico por su identificador numérico.
        /// </summary>
        /// <param name="id">Identificador del Pokémon (debe ser mayor que cero).</param>
        /// <returns>Lista de nombres de tipos asociados al Pokémon.</returns>
        /// <exception cref="ArgumentException">Si <paramref name="id"/> es menor o igual a cero.</exception>
        public async Task<List<string>> GetPokemonTypesAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("El ID del Pokémon debe ser mayor que cero.", nameof(id));

            var endpoint = $"{ENDPOINT_POKEMON}/{id}";

            try
            {
                using var response = await _httpClient.GetAsync(endpoint).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                return ParseStringArray(doc.RootElement, "types", "type", "name");
            }
            catch (HttpRequestException ex)
            {
                // _logger.LogError(ex, "Error HTTP al obtener tipos del Pokémon con ID {Id}.", id);
                Console.WriteLine($"[ERROR] Error al obtener tipos del Pokémon con ID {id}: {ex.Message}");
                return new List<string>();
            }
            catch (JsonException ex)
            {
                // _logger.LogError(ex, "Error de deserialización JSON en GetPokemonTypesAsync para ID {Id}.", id);
                Console.WriteLine($"[ERROR] Error de deserialización al obtener tipos del Pokémon con ID {id}: {ex.Message}");
                return new List<string>();
            }
            catch (Exception)
            {
                // _logger.LogError(ex, "Error inesperado en GetPokemonTypesAsync para ID {Id}.", id);
                Console.WriteLine($"[ERROR] Error inesperado al obtener tipos del Pokémon con ID {id}.");
                return new List<string>();
            }
        }

        /// <summary>
        /// Obtiene información básica de todos los Pokémon que pertenecen a un tipo específico.
        /// </summary>
        /// <param name="typeName">Nombre del tipo de Pokémon. No puede ser nulo ni vacío.</param>
        /// <returns>Lista de <see cref="PokemonBasicInfo"/> con nombre y URL de cada Pokémon del tipo.</returns>
        /// <exception cref="ArgumentException">Si <paramref name="typeName"/> es nulo o vacío.</exception>
        /// <exception cref="PokeApiException">Si ocurre un error HTTP o de deserialización al llamar a la PokeAPI.</exception>
        public async Task<List<PokemonBasicInfo>> GetPokemonsByTypeFullAsync(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
                throw new ArgumentException("El nombre del tipo no puede ser nulo o vacío.", nameof(typeName));

            var endpoint = $"{ENDPOINT_TYPE}/{typeName}";

            try
            {
                using var response = await _httpClient.GetAsync(endpoint).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var doc = await JsonDocument.ParseAsync(stream).ConfigureAwait(false);

                var list = new List<PokemonBasicInfo>();
                foreach (var p in doc.RootElement.GetProperty("pokemon").EnumerateArray())
                {
                    var poke = p.GetProperty("pokemon");
                    var name = poke.GetProperty("name").GetString()!;
                    var url = poke.GetProperty("url").GetString()!;
                    list.Add(new PokemonBasicInfo { Name = name, Url = url });
                }

                return list;
            }
            catch (HttpRequestException ex)
            {
                // _logger.LogError(ex, "Error HTTP al obtener Pokémon por tipo {TypeName}.", typeName);
                throw new PokeApiException($"Error al obtener Pokémon del tipo {typeName}: {ex.Message}", ex);
            }
            catch (JsonException ex)
            {
                // _logger.LogError(ex, "Error de deserialización JSON en GetPokemonsByTypeFullAsync para tipo {TypeName}.", typeName);
                throw new PokeApiException($"Error al procesar la respuesta JSON de Pokémon del tipo {typeName}: {ex.Message}", ex);
            }
            catch (Exception)
            {
                // _logger.LogError(ex, "Error inesperado en GetPokemonsByTypeFullAsync para tipo {TypeName}.", typeName);
                throw;
            }
        }

        /// <summary>
        /// Parsea un arreglo JSON de elementos complejos y extrae un listado de strings basado en propiedades anidadas.
        /// </summary>
        /// <param name="root">Elemento raíz que contiene el arreglo a parsear.</param>
        /// <param name="arrayPropertyName">Nombre de la propiedad de nivel superior que contiene el arreglo.</param>
        /// <param name="nestedObjectName">Nombre del objeto anidado dentro de cada elemento del arreglo.</param>
        /// <param name="valuePropertyName">Nombre de la propiedad cuyo valor se extraerá como string.</param>
        /// <returns>Lista de strings extraídos de la estructura JSON.</returns>
        private List<string> ParseStringArray(JsonElement root, string arrayPropertyName, string nestedObjectName, string valuePropertyName)
        {
            var results = new List<string>();

            if (!root.TryGetProperty(arrayPropertyName, out var arrayElement))
                return results;

            foreach (var element in arrayElement.EnumerateArray())
            {
                if (element.TryGetProperty(nestedObjectName, out var nested) &&
                    nested.TryGetProperty(valuePropertyName, out var valueElement))
                {
                    var value = valueElement.GetString();
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        results.Add(value);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Excepción personalizada para errores al interactuar con la PokeAPI.
        /// </summary>
        public class PokeApiException : Exception
        {
            public PokeApiException() { }

            public PokeApiException(string message) : base(message) { }

            public PokeApiException(string message, Exception innerException) : base(message, innerException) { }
        }
    }
}
