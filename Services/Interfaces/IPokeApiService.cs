using MiPokemonApp.Models.PokeApi;
using MiPokemonApp.Models.ViewModels;

namespace MiPokemonApp.Services.Interfaces
{
    public interface IPokeApiService
    {
        Task<PokemonListResponse> GetPokemonListAsync(int offset, int limit);
        Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId);
        Task<List<string>> GetAllTypesAsync();
        Task<List<string>> GetPokemonTypesAsync(int id);
        Task<List<PokemonBasicInfo>> GetPokemonsByTypeFullAsync(string typeName);

    }
}
