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
        Task<PokemonListResponse> GetPokemonListAsync(int offset, int limit);
        Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId);
        Task<List<string>> GetAllTypesAsync();
        Task<List<string>> GetPokemonTypesAsync(int id);
        Task<List<PokemonBasicInfo>> GetPokemonsByTypeFullAsync(string typeName);
    }
}
