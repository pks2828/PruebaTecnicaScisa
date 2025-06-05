namespace MiPokemonApp.Models.PokeApi
{
    public class PokemonListResponse
    {
        public int Count { get; set; }
        public string? Next { get; set; }
        public string? Previous { get; set; }
        public List<PokemonBasicInfo> Results { get; set; } = new();
    }
}
