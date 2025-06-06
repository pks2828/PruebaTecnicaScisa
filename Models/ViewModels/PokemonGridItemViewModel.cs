namespace MiPokemonApp.Models.ViewModels
{
    public class PokemonGridItemViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string SpeciesName { get; set; } = string.Empty;
        public List<string> Types { get; set; } = new(); // nuevo campo

    }
}
