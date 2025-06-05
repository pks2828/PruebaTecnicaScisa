namespace MiPokemonApp.Models.ViewModels
{
    public class PokemonFilterViewModel
    {
        public string? NameFilter { get; set; }
        public string? SelectedSpecies { get; set; }
        public List<string> SpeciesOptions { get; set; } = new();

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }

        // Lista de items que se mostrarán en la página actual
        public List<PokemonGridItemViewModel> Pokemons { get; set; } = new();

        // NUEVA: Números de página que el paginador debe mostrar (ej. [3,4,5,6,7])
        public List<int> PageNumbers { get; set; } = new();

        // (Opcional) Exponer si hay anterior o siguiente
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
