using MiPokemonApp.Models.PokeApi;
using MiPokemonApp.Models.ViewModels;

namespace MiPokemonApp.Services.Interfaces
{
    /// <summary>
    /// Interfaz para el servicio de consumo de la PokeAPI, que define métodos para obtener 
    /// listas de Pokémon, detalles individuales, tipos y búsquedas por tipo.
    /// </summary>
    public interface IPokeApiService
    {
        /// <summary>
        /// Obtiene una lista paginada de Pokémon desde la PokeAPI.
        /// </summary>
        Task<PokemonListResponse> GetPokemonListAsync(int offset, int limit);

        /// <summary>
        /// Obtiene los detalles de un Pokémon por nombre o ID.
        /// </summary>
        Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId);

        /// <summary>
        /// Recupera todos los nombres de tipos disponibles en la PokeAPI.
        /// </summary>
        Task<List<string>> GetAllTypesAsync();

        /// <summary>
        /// Obtiene los tipos asociados a un Pokémon dado su ID.
        /// </summary>
        Task<List<string>> GetPokemonTypesAsync(int id);

        /// <summary>
        /// Recupera la lista completa de Pokémon que pertenecen a un tipo específico.
        /// </summary>
        Task<List<PokemonBasicInfo>> GetPokemonsByTypeFullAsync(string typeName);
    }
}
