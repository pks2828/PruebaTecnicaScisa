using MiPokemonApp.Models.PokeApi;
using MiPokemonApp.Models.ViewModels;

namespace MiPokemonApp.Services.Interfaces
{
    public interface IPokeApiService
    {
        Task<PokemonListResponse> GetPokemonListAsync(int offset, int limit);
        Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId);
        Task<string> GetSpeciesNameAsync(int speciesIdOrPokemonId);
        Task<List<string>> GetAllSpeciesNamesAsync();
    }
}
