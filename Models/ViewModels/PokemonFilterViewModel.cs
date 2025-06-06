namespace MiPokemonApp.Models.ViewModels
{
    public class PokemonFilterViewModel
    {
        public string? NameFilter { get; set; }
        public string? SelectedType { get; set; }

        public List<string> TypeOptions { get; set; } = new();

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }

        public List<PokemonGridItemViewModel> Pokemons { get; set; } = new();
        public List<int> PageNumbers { get; set; } = new();

        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
}
