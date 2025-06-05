namespace MiPokemonApp.Models.ViewModels
{
    public class PokemonDetailViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Species { get; set; } = string.Empty;
        public List<string> Abilities { get; set; } = new();
        public List<string> Types { get; set; } = new();
    }
}
