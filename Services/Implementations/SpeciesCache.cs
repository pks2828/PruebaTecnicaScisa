using System;
using Microsoft.Extensions.Caching.Memory;

namespace MiPokemonApp.Services.Implementations
{
    public class SpeciesCache
    {
        private readonly IMemoryCache _memoryCache;
        private const string CachePrefix = "Species_";

        public SpeciesCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public void SetSpeciesName(int id, string name)
        {
            var options = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromHours(12));
            _memoryCache.Set($"{CachePrefix}{id}", name, options);
        }

        public bool TryGetSpeciesName(int id, out string speciesName)
        {
            return _memoryCache.TryGetValue($"{CachePrefix}{id}", out speciesName);
        }
    }
}
