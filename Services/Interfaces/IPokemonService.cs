using MiPokemonApp.Models.ViewModels;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MiPokemonApp.Services.Interfaces
{
    /// <summary>
    /// Contiene lógica de negocio y utilerías para operaciones de Pokémon:
    /// filtrado, paginación, armado de ViewModel, cacheo y validación de email.
    /// </summary>
    public interface IPokemonService
    {
        /// <summary>
        /// Devuelve un ViewModel completo para la vista Index:
        /// incluye lista paginada, filtros, opciones de tipos y cacheo.
        /// </summary>
        Task<PokemonFilterViewModel> BuildPokemonFilterViewModelAsync(string? nameFilter,string? typeFilter,int page);

        /// <summary>
        /// Obtiene los detalles de un Pokémon (por nombre o ID).
        /// </summary>
        Task<PokemonDetailViewModel> GetPokemonDetailAsync(string nameOrId);
    }
}
