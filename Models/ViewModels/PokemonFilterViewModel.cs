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
        public List<PokemonGridItemViewModel> Pokemons { get; set; } = new();
    }
}
